// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace SharpDetect.Plugins.DataRace.Eraser;

internal sealed class EraserDetector
{
    private readonly EraserPluginConfiguration _configuration;
    private readonly ShadowMemory _shadowMemory = new();
    private readonly LockSetTable _lockSetTable = new();
    private readonly ThreadLockSetTracker _threadLockSetTracker;
    private readonly AccessTracker _accessTracker;
    private readonly FieldResolver _fieldResolver;
    private readonly EraserStateMachine _stateMachine;
    private readonly TimeProvider _timeProvider;

    public EraserDetector(
        EraserPluginConfiguration configuration,
        IMetadataContext metadataContext,
        TimeProvider timeProvider,
        ILogger logger,
        Func<ProcessThreadId, string?> threadNameResolver)
    {
        _configuration = configuration;
        _timeProvider = timeProvider;

        _threadLockSetTracker = new ThreadLockSetTracker(_lockSetTable);
        _accessTracker = new AccessTracker(threadNameResolver);
        _fieldResolver = new FieldResolver(metadataContext, logger);
        _stateMachine = new EraserStateMachine(_lockSetTable);
    }

    public int GetDistinctLockSetCount() => _lockSetTable.Count;
    public int GetTrackedFieldCount() => _shadowMemory.Count;
    
    public void RecordThreadCreated(ProcessThreadId threadId)
    {
        _threadLockSetTracker.RegisterThread(threadId);
    }

    public void RecordThreadDestroyed(ProcessThreadId threadId)
    {
        _threadLockSetTracker.UnregisterThread(threadId);
    }

    public void RecordGarbageCollectedObjects(uint processId, ReadOnlySpan<TrackedObjectId> removedObjectIds)
    {
        _shadowMemory.RemoveTrackedObjects(processId, removedObjectIds);
        _accessTracker.RemoveTrackedObjects(processId, removedObjectIds);
    }

    public void RecordLockAcquired(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        _threadLockSetTracker.AcquireLock(threadId, lockId);
    }

    public void RecordLockReleased(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        _threadLockSetTracker.ReleaseLock(threadId, lockId);
    }

    public void RecordObjectWaitCalled(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        _threadLockSetTracker.ReleaseLock(threadId, lockId);
    }

    public void RecordObjectWaitReturned(ProcessThreadId threadId, ProcessTrackedObjectId lockId)
    {
        _threadLockSetTracker.AcquireLock(threadId, lockId);
    }

    public DataRaceInfo? RecordRead(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        return RecordFieldAccess(threadId, moduleId, methodToken, methodOffset, fieldToken, objectId, isWrite: false);
    }

    public DataRaceInfo? RecordWrite(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId)
    {
        return RecordFieldAccess(threadId, moduleId, methodToken, methodOffset, fieldToken, objectId, isWrite: true);
    }

    private DataRaceInfo? RecordFieldAccess(
        ProcessThreadId threadId,
        ModuleId moduleId,
        MdMethodDef methodToken,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        bool isWrite)
    {
        // Resolve the field
        if (!_fieldResolver.TryResolve(threadId.ProcessId, moduleId, fieldToken, out var fieldDef, out var fieldFlags))
            return null;

        // Skip fields that cannot have data races
        if (FieldResolver.ShouldExcludeFromAnalysis(fieldFlags, _configuration))
            return null;

        // Get or create shadow variable for this field
        var fieldId = new FieldId(threadId.ProcessId, moduleId, fieldToken, fieldDef!);
        var shadow = _shadowMemory.GetOrCreateVirgin(fieldId, objectId);
        var threadLockSet = _threadLockSetTracker.GetLockSet(threadId);

        // Update shadow variable, state transition
        var transitionResult = _stateMachine.ComputeTransition(threadId, shadow, threadLockSet, isWrite);
        _shadowMemory.Update(fieldId, objectId, transitionResult.NewShadow);
        var accessType = isWrite ? AccessType.Write : AccessType.Read;
        var hasLastRelevant = isWrite
            ? _accessTracker.TryGetLastAccess(fieldId, objectId, out var lastRelevantAccess)
            : _accessTracker.TryGetLastWriteAccess(fieldId, objectId, out lastRelevantAccess);
        var currentAccess = _accessTracker.RecordAccess(fieldId, objectId, threadId, moduleId, methodToken, methodOffset, accessType);
        if (!transitionResult.IsRaceDetected ||
            !hasLastRelevant ||
            lastRelevantAccess.ProcessThreadId == threadId)
            return null;

        return new DataRaceInfo(
            threadId.ProcessId,
            fieldId,
            objectId,
            _accessTracker.Materialize(currentAccess),
            _accessTracker.Materialize(lastRelevantAccess),
            Timestamp: _timeProvider.GetUtcNow().DateTime);
    }
}
