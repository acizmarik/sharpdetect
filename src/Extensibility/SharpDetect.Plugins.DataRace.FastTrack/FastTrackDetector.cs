// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class FastTrackDetector
{
    private readonly FastTrackPluginConfiguration _configuration;
    private readonly ShadowMemory _shadowMemory = new();
    private readonly AccessTracker _accessTracker;
    private readonly FieldResolver _fieldResolver;
    private readonly TimeProvider _timeProvider;

    private readonly ThreadClockTable _threadClocks = new();
    private readonly ObjectClockSlots _forkClocks;
    private readonly ObjectClockSlots _taskClocks;
    private readonly ObjectClockSlots _taskRegistrationClocks;
    private readonly TrackedObjectSet _completedTasks = new();
    private readonly ObjectClockSlots _lockClocks;
    private readonly ObjectClockSlots _eventClocks;
    private readonly SemaphorePermitPools _semaphorePermits;
    private readonly VolatileClockTable _volatileClocks;
    private readonly PublicationTracker _publications;
    private readonly TypeInitializationClocks _typeInitializations;
    private readonly WriteClassifier _writeClassifier;
    private readonly ITrackedObjectState[] _objectKeyedState;

    public FastTrackDetector(
        FastTrackPluginConfiguration configuration,
        IMetadataContext metadataContext,
        TimeProvider timeProvider,
        ILogger logger,
        Func<ProcessThreadId, string?> threadNameResolver)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;
        _accessTracker = new AccessTracker(threadNameResolver);
        _fieldResolver = new FieldResolver(metadataContext, logger);

        _forkClocks = new ObjectClockSlots(_threadClocks);
        _taskClocks = new ObjectClockSlots(_threadClocks);
        _taskRegistrationClocks = new ObjectClockSlots(_threadClocks);
        _lockClocks = new ObjectClockSlots(_threadClocks);
        _eventClocks = new ObjectClockSlots(_threadClocks);
        _semaphorePermits = new SemaphorePermitPools(_threadClocks);
        _volatileClocks = new VolatileClockTable(_threadClocks);
        _publications = new PublicationTracker(_threadClocks);
        _typeInitializations = new TypeInitializationClocks(_threadClocks, _fieldResolver, new MethodResolver(metadataContext, logger));
        _writeClassifier = new WriteClassifier(metadataContext, _typeInitializations, logger);

        _objectKeyedState =
        [
            _forkClocks,
            _taskClocks,
            _taskRegistrationClocks,
            _completedTasks,
            _lockClocks,
            _eventClocks,
            _semaphorePermits,
            _volatileClocks,
            _publications,
            _writeClassifier
        ];
    }

    public int GetShadowVariableCount() => _shadowMemory.Count;
    public int GetTrackedThreadCount() => _threadClocks.Count;
    public int GetTrackedPublicationCount() => _publications.PublicationCount;
    internal int GetIndexedPublicationParticipantCount() => _publications.IndexedParticipantCount;
    internal int GetPublicationObserverEntryCount() => _publications.PendingObservationCount;

    public void RecordThreadCreated(ProcessThreadId threadId)
        => _threadClocks.EnsureExists(threadId);

    public void RecordThreadDestroyed(ProcessThreadId threadId)
    {
        // Keep the clock around for join operations; it will be cleaned up naturally
        // Its index in the thread index table must not be given to a different thread either
    }

    public void RecordGarbageCollectedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        _shadowMemory.RemoveTrackedObjects(processId, removedObjectIds);
        _accessTracker.RemoveTrackedObjects(processId, removedObjectIds);

        foreach (var objectId in removedObjectIds)
        {
            var processObjectId = new ProcessTrackedObjectId(processId, objectId);
            foreach (var state in _objectKeyedState)
                state.RemoveTrackedObject(processObjectId);
        }
    }

    public void RecordFinalizationQueuedObjects(uint processId, ReadOnlySpan<TrackedObjectId> queuedObjectIds)
        => _writeClassifier.RecordFinalizationQueued(processId, queuedObjectIds);

    public void RecordThreadForkRequested(ProcessThreadId parentThreadId, ProcessTrackedObjectId threadObjectId)
        => _forkClocks.ReleaseSnapshot(threadObjectId, parentThreadId);

    public void RecordThreadFork(ProcessTrackedObjectId threadObjectId, ProcessThreadId childThreadId)
    {
        _threadClocks.EnsureExists(childThreadId);
        _forkClocks.TryAcquireOnce(threadObjectId, childThreadId);
    }

    public void RecordThreadJoin(ProcessThreadId joinerThreadId, ProcessThreadId joinedThreadId)
        => _threadClocks.AcquireFromThread(joinerThreadId, joinedThreadId);

    public void RecordTaskScheduled(ProcessThreadId parentThreadId, ProcessTrackedObjectId taskId)
        => _taskClocks.ReleaseSnapshot(taskId, parentThreadId);

    public void RecordTaskContinuationRegistered(
        ProcessThreadId registeringThreadId,
        ProcessTrackedObjectId continuationTaskId)
        => _taskRegistrationClocks.ReleaseSnapshot(continuationTaskId, registeringThreadId);

    public void RecordTaskStarted(ProcessThreadId workerThreadId, ProcessTrackedObjectId taskId)
    {
        // The scheduling clock is kept for later joins, but a registration is consumed by the run it enables
        _taskClocks.TryAcquire(taskId, workerThreadId);
        _taskRegistrationClocks.TryAcquireOnce(taskId, workerThreadId);
    }

    public void RecordTaskCompleted(ProcessThreadId workerThreadId, ProcessTrackedObjectId taskId)
    {
        _taskClocks.ReleaseSnapshot(taskId, workerThreadId);
        _completedTasks.Add(taskId);
    }

    public void RecordTaskSegmentCompleted(ProcessThreadId workerThreadId, ProcessTrackedObjectId taskId)
        => _taskClocks.ReleaseSnapshot(taskId, workerThreadId);

    public void RecordTaskPromiseCompleted(ProcessThreadId completerThreadId, ProcessTrackedObjectId taskId)
    {
        _taskClocks.Release(taskId, completerThreadId);
        _completedTasks.Add(taskId);
    }

    public void RecordTaskJoinFinished(ProcessThreadId waiterThreadId, ProcessTrackedObjectId taskId)
    {
        _taskClocks.TryAcquire(taskId, waiterThreadId);
        _threadClocks.Advance(waiterThreadId);
    }

    public void RecordTaskJoinThrew(ProcessThreadId waiterThreadId, ProcessTrackedObjectId taskId)
    {
        if (_completedTasks.Contains(taskId))
            RecordTaskJoinFinished(waiterThreadId, taskId);
    }

    public void RecordLockAcquired(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
        => _lockClocks.Acquire(lockId, threadId);

    public void RecordLockReleased(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
        => _lockClocks.Release(lockId, threadId);

    public void RecordObjectWaitCalled(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
        => RecordLockReleased(threadId, lockId);

    public void RecordObjectWaitReturned(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
        => RecordLockAcquired(threadId, lockId);

    public void RecordSemaphoreCreated(ProcessTrackedObjectId semaphoreId, int initialCount)
        => _semaphorePermits.Create(semaphoreId, initialCount);

    public void RecordSemaphoreAcquired(ProcessThreadId threadId, ProcessTrackedObjectId semaphoreId)
        => _semaphorePermits.Acquire(semaphoreId, threadId);

    public void RecordSemaphoreReleased(ProcessThreadId threadId, ProcessTrackedObjectId semaphoreId, int releaseCount)
        => _semaphorePermits.Release(semaphoreId, threadId, releaseCount);

    public void RecordEventCreated(ProcessTrackedObjectId eventId, bool initialState)
    {
        if (initialState)
            _eventClocks.SetEmpty(eventId);
        else
            _eventClocks.Remove(eventId);
    }

    public void RecordEventSignaled(ProcessThreadId threadId, ProcessTrackedObjectId eventId)
        => _eventClocks.Release(eventId, threadId);

    public void RecordEventReset(ProcessTrackedObjectId eventId)
        => _eventClocks.Remove(eventId);

    public void RecordEventWaitReturned(ProcessThreadId threadId, ProcessTrackedObjectId eventId, bool isAutoReset)
    {
        if (_eventClocks.TryAcquire(eventId, threadId) && isAutoReset)
            _eventClocks.Remove(eventId);
    }

    public DataRaceInfo? RecordVolatileRead(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        CapturedStackTrace stack)
    {
        var moduleId = stack.Top.ModuleId;
        AcquireTypeInitialization(threadId, moduleId, fieldToken, objectId);
        if (!TryResolveField(threadId.ProcessId, moduleId, fieldToken, declaringTypeHandle, out var fieldId, out var isAnalyzed))
            return null;

        _volatileClocks.Acquire(fieldId, objectId, threadId);
        if (!isAnalyzed)
            return null;

        var threadVc = _threadClocks.GetOrCreate(threadId);
        return RecordAtomicAccess(threadId, methodOffset, fieldId, objectId, threadVc.GetEpoch(threadId), threadVc, stored: false, stack);
    }

    public DataRaceInfo? RecordVolatileWrite(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        CapturedStackTrace stack)
    {
        var moduleId = stack.Top.ModuleId;
        AcquireTypeInitialization(threadId, moduleId, fieldToken, objectId);
        if (!TryResolveField(threadId.ProcessId, moduleId, fieldToken, declaringTypeHandle, out var fieldId, out var isAnalyzed))
            return null;

        var threadVc = _threadClocks.GetOrCreate(threadId);
        var epoch = threadVc.GetEpoch(threadId);
        _volatileClocks.ReleaseProvisionally(fieldId, objectId, threadId);
        return isAnalyzed
            ? RecordAtomicAccess(threadId, methodOffset, fieldId, objectId, epoch, threadVc, stored: true, stack)
            : null;
    }

    public void RecordVolatileWriteCompleted(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle)
    {
        if (TryResolveField(threadId.ProcessId, moduleId, fieldToken, declaringTypeHandle, out var fieldId, out _))
            _volatileClocks.CompleteRelease(fieldId, objectId, threadId);
    }

    public void RecordAtomicRelease(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle)
    {
        AcquireTypeInitialization(threadId, moduleId, fieldToken, objectId);
        if (!TryResolveField(threadId.ProcessId, moduleId, fieldToken, declaringTypeHandle, out var fieldId, out var isAnalyzed))
            return;

        if (isAnalyzed)
            BeginAtomicOperation(threadId, fieldId, objectId);

        _volatileClocks.ReleaseProvisionally(fieldId, objectId, threadId);
    }

    public DataRaceInfo? RecordAtomicAcquire(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        CapturedStackTrace stack)
        => CompleteAtomicOperation(threadId, methodOffset, fieldToken, objectId, declaringTypeHandle, stack, stored: true);

    public void RecordCompareExchangeRelease(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle)
        => RecordAtomicRelease(threadId, moduleId, fieldToken, objectId, declaringTypeHandle);

    public DataRaceInfo? RecordCompareExchangeCompleted(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        CapturedStackTrace stack,
        bool succeeded)
        => CompleteAtomicOperation(threadId, methodOffset, fieldToken, objectId, declaringTypeHandle, stack, stored: succeeded);

    public void RecordValuePublished(
        ProcessThreadId threadId,
        ProcessTrackedObjectId containerId,
        ProcessTrackedObjectId valueId,
        bool onlyIfAbsent = false)
        => _publications.RecordPublished(threadId, containerId, valueId, onlyIfAbsent);

    public void RecordValueObserved(
        ProcessThreadId threadId,
        ProcessTrackedObjectId containerId,
        ProcessTrackedObjectId valueId)
        => _publications.RecordObserved(threadId, containerId, valueId);

    public void RecordTypeInitializerEntered(ProcessThreadId threadId, ModuleId moduleId, MdMethodDef typeInitializerToken)
        => _typeInitializations.Enter(threadId, moduleId, typeInitializerToken);

    public void RecordTypeInitialized(ProcessThreadId threadId, ModuleId moduleId, MdMethodDef typeInitializerToken)
        => _typeInitializations.Release(threadId, moduleId, typeInitializerToken);

    public void RecordTypeInitializerFailed(ProcessThreadId threadId, ModuleId moduleId, MdMethodDef typeInitializerToken)
        => _typeInitializations.Fail(threadId, moduleId, typeInitializerToken);

    public DataRaceInfo? RecordRead(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        CapturedStackTrace stack)
    {
        AcquireTypeInitialization(threadId, stack.Top.ModuleId, fieldToken, objectId);
        if (!TryResolveAnalyzedField(threadId, stack, fieldToken, declaringTypeHandle, out var fieldId))
            return null;

        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = _threadClocks.GetOrCreateSeen(threadId);
        _writeClassifier.NoteAccess(threadId, objectId);

        // Write-read race: the last write does not happen-before this read
        var conflict = HasUnorderedWrite(shadow, threadVc, objectId)
            ? GetConflictingWrite(fieldId, objectId, threadId)
            : null;

        // A plain read of a value stored atomically cannot tear, so it is reported only on request
        if (_configuration.ReportPlainReadsRacingWithAtomicWrites)
            conflict ??= FindAtomicConflict(fieldId, objectId, threadId, shadow, threadVc, storesOnly: true);

        UpdateReadState(threadId, shadow, threadVc);
        var currentAccess = _accessTracker.RecordAccess(
            fieldId, objectId, threadId, methodOffset, AccessType.Read, stack, trackReadsByThread: shadow.HasReadVectorClock);
        if (_configuration.ReportPlainReadsRacingWithAtomicWrites)
            shadow.InFlightAtomicAccesses?.NotePlainAccess(threadId, threadVc, threadVc.GetEpoch(threadId), currentAccess, isWrite: false);

        return CreateRaceInfo(threadId, fieldId, objectId, currentAccess, conflict);
    }

    public DataRaceInfo? RecordWrite(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        CapturedStackTrace stack)
    {
        AcquireTypeInitialization(threadId, stack.Top.ModuleId, fieldToken, objectId);
        if (!TryResolveAnalyzedField(threadId, stack, fieldToken, declaringTypeHandle, out var fieldId))
            return null;

        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = _threadClocks.GetOrCreateSeen(threadId);
        var currentEpoch = threadVc.GetEpoch(threadId);
        var writeKind = _writeClassifier.Classify(threadId, fieldId.FieldDef, objectId, stack);

        var conflict = writeKind == WriteKind.Regular
            ? FindWriteConflict(fieldId, objectId, threadId, shadow, threadVc) ??
              FindAtomicConflict(fieldId, objectId, threadId, shadow, threadVc, storesOnly: false)
            : null;

        var currentAccess = _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Write, stack);
        if (writeKind == WriteKind.Regular)
            shadow.InFlightAtomicAccesses?.NotePlainAccess(threadId, threadVc, currentEpoch, currentAccess, isWrite: true);

        shadow.SetWrite(currentEpoch, writeKind);
        shadow.SetRead(Epoch.None);
        shadow.ClearAtomicAccesses();
        return CreateRaceInfo(threadId, fieldId, objectId, currentAccess, conflict);
    }

    private DataRaceInfo? RecordAtomicAccess(
        ProcessThreadId threadId,
        uint methodOffset,
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        Epoch epoch,
        VectorClock epochVc,
        bool stored,
        CapturedStackTrace stack,
        InFlightAtomicAccess? operation = null)
    {
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = _threadClocks.GetOrCreate(threadId);
        _writeClassifier.NoteAccess(threadId, objectId);

        AccessRecord? conflict;
        if (operation != null)
        {
            conflict = FindCompletedOperationConflict(objectId, threadId, threadVc, operation, stored);
        }
        else
        {
            // Plain write and atomic access: a lost update, or a value that is not atomic at all
            conflict = HasUnorderedWrite(shadow, threadVc, objectId)
                ? GetConflictingWrite(fieldId, objectId, threadId)
                : null;

            if (stored && _configuration.ReportPlainReadsRacingWithAtomicWrites && !_writeClassifier.IsFinalizationQueued(objectId))
                conflict ??= FindReadConflict(fieldId, objectId, threadId, shadow, threadVc);
        }

        // What the thread knew at the access decides whether it subsumes earlier atomic accesses
        shadow.GetOrCreateAtomicAccesses().Record(epoch, epochVc, stored, _threadClocks);
        var currentAccess = _accessTracker.RecordAtomicAccess(fieldId, objectId, threadId, methodOffset, stored, stack);
        return CreateRaceInfo(threadId, fieldId, objectId, currentAccess, conflict);
    }

    private void BeginAtomicOperation(ProcessThreadId threadId, FieldId fieldId, ProcessTrackedObjectId? objectId)
    {
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = _threadClocks.GetOrCreate(threadId);
        var operation = shadow.GetOrCreateInFlightAtomicAccesses().Begin(threadId, threadVc.GetEpoch(threadId));

        if (!shadow.WriteEpoch.IsNone &&
            shadow.WriteEpoch.ThreadId != threadId &&
            shadow.LastWriteKind == WriteKind.Regular &&
            _accessTracker.TryGetLastWriteAccess(fieldId, objectId, out var lastWrite))
        {
            operation.WriteBefore = new PlainAccess(shadow.WriteEpoch, lastWrite);
        }

        if (_configuration.ReportPlainReadsRacingWithAtomicWrites)
        {
            operation.ReadEpochBefore = shadow.ReadEpoch;
            operation.ReadClockBefore = shadow.ReadVectorClock?.Clone();
            _accessTracker.CopyLastReadAccesses(fieldId, objectId, operation.GetOrCreateReadsBefore());
        }
    }

    private AccessRecord? FindCompletedOperationConflict(
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        VectorClock threadVc,
        InFlightAtomicAccess operation,
        bool stored)
    {
        if (_writeClassifier.IsFinalizationQueued(objectId))
            return null;

        if (operation.WriteBefore is { } writeBefore && !writeBefore.Epoch.HappensBefore(threadVc))
            return writeBefore.Record;

        if (operation.FindUnorderedWrite(threadVc) is { } unorderedWrite)
            return unorderedWrite;

        if (!stored)
            return operation.FindObservingWrite(threadVc);

        if (!_configuration.ReportPlainReadsRacingWithAtomicWrites)
            return null;

        if (FindUnorderedReader(threadId, operation.ReadEpochBefore, operation.ReadClockBefore, threadVc) is { } reader &&
            operation.ReadsBefore != null &&
            operation.ReadsBefore.TryGetValue(reader, out var racingRead))
        {
            return racingRead;
        }

        return operation.FindUnorderedRead(threadVc);
    }

    private AccessRecord? FindAtomicConflict(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        ShadowVariable shadow,
        VectorClock threadVc,
        bool storesOnly)
    {
        if (shadow.AtomicAccesses is not { } atomicAccesses || _writeClassifier.IsFinalizationQueued(objectId))
            return null;

        var racingThread = storesOnly
            ? atomicAccesses.FindUnorderedStore(threadId, threadVc)
            : atomicAccesses.FindUnorderedAccess(threadId, threadVc);

        return racingThread is { } racer &&
               _accessTracker.TryGetLastAtomicAccess(fieldId, objectId, racer, storesOnly, out var racingAccess)
            ? racingAccess
            : null;
    }

    private void AcquireTypeInitialization(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        if (objectId == null)
            _typeInitializations.Acquire(threadId, moduleId, fieldToken);
    }

    private bool HasUnorderedWrite(ShadowVariable shadow, VectorClock threadVc, ProcessTrackedObjectId? objectId)
        => !_writeClassifier.IsFinalizationQueued(objectId) &&
           !shadow.WriteEpoch.IsNone &&
           !shadow.WriteEpoch.HappensBefore(threadVc) &&
           shadow.LastWriteKind == WriteKind.Regular;

    private AccessRecord? FindWriteConflict(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        ShadowVariable shadow,
        VectorClock threadVc)
    {
        // Write-write race: the last write by another thread does not happen-before this write
        if (!shadow.WriteEpoch.IsNone &&
            shadow.WriteEpoch.ThreadId != threadId &&
            !shadow.WriteEpoch.HappensBefore(threadVc))
        {
            return GetConflictingWrite(fieldId, objectId, threadId);
        }

        // Read-write race: an earlier read does not happen-before this write
        return FindReadConflict(fieldId, objectId, threadId, shadow, threadVc);
    }

    private AccessRecord? FindReadConflict(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        ShadowVariable shadow,
        VectorClock threadVc)
    {
        return FindUnorderedReader(threadId, shadow, threadVc) is { } reader &&
               _accessTracker.TryGetLastReadAccess(fieldId, objectId, reader, out var racingRead)
            ? racingRead
            : null;
    }

    private static ProcessThreadId? FindUnorderedReader(ProcessThreadId writerThreadId, ShadowVariable shadow, VectorClock writerVc)
        => FindUnorderedReader(writerThreadId, shadow.ReadEpoch, shadow.ReadVectorClock, writerVc);

    private static ProcessThreadId? FindUnorderedReader(
        ProcessThreadId writerThreadId,
        Epoch readEpoch,
        VectorClock? readVc,
        VectorClock writerVc)
    {
        if (readVc != null)
            return readVc.FindRacingReader(writerVc);

        return !readEpoch.IsNone &&
               readEpoch.ThreadId != writerThreadId &&
               !readEpoch.HappensBefore(writerVc)
            ? readEpoch.ThreadId
            : null;
    }

    private AccessRecord? GetConflictingWrite(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId)
    {
        return _accessTracker.TryGetLastWriteAccess(fieldId, objectId, out var previous) && previous.ProcessThreadId != threadId
            ? previous
            : null;
    }

    private DataRaceInfo? CreateRaceInfo(
        ProcessThreadId threadId,
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        in AccessRecord currentAccess,
        AccessRecord? conflictingAccess)
    {
        if (conflictingAccess is not { } conflict)
            return null;

        return new DataRaceInfo(
            threadId.ProcessId,
            fieldId,
            objectId,
            _accessTracker.Materialize(currentAccess),
            _accessTracker.Materialize(conflict),
            _timeProvider.GetUtcNow().DateTime);
    }

    private void UpdateReadState(ProcessThreadId threadId, ShadowVariable shadow, VectorClock threadVc)
    {
        if (shadow.HasReadVectorClock)
        {
            shadow.ReadVectorClock!.SetClock(threadId, threadVc.GetClock(threadId));
        }
        else if (shadow.ReadEpoch.IsNone ||
                 shadow.ReadEpoch.ThreadId == threadId ||
                 shadow.ReadEpoch.HappensBefore(threadVc))
        {
            shadow.SetRead(new Epoch(threadId, threadVc.GetClock(threadId)));
        }
        else
        {
            var readVc = _threadClocks.CreateClock();
            readVc.SetClock(shadow.ReadEpoch.ThreadId, shadow.ReadEpoch.Clock);
            readVc.SetClock(threadId, threadVc.GetClock(threadId));
            shadow.ExpandReadToVectorClock(readVc);
        }
    }

    private bool TryResolveAnalyzedField(
        ProcessThreadId threadId,
        CapturedStackTrace stack,
        MdToken fieldToken,
        nuint declaringTypeHandle,
        out FieldId fieldId)
    {
        var moduleId = stack.Top.ModuleId;
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out var fieldFlags) ||
            FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration))
        {
            fieldId = default;
            return false;
        }

        fieldId = FieldId.Create(threadId.ProcessId, fieldDef!, declaringTypeHandle);
        return true;
    }

    private DataRaceInfo? CompleteAtomicOperation(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        CapturedStackTrace stack,
        bool stored)
    {
        var moduleId = stack.Top.ModuleId;
        AcquireTypeInitialization(threadId, moduleId, fieldToken, objectId);
        if (!TryResolveField(threadId.ProcessId, moduleId, fieldToken, declaringTypeHandle, out var fieldId, out var isAnalyzed))
            return null;

        var releaseVc = _volatileClocks.CompleteProvisionalRelease(fieldId, objectId, threadId, stored);
        _volatileClocks.Acquire(fieldId, objectId, threadId);
        if (!isAnalyzed)
            return null;

        InFlightAtomicAccess? operation = null;
        _shadowMemory.GetOrCreateVirgin(fieldId, objectId).InFlightAtomicAccesses?.TryComplete(threadId, out operation);

        // The operation is recorded at its release: acquirers of its value see that clock
        var accessVc = releaseVc ?? _threadClocks.GetOrCreate(threadId);
        return RecordAtomicAccess(threadId, methodOffset, fieldId, objectId, accessVc.GetEpoch(threadId), accessVc, stored, stack, operation);
    }

    private bool TryResolveField(
        uint processId,
        ModuleId moduleId,
        MdToken fieldToken,
        nuint declaringTypeHandle,
        out FieldId fieldId,
        out bool isAnalyzed)
    {
        if (!_fieldResolver.TryResolve(processId, moduleId, fieldToken, out var fieldDef, out var fieldFlags))
        {
            fieldId = default;
            isAnalyzed = false;
            return false;
        }

        fieldId = FieldId.Create(processId, fieldDef!, declaringTypeHandle);
        isAnalyzed = !FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration);
        return true;
    }
}
