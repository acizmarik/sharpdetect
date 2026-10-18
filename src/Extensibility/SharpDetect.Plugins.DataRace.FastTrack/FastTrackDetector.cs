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

    public void RecordTaskJoinFailed(ProcessThreadId waiterThreadId, ProcessTrackedObjectId taskId)
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

    public void RecordVolatileRead(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        AcquireTypeInitialization(threadId, moduleId, fieldToken, objectId);
        if (TryResolveField(threadId.ProcessId, moduleId, fieldToken, out var fieldId))
            _volatileClocks.Acquire(fieldId, objectId, threadId);
    }

    public void RecordVolatileWrite(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        AcquireTypeInitialization(threadId, moduleId, fieldToken, objectId);
        if (TryResolveField(threadId.ProcessId, moduleId, fieldToken, out var fieldId))
            _volatileClocks.Release(fieldId, objectId, threadId);
    }

    public void RecordAtomicReadModifyWrite(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        RecordVolatileRead(threadId, moduleId, fieldToken, objectId);
        RecordVolatileWrite(threadId, moduleId, fieldToken, objectId);
    }

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
        CapturedStackTrace stack)
    {
        AcquireTypeInitialization(threadId, stack.Top.ModuleId, fieldToken, objectId);
        if (!TryResolveAnalyzedField(threadId, stack, fieldToken, out var fieldId))
            return null;

        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = _threadClocks.GetOrCreate(threadId);
        _writeClassifier.NoteAccess(threadId, objectId);

        // Write-read race: the last write does not happen-before this read
        var conflict = HasUnorderedWrite(shadow, threadVc, objectId)
            ? GetConflictingWrite(fieldId, objectId, threadId)
            : null;

        var currentAccess = _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Read, stack);
        UpdateReadState(threadId, shadow, threadVc);
        return CreateRaceInfo(threadId, fieldId, objectId, currentAccess, conflict);
    }

    public DataRaceInfo? RecordWrite(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        CapturedStackTrace stack)
    {
        AcquireTypeInitialization(threadId, stack.Top.ModuleId, fieldToken, objectId);
        if (!TryResolveAnalyzedField(threadId, stack, fieldToken, out var fieldId))
            return null;

        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = _threadClocks.GetOrCreate(threadId);
        var currentEpoch = threadVc.GetEpoch(threadId);
        var writeKind = _writeClassifier.Classify(threadId, fieldId.FieldDef, objectId, stack);

        var conflict = writeKind == WriteKind.Regular
            ? FindWriteConflict(fieldId, objectId, threadId, shadow, threadVc)
            : null;

        var currentAccess = _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Write, stack);
        shadow.SetWrite(currentEpoch, writeKind);
        shadow.SetRead(Epoch.None);
        return CreateRaceInfo(threadId, fieldId, objectId, currentAccess, conflict);
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
        return FindUnorderedReader(threadId, shadow, threadVc) is { } reader &&
               _accessTracker.TryGetLastReadAccess(fieldId, objectId, reader, out var racingRead)
            ? racingRead
            : null;
    }

    private static ProcessThreadId? FindUnorderedReader(ProcessThreadId writerThreadId, ShadowVariable shadow, VectorClock writerVc)
    {
        if (shadow.HasReadVectorClock)
            return shadow.ReadVectorClock!.FindRacingReader(writerVc);

        return !shadow.ReadEpoch.IsNone &&
               shadow.ReadEpoch.ThreadId != writerThreadId &&
               !shadow.ReadEpoch.HappensBefore(writerVc)
            ? shadow.ReadEpoch.ThreadId
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
        out FieldId fieldId)
    {
        var moduleId = stack.Top.ModuleId;
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out var fieldFlags) ||
            FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration))
        {
            fieldId = default;
            return false;
        }

        fieldId = new FieldId(threadId.ProcessId, moduleId, fieldToken, fieldDef!);
        return true;
    }

    private bool TryResolveField(uint processId, ModuleId moduleId, MdToken fieldToken, out FieldId fieldId)
    {
        if (!_fieldResolver.TryResolve(processId, moduleId, fieldToken, out var fieldDef, out _))
        {
            fieldId = default;
            return false;
        }

        fieldId = new FieldId(processId, moduleId, fieldToken, fieldDef!);
        return true;
    }
}
