// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Serialization;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Loader;

namespace SharpDetect.Plugins;

public abstract class HappensBeforeOrderingPluginBase : PluginBase
{
    public readonly record struct LockAcquireAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ShadowLock LockObj);
    public readonly record struct LockAcquireResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ShadowLock LockObj, bool IsSuccess);
    public readonly record struct LockReleaseArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ShadowLock LockObj);
    public readonly record struct ObjectPulseOneArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ShadowLock LockObj);
    public readonly record struct ObjectPulseAllArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ShadowLock LockObj);
    public readonly record struct ObjectWaitAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ShadowLock LockObj);
    public readonly record struct ObjectWaitResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ShadowLock LockObj, bool IsSuccess);
    public readonly record struct ThreadStartArgs(ProcessThreadId ProcessThreadId, TrackedObjectId ThreadObjectId);
    public readonly record struct ThreadMappingArgs(ProcessThreadId ProcessThreadId, TrackedObjectId ThreadObjectId);
    public readonly record struct ThreadJoinAttemptArgs(ProcessThreadId BlockedProcessThreadId, ProcessThreadId JoiningProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken);
    public readonly record struct ThreadJoinResultArgs(ProcessThreadId BlockedProcessThreadId, ProcessThreadId JoinedProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, bool IsSuccess);

    public event Action<LockAcquireAttemptArgs>? LockAcquireAttempted;
    public event Action<LockAcquireResultArgs>? LockAcquireReturned;
    public event Action<LockReleaseArgs>? LockReleased;
    public event Action<ObjectPulseOneArgs>? ObjectPulsedOne;
    public event Action<ObjectPulseAllArgs>? ObjectPulsedAll;
    public event Action<ObjectWaitAttemptArgs>? ObjectWaitAttempted;
    public event Action<ObjectWaitResultArgs>? ObjectWaitReturned;
    public event Action<ThreadStartArgs>? ThreadStarted;
    public event Action<ThreadMappingArgs>? ThreadMappingUpdated;
    public event Action<ThreadJoinAttemptArgs>? ThreadJoinAttempted;
    public event Action<ThreadJoinResultArgs>? ThreadJoinReturned;

    private readonly ThreadCallStackTracker _callStackTracker = new();
    private readonly ThreadObjectRegistry _threadObjectRegistry = new();
    private readonly MonitorWaitReentrancyTracker _reentrancyTracker = new();
    private readonly LockRegistry _lockRegistry = new();
    private readonly IMetadataContext _metadataContext;
    private readonly IArgumentsParser _argumentsParser;
    private readonly IRecordedEventsDeliveryContext _eventsDeliveryContext;

    protected HappensBeforeOrderingPluginBase(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IRecordedEventsDeliveryContext eventsDeliveryContext,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        TimeProvider timeProvider,
        ILogger logger)
        : base(moduleBindContext, profilerCommandSenderProvider, timeProvider, logger)
    {
        _metadataContext = metadataContext;
        _argumentsParser = argumentsParser;
        _eventsDeliveryContext = eventsDeliveryContext;
    }
    
    [RecordedEventBind((ushort)RecordedEventType.LockAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.LockTryAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockTryAcquire)]
    public void OnLockAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        var lockObj = GetOrAddLockFromArguments(id, arguments);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.LockAcquireResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void OnLockAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken: true);

    [RecordedEventBind((ushort)RecordedEventType.LockAcquireResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void OnLockAcquireExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var hasReturnValue = args.ReturnValue.Length > 0;
        var byRefArguments = args.ByRefArgumentValues.Length > 0 ? ParseArguments(metadata, args) : default;
        
        bool lockTaken;
        if (hasReturnValue)
            lockTaken = MemoryMarshal.Read<bool>(args.ReturnValue);
        else if (byRefArguments != default)
            lockTaken = (bool)byRefArguments[0].Value.AsT0;
        else
            lockTaken = true;
        
        HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken);
    }

    private void HandleLockAcquireExit(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef functionToken, bool lockTaken)
    {
        var id = GetProcessThreadId(metadata);
        var frame = _callStackTracker.Peek(id);
        EnsureCallStackIntegrity(frame, moduleId, functionToken);
        var lockObj = GetLockFromFrame(id, frame);
        
        if (!lockTaken)
        {
            _callStackTracker.Pop(id);
            LockAcquireReturned?.Invoke(new(id, moduleId, functionToken, lockObj, false));
            return;
        }

        if (TryConsumeOrDeferLockAcquire(id, lockObj))
            LockAcquireReturned?.Invoke(new(id, moduleId, functionToken, lockObj, true));
    }

    [RecordedEventBind((ushort)RecordedEventType.LockRelease)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockRelease)]
    public void OnLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.LockReleaseResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void OnLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockObj) = PopLockObjectFromCallStack(metadata, args);
        lockObj.Release(id);
        LockReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneAttempt)]
    public void OnMonitorPulseOneAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneResult)]
    public void OnMonitorPulseOneResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockObj) = PopLockObjectFromCallStack(metadata, args);
        if (!_eventsDeliveryContext.SignalOneThreadWaitingForObjectPulse(lockObj))
        {
            Logger.LogWarning("No threads were waiting on lock {LockId} for pulse one.", lockObj.LockObjectId.ObjectId.Value);
        }
        ObjectPulsedOne?.Invoke(new ObjectPulseOneArgs(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllAttempt)]
    public void OnMonitorPulseAllAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllResult)]
    public void OnMonitorPulseAllResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockObj) = PopLockObjectFromCallStack(metadata, args);
        if (!_eventsDeliveryContext.SignalAllThreadsWaitingForObjectPulse(lockObj))
        {
            Logger.LogWarning("No threads were waiting on lock {LockId} for pulse all.", lockObj.LockObjectId.ObjectId.Value);
        }
        ObjectPulsedAll?.Invoke(new ObjectPulseAllArgs(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitAttempt)]
    public void OnMonitorWaitAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        var lockObj = GetOrAddLockFromArguments(id, arguments);
        var reentrancyCount = lockObj.ReleaseAll(id);
        
        _reentrancyTracker.PushReentrancyCount(id, reentrancyCount);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        _eventsDeliveryContext.RegisterThreadWaitingForObjectPulse(id, lockObj);
        ObjectWaitAttempted?.Invoke(new ObjectWaitAttemptArgs(id, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitResult)]
    public void OnMonitorWaitResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var frame = _callStackTracker.Peek(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var lockObj = GetLockFromFrame(id, frame);
        var success = MemoryMarshal.Read<bool>(args.ReturnValue);
        var reentrancyCount = _reentrancyTracker.PeekReentrancyCount(id);
        
        if (TryConsumeOrDeferWaitReturn(id, lockObj, reentrancyCount, success))
        {
            _reentrancyTracker.PopReentrancyCount(id);
            ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(id, args.ModuleId, args.MethodToken, lockObj, success));
        }
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadStart)]
    public void OnThreadStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        var processThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        
        _threadObjectRegistry.RegisterMapping(processThreadObjectId, id);
        ThreadStarted?.Invoke(new ThreadStartArgs(id, processThreadObjectId.ObjectId));
        Logger.LogInformation("Thread started {Name}.", Threads[id]);
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForThreadStart(processThreadObjectId);
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadMapping)]
    public void OnThreadMapping(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var threadObjectId = new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ReturnValue));
        var processThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, threadObjectId);
        
        _threadObjectRegistry.RegisterMapping(processThreadObjectId, id);
        ThreadMappingUpdated?.Invoke(new ThreadMappingArgs(id, threadObjectId));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForThreadStart(processThreadObjectId);
    }
    
    [RecordedEventBind((ushort)RecordedEventType.ThreadJoinAttempt)]
    public void OnThreadJoinAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        var joinedThreadObjectId = arguments[0].Value.AsT1;
        var processJoinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, joinedThreadObjectId);
        
        if (TryConsumeOrDeferThreadJoin(id, processJoinedThreadObjectId, out var processJoinedThreadId))
        {
            _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
            ThreadJoinAttempted?.Invoke(new ThreadJoinAttemptArgs(id, processJoinedThreadId, args.ModuleId, args.MethodToken));
        }
    }
    
    [RecordedEventBind((ushort)RecordedEventType.ThreadJoinResult)]
    public void OnThreadJoinResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var joinedThreadObjectId = frame.Arguments!.Value[0].Value.AsT1;
        var processJoinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, joinedThreadObjectId);
        var processJoinedThreadId = _threadObjectRegistry.GetThreadId(processJoinedThreadObjectId);
        
        ThreadJoinReturned?.Invoke(new ThreadJoinResultArgs(id, processJoinedThreadId, args.ModuleId, args.MethodToken, IsSuccess: true));
    }

    private ShadowLock GetOrAddLockFromArguments(ProcessThreadId processThreadId, RuntimeArgumentList arguments)
    {
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(processThreadId.ProcessId, lockObjectId);
        return _lockRegistry.GetOrAdd(processLockObjectId);
    }
    
    private ShadowLock GetLockFromFrame(ProcessThreadId processThreadId, StackFrame frame)
    {
        var lockObjectId = frame.Arguments!.Value[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(processThreadId.ProcessId, lockObjectId);
        return _lockRegistry.Get(processLockObjectId);
    }
    
    private bool TryConsumeOrDeferLockAcquire(ProcessThreadId processThreadId, ShadowLock lockObj)
    {
        if (lockObj.Owner is null || lockObj.Owner.Value == processThreadId)
        {
            // It can be executed right away
            lockObj.Acquire(processThreadId);
            _callStackTracker.Pop(processThreadId);
            return true;
        }
        
        // It needs to be deferred
        _eventsDeliveryContext.BlockEventsDeliveryForThreadWaitingForObject(processThreadId, lockObj);
        return false;
    }

    private bool TryConsumeOrDeferWaitReturn(ProcessThreadId processThreadId, ShadowLock lockObj, int reentrancyCount, bool success)
    {
        var canReacquireLock = lockObj.Owner is null || lockObj.Owner.Value == processThreadId;
        var isWaitingForPulse = _eventsDeliveryContext.IsWaitingForObjectPulse(processThreadId, lockObj);
        
        if (success && !isWaitingForPulse && canReacquireLock)
        {
            // It can be executed right away (Monitor.Wait SUCCEEDED)
            lockObj.AcquireMultiple(processThreadId, reentrancyCount);
            _callStackTracker.Pop(processThreadId);
            return true;
        }
        if (!success && canReacquireLock)
        {
            // It can be executed right away (Monitor.Wait FAILED)
            lockObj.AcquireMultiple(processThreadId, reentrancyCount);
            _eventsDeliveryContext.UnregisterThreadWaitingForObjectPulse(processThreadId, lockObj);
            _callStackTracker.Pop(processThreadId);
            return true;
        }

        // It needs to be deferred
        _eventsDeliveryContext.BlockEventsDeliveryForThreadWaitingForObject(processThreadId, lockObj);
        return false;
    }
    
    private bool TryConsumeOrDeferThreadJoin(
        ProcessThreadId processThreadId,
        ProcessTrackedObjectId processJoiningThreadObjectId,
        out ProcessThreadId joiningProcessThreadId)
    {
        if (_threadObjectRegistry.TryGetThreadId(processJoiningThreadObjectId, out joiningProcessThreadId))
        {
            // It can be executed right away
            return true;
        }

        // It needs to be deferred
        joiningProcessThreadId = default;
        _eventsDeliveryContext.BlockEventsDeliveryForThreadWaitingForThreadStart(processThreadId, processJoiningThreadObjectId);
        return false;
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        _callStackTracker.InitializeCallStack(id);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ModuleLoadRecordedEvent args)
    {
        ModuleBindContext.LoadModule(metadata, args.ModuleId, args.Path);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodWrapperInjectionRecordedEvent args)
    {
        _metadataContext.GetEmitter(metadata.Pid).Emit(args.ModuleId, args.WrapperMethodToken, args.WrappedMethodToken);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectedTrackedObjectsRecordedEvent args)
    {
        _lockRegistry.RemoveRange(metadata.Pid, args.RemovedTrackedObjectIds);
        base.Visit(metadata, args);
    }

    private RuntimeArgumentList ParseArguments(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => ParseArguments(metadata, args.ModuleId, args.MethodToken, args.ArgumentValues, args.ArgumentInfos);

    private RuntimeArgumentList ParseArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => ParseArguments(metadata, args.ModuleId, args.MethodToken, args.ByRefArgumentValues, args.ByRefArgumentInfos);

    private RuntimeArgumentList ParseArguments(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> infos)
    {
        var result = _argumentsParser.ParseArguments(metadata, moduleId, methodToken, values, infos);
        if (result.IsError)
            throw new PluginException($"Could not parse arguments for method {methodToken} from module {moduleId.Value}");

        return result.Value;
    }
    
    private void PushArgumentsOnCallStack(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
    }
    
    private (ProcessThreadId Id, ShadowLock LockObj) PopLockObjectFromCallStack(
        RecordedEventMetadata metadata,
        MethodExitRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var lockObj = GetLockFromFrame(id, frame);
        return (id, lockObj);
    }
    
    internal RuntimeStateSnapshot DumpRuntimeState()
    {
        return new RuntimeStateSnapshot(
            Callstacks: _callStackTracker.GetSnapshot(),
            Threads: _callStackTracker.GetThreadIds(),
            Locks: _lockRegistry.GetAllLocks());
    }
    
    private static ProcessThreadId GetProcessThreadId(RecordedEventMetadata metadata)
        => new(metadata.Pid, metadata.Tid);
    
    private static void EnsureCallStackIntegrity(StackFrame frame, ModuleId moduleId, MdMethodDef methodToken)
    {
        if (frame.ModuleId != moduleId || frame.MethodToken != methodToken)
            throw new PluginException("Call stack frame does not match the expected method.");
    }
    
    internal record RuntimeStateSnapshot(
        IReadOnlyDictionary<ProcessThreadId, Callstack> Callstacks,
        IReadOnlySet<ProcessThreadId> Threads,
        IReadOnlySet<ShadowLock> Locks);
}
