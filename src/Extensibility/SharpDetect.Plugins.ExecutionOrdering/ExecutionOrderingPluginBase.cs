// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Serialization;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Loader;
using SharpDetect.Plugins.PerThreadOrdering;

namespace SharpDetect.Plugins.ExecutionOrdering;

public abstract class ExecutionOrderingPluginBase : PerThreadOrderingPluginBase, IExecutionOrderingPlugin
{
    private readonly IRecordedEventsDeliveryContext _eventsDeliveryContext;
    private readonly LockRegistry _lockRegistry = new();
    private readonly MonitorWaitReentrancyTracker _reentrancyTracker = new();

    protected ExecutionOrderingPluginBase(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IRecordedEventsDeliveryContext eventsDeliveryContext,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        TimeProvider timeProvider,
        ILogger logger)
        : base(moduleBindContext, metadataContext, argumentsParser, profilerCommandSenderProvider, timeProvider, logger)
    {
        _eventsDeliveryContext = eventsDeliveryContext;
    }
    
    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectedTrackedObjectsRecordedEvent args)
    {
        _lockRegistry.RemoveRange(metadata.Pid, args.RemovedTrackedObjectIds);
        base.Visit(metadata, args);
    }
    
    protected override void OnBeforeWaitAttempt(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
        var lockObj = GetOrAddShadowLock(lockId);
        var reentrancyCount = lockObj.ReleaseAll(id);
        _reentrancyTracker.PushReentrancyCount(id, reentrancyCount);
    }
    
    protected override void OnAfterWaitReturn(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
        var lockObj = GetShadowLock(lockId);
        var reentrancyCount = _reentrancyTracker.PopReentrancyCount(id);
        lockObj.AcquireMultiple(id, reentrancyCount);
    }
    
    protected override void ProcessLockAcquire(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef functionToken,
        ProcessTrackedObjectId lockId)
    {
        var lockObj = GetOrAddShadowLock(lockId);
        
        if (TryConsumeOrDeferLockAcquire(id, lockObj))
        {
            lockObj.Acquire(id);
            base.ProcessLockAcquire(id, moduleId, functionToken, lockId);
        }
    }

    protected override void ProcessLockRelease(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        var lockObj = GetShadowLock(lockId);
        lockObj.Release(id);
        base.ProcessLockRelease(id, moduleId, methodToken, lockId);
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    protected override void ProcessPulseOne(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        var lockObj = GetShadowLock(lockId);
        if (!_eventsDeliveryContext.SignalOneThreadWaitingForObjectPulse(lockObj))
            Logger.LogWarning("No threads were waiting on lock {LockId} for pulse one.", lockId.ObjectId.Value);
        
        base.ProcessPulseOne(id, moduleId, methodToken, lockId);
    }

    protected override void ProcessPulseAll(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        var lockObj = GetShadowLock(lockId);
        if (!_eventsDeliveryContext.SignalAllThreadsWaitingForObjectPulse(lockObj))
            Logger.LogWarning("No threads were waiting on lock {LockId} for pulse all.", lockId.ObjectId.Value);
        
        base.ProcessPulseAll(id, moduleId, methodToken, lockId);
    }

    protected override void ProcessWaitAttempt(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        var lockObj = GetShadowLock(lockId);
        _eventsDeliveryContext.RegisterThreadWaitingForObjectPulse(id, lockObj);
        base.ProcessWaitAttempt(id, moduleId, methodToken, lockId);
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    protected override void ProcessWaitReturn(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId,
        bool success)
    {
        var lockObj = GetShadowLock(lockId);
        
        if (TryConsumeOrDeferWaitReturn(id, lockObj, success))
            base.ProcessWaitReturn(id, moduleId, methodToken, lockId, success);
    }

    protected override void ProcessThreadStart(ProcessThreadId id, ProcessTrackedObjectId processThreadObjectId)
    {
        base.ProcessThreadStart(id, processThreadObjectId);
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForThreadStart(processThreadObjectId);
    }

    protected override void ProcessThreadMapping(ProcessThreadId id, ProcessTrackedObjectId processThreadObjectId)
    {
        base.ProcessThreadMapping(id, processThreadObjectId);
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForThreadStart(processThreadObjectId);
    }

    protected override void ProcessThreadJoinAttempt(
        ProcessThreadId id,
        ProcessTrackedObjectId processJoinedThreadObjectId,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        if (TryGetThreadId(processJoinedThreadObjectId, out var joiningProcessThreadId))
        {
            RaiseThreadJoinAttempted(new ThreadJoinAttemptArgs(id, joiningProcessThreadId, moduleId, methodToken));
        }
        else
        {
            // Thread mapping not yet received - defer until we have it
            _eventsDeliveryContext.BlockEventsDeliveryForThreadWaitingForThreadStart(id, processJoinedThreadObjectId);
        }
    }
    
    private ShadowLock GetOrAddShadowLock(ProcessTrackedObjectId lockId)
        => _lockRegistry.GetOrAdd(lockId);

    private ShadowLock GetShadowLock(ProcessTrackedObjectId lockId)
        => _lockRegistry.Get(lockId);
    
    private bool TryConsumeOrDeferLockAcquire(ProcessThreadId processThreadId, ShadowLock lockObj)
    {
        if (lockObj.Owner is null || lockObj.Owner.Value == processThreadId)
            return true;
        
        _eventsDeliveryContext.BlockEventsDeliveryForThreadWaitingForObject(processThreadId, lockObj);
        return false;
    }

    private bool TryConsumeOrDeferWaitReturn(ProcessThreadId processThreadId, ShadowLock lockObj, bool success)
    {
        var canReacquireLock = lockObj.Owner is null || lockObj.Owner.Value == processThreadId;
        var isWaitingForPulse = _eventsDeliveryContext.IsWaitingForObjectPulse(processThreadId, lockObj);
        
        if (success && !isWaitingForPulse && canReacquireLock)
            return true;
        
        if (!success && canReacquireLock)
        {
            _eventsDeliveryContext.UnregisterThreadWaitingForObjectPulse(processThreadId, lockObj);
            return true;
        }

        _eventsDeliveryContext.BlockEventsDeliveryForThreadWaitingForObject(processThreadId, lockObj);
        return false;
    }
    
    internal RuntimeStateSnapshot DumpRuntimeState()
    {
        return new RuntimeStateSnapshot(
            Callstacks: GetCallstacksSnapshot(),
            Threads: GetTrackedThreadIds(),
            Locks: _lockRegistry.GetAllLocks());
    }
    
    internal record RuntimeStateSnapshot(
        IReadOnlyDictionary<ProcessThreadId, Callstack> Callstacks,
        IReadOnlySet<ProcessThreadId> Threads,
        IReadOnlySet<ShadowLock> Locks);
}
