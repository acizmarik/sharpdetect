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
    private readonly MethodResolver _methodResolver;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<ProcessThreadId, VectorClock> _threadClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, VectorClock> _lockClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, Queue<VectorClock>> _semaphoreClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, VectorClock> _eventClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, VectorClock> _taskClocks = [];
    private readonly Dictionary<FieldId, VectorClock> _staticVolatileClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, Dictionary<FieldId, VectorClock>> _instanceVolatileClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, ObjectEscapeState> _escapeStates = [];
    private readonly record struct ObjectEscapeState(ProcessThreadId Instantiator, bool Escaped);
    
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
        _methodResolver = new MethodResolver(metadataContext, logger);
    }

    public int GetTrackedFieldCount() => _shadowMemory.Count;
    public int GetTrackedThreadCount() => _threadClocks.Count;

    public void RecordThreadCreated(ProcessThreadId threadId)
    {
        if (!_threadClocks.ContainsKey(threadId))
        {
            var vc = new VectorClock();
            vc.SetClock(threadId, 1);
            _threadClocks[threadId] = vc;
        }
    }

    public void RecordThreadDestroyed(ProcessThreadId threadId)
    {
        // Keep the clock around for join operations; it will be cleaned up naturally
    }

    public void RecordGarbageCollectedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        _shadowMemory.RemoveTrackedObjects(processId, removedObjectIds);
        _accessTracker.RemoveTrackedObjects(processId, removedObjectIds);

        foreach (var objectId in removedObjectIds)
        {
            var processObjectId = new ProcessTrackedObjectId(processId, objectId);
            _lockClocks.Remove(processObjectId);
            _semaphoreClocks.Remove(processObjectId);
            _eventClocks.Remove(processObjectId);
            _taskClocks.Remove(processObjectId);
            _escapeStates.Remove(processObjectId);
            _instanceVolatileClocks.Remove(processObjectId);
        }
    }
    
    public void RecordThreadFork(ProcessThreadId parentThreadId, ProcessThreadId childThreadId)
    {
        var parentVc = GetOrCreateThreadClock(parentThreadId);
        var childVc = GetOrCreateThreadClock(childThreadId);
        childVc.Join(parentVc);
        parentVc.Increment(parentThreadId);
    }
    
    public void RecordThreadJoin(ProcessThreadId joinerThreadId, ProcessThreadId joinedThreadId)
    {
        var joinerVc = GetOrCreateThreadClock(joinerThreadId);
        var joinedVc = GetOrCreateThreadClock(joinedThreadId);
        joinerVc.Join(joinedVc);
        joinerVc.Increment(joinerThreadId);
    }

    public void RecordTaskScheduled(ProcessThreadId parentThreadId, ProcessTrackedObjectId taskId)
    {
        var parentVc = GetOrCreateThreadClock(parentThreadId);
        _taskClocks[taskId] = parentVc.Clone();
        parentVc.Increment(parentThreadId);
    }

    public void RecordTaskStarted(ProcessThreadId workerThreadId, ProcessTrackedObjectId taskId)
    {
        if (_taskClocks.TryGetValue(taskId, out var taskVc))
        {
            var workerVc = GetOrCreateThreadClock(workerThreadId);
            workerVc.Join(taskVc);
        }
    }

    public void RecordTaskCompleted(ProcessThreadId workerThreadId, ProcessTrackedObjectId taskId)
    {
        var workerVc = GetOrCreateThreadClock(workerThreadId);
        _taskClocks[taskId] = workerVc.Clone();
        workerVc.Increment(workerThreadId);
    }

    public void RecordTaskJoinFinished(ProcessThreadId waiterThreadId, ProcessTrackedObjectId taskId)
    {
        var waiterVc = GetOrCreateThreadClock(waiterThreadId);
        if (_taskClocks.TryGetValue(taskId, out var taskVc))
            waiterVc.Join(taskVc);

        waiterVc.Increment(waiterThreadId);
    }
    
    public void RecordLockAcquired(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var threadVc = GetOrCreateThreadClock(threadId);
        var lockVc = GetOrCreateLockClock(lockId);
        threadVc.Join(lockVc);
    }
    
    public void RecordLockReleased(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        var threadVc = GetOrCreateThreadClock(threadId);
        _lockClocks[lockId] = threadVc.Clone();
        threadVc.Increment(threadId);
    }

    public void RecordSemaphoreCreated(ProcessTrackedObjectId semaphoreId, int initialCount)
    {
        var pool = new Queue<VectorClock>(capacity: initialCount);
        for (var i = 0; i < initialCount; i++)
            pool.Enqueue(new VectorClock());
        _semaphoreClocks[semaphoreId] = pool;
    }

    public void RecordSemaphoreAcquired(ProcessThreadId threadId, ProcessTrackedObjectId semaphoreId)
    {
        var pool = GetOrCreateSemaphorePool(semaphoreId);
        if (pool.Count == 0)
            return;

        var threadVc = GetOrCreateThreadClock(threadId);
        var slotVc = pool.Dequeue();
        threadVc.Join(slotVc);
    }

    public void RecordSemaphoreReleased(ProcessThreadId threadId, ProcessTrackedObjectId semaphoreId, int releaseCount)
    {
        var threadVc = GetOrCreateThreadClock(threadId);
        var pool = GetOrCreateSemaphorePool(semaphoreId);
        for (var i = 0; i < releaseCount; i++)
            pool.Enqueue(threadVc.Clone());
        threadVc.Increment(threadId);
    }

    public void RecordEventCreated(ProcessTrackedObjectId eventId, bool initialState)
    {
        if (initialState)
            _eventClocks[eventId] = new VectorClock();
        else
            _eventClocks.Remove(eventId);
    }

    public void RecordEventSignaled(ProcessThreadId threadId, ProcessTrackedObjectId eventId)
    {
        var threadVc = GetOrCreateThreadClock(threadId);
        if (_eventClocks.TryGetValue(eventId, out var eventVc))
        {
            eventVc.Join(threadVc);
        }
        else
        {
            _eventClocks[eventId] = threadVc.Clone();
        }

        threadVc.Increment(threadId);
    }

    public void RecordEventReset(ProcessTrackedObjectId eventId)
    {
        _eventClocks.Remove(eventId);
    }

    public void RecordEventWaitReturned(ProcessThreadId threadId, ProcessTrackedObjectId eventId, bool isAutoReset)
    {
        if (!_eventClocks.TryGetValue(eventId, out var eventVc))
            return;

        var threadVc = GetOrCreateThreadClock(threadId);
        threadVc.Join(eventVc);

        if (isAutoReset)
            _eventClocks.Remove(eventId);
    }

    public void RecordObjectWaitCalled(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        RecordLockReleased(threadId, lockId);
    }
    
    public void RecordObjectWaitReturned(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        RecordLockAcquired(threadId, lockId);
    }

    public void RecordVolatileRead(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out _))
            return;

        var fieldId = new FieldId(threadId.ProcessId, moduleId, fieldToken, fieldDef!);
        var threadVc = GetOrCreateThreadClock(threadId);
        var volatileVc = GetOrCreateVolatileClock(fieldId, objectId);
        threadVc.Join(volatileVc);
    }

    public void RecordVolatileWrite(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out _))
            return;

        var fieldId = new FieldId(threadId.ProcessId, moduleId, fieldToken, fieldDef!);
        var threadVc = GetOrCreateThreadClock(threadId);
        var volatileVc = GetOrCreateVolatileClock(fieldId, objectId);
        volatileVc.Join(threadVc);
        GetVolatileClockMap(objectId)[fieldId] = threadVc.Clone();
        threadVc.Increment(threadId);
    }

    public DataRaceInfo? RecordRead(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        CapturedStackTrace stack)
    {
        var moduleId = stack.Top.ModuleId;
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out var fieldFlags) ||
            FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration))
        {
            return null;
        }

        var fieldId = new FieldId(threadId.ProcessId, moduleId, fieldToken, fieldDef!);
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = GetOrCreateThreadClock(threadId);

        _ = UpdateObjectPublicationState(threadId, objectId);

        if (!shadow.WriteEpoch.IsNone &&
            !shadow.WriteEpoch.HappensBefore(threadVc) &&
            shadow.LastWriteKind == WriteKind.Regular)
        {
            // Write-read race detected: last write does not happen-before this read
            var hasLastWrite = _accessTracker.TryGetLastWriteAccess(fieldId, objectId, out var lastWrite);
            var currentAccess = _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Read, stack);
            UpdateReadState(threadId, shadow, threadVc);

            if (hasLastWrite && lastWrite.ProcessThreadId != threadId)
            {
                return new DataRaceInfo(
                    threadId.ProcessId,
                    fieldId,
                    objectId,
                    _accessTracker.Materialize(currentAccess),
                    _accessTracker.Materialize(lastWrite),
                    _timeProvider.GetUtcNow().DateTime);
            }

            return null;
        }

        _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Read, stack);
        UpdateReadState(threadId, shadow, threadVc);
        return null;
    }
    
    public DataRaceInfo? RecordWrite(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        CapturedStackTrace stack)
    {
        var moduleId = stack.Top.ModuleId;
        var methodToken = stack.Top.MethodToken;
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out var fieldFlags) ||
            FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration))
        {
            return null;
        }

        var fieldId = new FieldId(threadId.ProcessId, moduleId, fieldToken, fieldDef!);
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = GetOrCreateThreadClock(threadId);
        var currentEpoch = threadVc.GetEpoch(threadId);
        var writeKind = ClassifyWrite(threadId, moduleId, methodToken, fieldFlags, objectId);

        var isInstantiationWrite = writeKind == WriteKind.Instantiation;
        if (!isInstantiationWrite &&
            !shadow.WriteEpoch.IsNone &&
            shadow.WriteEpoch.ThreadId != threadId &&
            !shadow.WriteEpoch.HappensBefore(threadVc))
        {
            var hasLastWrite = _accessTracker.TryGetLastWriteAccess(fieldId, objectId, out var lastWrite);
            var currentAccess = _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Write, stack);
            shadow.SetWrite(currentEpoch, writeKind);
            shadow.SetRead(Epoch.None);

            if (hasLastWrite && lastWrite.ProcessThreadId != threadId)
            {
                return new DataRaceInfo(
                    threadId.ProcessId,
                    fieldId,
                    objectId,
                    _accessTracker.Materialize(currentAccess),
                    _accessTracker.Materialize(lastWrite),
                    _timeProvider.GetUtcNow().DateTime);
            }

            return null;
        }

        var readWriteRace = isInstantiationWrite ? null : CheckReadWriteRace(threadId, shadow, threadVc);
        if (readWriteRace != null)
        {
            var hasLastAccess = _accessTracker.TryGetLastAccess(fieldId, objectId, out var lastAccess);
            var currentAccess = _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Write, stack);
            shadow.SetWrite(currentEpoch, writeKind);
            shadow.SetRead(Epoch.None);

            if (hasLastAccess && lastAccess.ProcessThreadId != threadId)
            {
                return new DataRaceInfo(
                    threadId.ProcessId,
                    fieldId,
                    objectId,
                    _accessTracker.Materialize(currentAccess),
                    _accessTracker.Materialize(lastAccess),
                    _timeProvider.GetUtcNow().DateTime);
            }

            return null;
        }

        _accessTracker.RecordAccess(fieldId, objectId, threadId, methodOffset, AccessType.Write, stack);
        shadow.SetWrite(currentEpoch, writeKind);
        shadow.SetRead(Epoch.None);
        return null;
    }
    
    private WriteKind ClassifyWrite(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        FieldFlags fieldFlags,
        ProcessTrackedObjectId? objectId)
    {
        var isStaticField = objectId == null;
        var constructorKind = _methodResolver.GetConstructorKind(threadId.ProcessId, moduleId, methodToken);
        var isInstantiatorExclusive = UpdateObjectPublicationState(threadId, objectId);

        if (isStaticField)
        {
            return constructorKind == ConstructorKind.Static
                ? WriteKind.Instantiation
                : WriteKind.Regular;
        }

        if (constructorKind != ConstructorKind.None)
            return WriteKind.Instantiation;

        // An auto-property is written via its compiler-generated setter
        // Treat it as initialization if object not escaped instantiation thread yet
        if (isInstantiatorExclusive && fieldFlags.HasFlag(FieldFlags.IsAutoPropertyBackingField))
            return WriteKind.MaybeInstantiation;

        return WriteKind.Regular;
    }
    
    private bool UpdateObjectPublicationState(ProcessThreadId threadId, ProcessTrackedObjectId? objectId)
    {
        if (objectId is not { } objId)
            return false;

        if (!_escapeStates.TryGetValue(objId, out var state))
        {
            _escapeStates[objId] = new ObjectEscapeState(threadId, Escaped: false);
            return true;
        }

        if (state.Escaped)
            return false;

        if (state.Instantiator == threadId)
            return true;

        _escapeStates[objId] = state with { Escaped = true };
        return false;
    }

    private static void UpdateReadState(ProcessThreadId threadId, ShadowVariable shadow, VectorClock threadVc)
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
            var readVc = new VectorClock();
            readVc.SetClock(shadow.ReadEpoch.ThreadId, shadow.ReadEpoch.Clock);
            readVc.SetClock(threadId, threadVc.GetClock(threadId));
            shadow.ExpandReadToVectorClock(readVc);
        }
    }
    
    private static ProcessThreadId? CheckReadWriteRace(
        ProcessThreadId writerThreadId,
        ShadowVariable shadow,
        VectorClock writerVc)
    {
        if (shadow.HasReadVectorClock)
            return shadow.ReadVectorClock!.FindRacingReader(writerVc);

        if (!shadow.ReadEpoch.IsNone &&
            shadow.ReadEpoch.ThreadId != writerThreadId &&
            !shadow.ReadEpoch.HappensBefore(writerVc))
        {
            return shadow.ReadEpoch.ThreadId;
        }

        return null;
    }

    private VectorClock GetOrCreateThreadClock(ProcessThreadId threadId)
    {
        if (!_threadClocks.TryGetValue(threadId, out var vc))
        {
            vc = new VectorClock();
            vc.SetClock(threadId, 1);
            _threadClocks[threadId] = vc;
        }

        return vc;
    }

    private Queue<VectorClock> GetOrCreateSemaphorePool(ProcessTrackedObjectId semaphoreId)
    {
        if (!_semaphoreClocks.TryGetValue(semaphoreId, out var pool))
        {
            pool = new Queue<VectorClock>();
            _semaphoreClocks[semaphoreId] = pool;
        }

        return pool;
    }

    private VectorClock GetOrCreateLockClock(ProcessTrackedObjectId lockId)
    {
        if (!_lockClocks.TryGetValue(lockId, out var vc))
        {
            vc = new VectorClock();
            _lockClocks[lockId] = vc;
        }

        return vc;
    }


    private VectorClock GetOrCreateVolatileClock(FieldId fieldId, ProcessTrackedObjectId? objectId)
    {
        var clocks = GetVolatileClockMap(objectId);
        if (!clocks.TryGetValue(fieldId, out var vc))
        {
            vc = new VectorClock();
            clocks[fieldId] = vc;
        }

        return vc;
    }

    private Dictionary<FieldId, VectorClock> GetVolatileClockMap(ProcessTrackedObjectId? objectId)
    {
        if (objectId is not { } objId)
            return _staticVolatileClocks;

        if (!_instanceVolatileClocks.TryGetValue(objId, out var clocks))
        {
            clocks = [];
            _instanceVolatileClocks[objId] = clocks;
        }

        return clocks;
    }
}


