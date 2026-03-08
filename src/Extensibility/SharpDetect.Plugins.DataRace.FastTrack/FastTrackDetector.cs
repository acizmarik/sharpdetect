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
    private readonly Func<ProcessThreadId, string?> _threadNameResolver;
    private readonly Dictionary<ProcessThreadId, VectorClock> _threadClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, VectorClock> _lockClocks = [];

    public FastTrackDetector(
        FastTrackPluginConfiguration configuration,
        IMetadataContext metadataContext,
        TimeProvider timeProvider,
        ILogger logger,
        Func<ProcessThreadId, string?> threadNameResolver)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;
        _threadNameResolver = threadNameResolver;
        _accessTracker = new AccessTracker(timeProvider, threadNameResolver);
        _fieldResolver = new FieldResolver(metadataContext, logger);
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
    
    public void RecordObjectWaitCalled(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        RecordLockReleased(threadId, lockId);
    }
    
    public void RecordObjectWaitReturned(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        RecordLockAcquired(threadId, lockId);
    }

    public DataRaceInfo? RecordRead(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out var fieldFlags) ||
            FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration))
        {
            return null;
        }

        var fieldId = new FieldId(threadId.ProcessId, fieldDef!);
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = GetOrCreateThreadClock(threadId);
        var previousState = shadow.GetStateDescription(_threadNameResolver);
        
        if (!shadow.WriteEpoch.IsNone && 
            !shadow.WriteEpoch.HappensBefore(threadVc) && 
            shadow.ExclusiveWriteThread == null)
        {
            // Write-read race detected: last write does not happen-before this read
            var accessType = AccessType.Read;
            var currentAccess = _accessTracker.CreateAccessInfo(threadId, moduleId, methodToken, accessType);
            var lastAccess = _accessTracker.GetLastWriteAccess(fieldId, objectId);
            _accessTracker.RecordAccess(fieldId, objectId, currentAccess);
            UpdateReadState(threadId, shadow, threadVc);
            var newState = shadow.GetStateDescription(_threadNameResolver);

            if (lastAccess != null && lastAccess.ProcessThreadId != threadId)
            {
                return new DataRaceInfo(
                    threadId.ProcessId,
                    fieldId,
                    objectId,
                    currentAccess,
                    lastAccess,
                    previousState,
                    newState,
                    _timeProvider.GetUtcNow().DateTime);
            }
        }

        var readAccess = _accessTracker.CreateAccessInfo(threadId, moduleId, methodToken, AccessType.Read);
        _accessTracker.RecordAccess(fieldId, objectId, readAccess);
        UpdateReadState(threadId, shadow, threadVc);
        return null;
    }
    
    public DataRaceInfo? RecordWrite(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out var fieldFlags) ||
            FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration))
        {
            return null;
        }

        var fieldId = new FieldId(threadId.ProcessId, fieldDef!);
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadVc = GetOrCreateThreadClock(threadId);
        var previousState = shadow.GetStateDescription(_threadNameResolver);
        var currentEpoch = threadVc.GetEpoch(threadId);

        if (!shadow.WriteEpoch.IsNone && 
            shadow.WriteEpoch.ThreadId != threadId && 
            !shadow.WriteEpoch.HappensBefore(threadVc))
        {
            var currentAccess = _accessTracker.CreateAccessInfo(threadId, moduleId, methodToken, AccessType.Write);
            var lastAccess = _accessTracker.GetLastWriteAccess(fieldId, objectId);
            _accessTracker.RecordAccess(fieldId, objectId, currentAccess);
            shadow.SetWrite(currentEpoch);
            shadow.SetRead(Epoch.None);
            var newState = shadow.GetStateDescription(_threadNameResolver);

            if (lastAccess != null && lastAccess.ProcessThreadId != threadId)
            {
                return new DataRaceInfo(
                    threadId.ProcessId,
                    fieldId,
                    objectId,
                    currentAccess,
                    lastAccess,
                    previousState,
                    newState,
                    _timeProvider.GetUtcNow().DateTime);
            }
        }

        var readWriteRace = CheckReadWriteRace(threadId, shadow, threadVc);
        if (readWriteRace != null)
        {
            var currentAccess = _accessTracker.CreateAccessInfo(threadId, moduleId, methodToken, AccessType.Write);
            var lastAccess = _accessTracker.GetLastAccess(fieldId, objectId);
            _accessTracker.RecordAccess(fieldId, objectId, currentAccess);
            shadow.SetWrite(currentEpoch);
            shadow.SetRead(Epoch.None);
            var newState = shadow.GetStateDescription(_threadNameResolver);

            if (lastAccess != null && lastAccess.ProcessThreadId != threadId)
            {
                return new DataRaceInfo(
                    threadId.ProcessId,
                    fieldId,
                    objectId,
                    currentAccess,
                    lastAccess,
                    previousState,
                    newState,
                    _timeProvider.GetUtcNow().DateTime);
            }
        }

        var writeAccess = _accessTracker.CreateAccessInfo(threadId, moduleId, methodToken, AccessType.Write);
        _accessTracker.RecordAccess(fieldId, objectId, writeAccess);
        shadow.SetWrite(currentEpoch);
        shadow.SetRead(Epoch.None);
        return null;
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

    private VectorClock GetOrCreateLockClock(ProcessTrackedObjectId lockId)
    {
        if (!_lockClocks.TryGetValue(lockId, out var vc))
        {
            vc = new VectorClock();
            _lockClocks[lockId] = vc;
        }

        return vc;
    }
}


