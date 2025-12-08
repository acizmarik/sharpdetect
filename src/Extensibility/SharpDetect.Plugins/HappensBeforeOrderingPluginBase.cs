// Copyright 2025 Andrej Čižmárik and Contributors
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

namespace SharpDetect.Plugins;

public abstract class HappensBeforeOrderingPluginBase : PluginBase
{
    public readonly record struct LockAcquireAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct LockAcquireResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj, bool IsSuccess);
    public readonly record struct LockReleaseArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectPulseOneArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectPulseAllArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectWaitAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectWaitResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj, bool IsSuccess);
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

    protected HappensBeforeOrderingPluginBase(IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _metadataContext = serviceProvider.GetRequiredService<IMetadataContext>();
        _argumentsParser = serviceProvider.GetRequiredService<IArgumentsParser>();
        _eventsDeliveryContext = serviceProvider.GetRequiredService<IRecordedEventsDeliveryContext>();
    }
    
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquire)]
    public void MonitorLockAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => MonitorLockTryAcquireEnter(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockTryAcquire)]
    public void MonitorLockTryAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        var lockObj = GetOrAddLockFromArguments(id, arguments);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void MonitorLockAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => MonitorLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken: true);

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void MonitorLockAcquireExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var byRefArguments = args.ByRefArgumentValues.Length > 0 ? ParseArguments(metadata, args) : default;
        var lockTaken = byRefArguments == default || (bool)byRefArguments[0].Value.AsT0;
        MonitorLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken);
    }

    private void MonitorLockAcquireExit(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef functionToken, bool lockTaken)
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

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockRelease)]
    public void MonitorLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleMonitorOperationEntry(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void MonitorLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockObj) = HandleMonitorOperationExit(metadata, args);
        lockObj.Release(id);
        LockReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneAttempt)]
    public void MonitorPulseOneAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleMonitorOperationEntry(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneResult)]
    public void MonitorPulseOneResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockObj) = HandleMonitorOperationExit(metadata, args);
        ObjectPulsedOne?.Invoke(new ObjectPulseOneArgs(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllAttempt)]
    public void MonitorPulseAllAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleMonitorOperationEntry(metadata, args);
    
    private void HandleMonitorOperationEntry(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllResult)]
    public void MonitorPulseAllResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockObj) = HandleMonitorOperationExit(metadata, args);
        ObjectPulsedAll?.Invoke(new ObjectPulseAllArgs(id, args.ModuleId, args.MethodToken, lockObj));
    }
    
    private (ProcessThreadId Id, Lock LockObj) HandleMonitorOperationExit(
        RecordedEventMetadata metadata,
        MethodExitRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var lockObj = GetLockFromFrame(id, frame);
        return (id, lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitAttempt)]
    public void MonitorWaitAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var arguments = ParseArguments(metadata, args);
        var lockObj = GetOrAddLockFromArguments(id, arguments);
        var reentrancyCount = lockObj.ReleaseAll(id);
        
        _reentrancyTracker.PushReentrancyCount(id, reentrancyCount);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        ObjectWaitAttempted?.Invoke(new ObjectWaitAttemptArgs(id, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitResult)]
    public void MonitorWaitResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var frame = _callStackTracker.Peek(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var lockObj = GetLockFromFrame(id, frame);
        var success = MemoryMarshal.Read<bool>(args.ReturnValue);
        var reentrancyCount = _reentrancyTracker.PeekReentrancyCount(id);
        
        if (TryConsumeOrDeferLockAcquireMultiple(id, lockObj, reentrancyCount))
        {
            _reentrancyTracker.PopReentrancyCount(id);
            ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(id, args.ModuleId, args.MethodToken, lockObj, success));
        }
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadStart)]
    public void ThreadStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
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
    public void ThreadMapping(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var threadObjectId = new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ReturnValue));
        var processThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, threadObjectId);
        
        _threadObjectRegistry.RegisterMapping(processThreadObjectId, id);
        ThreadMappingUpdated?.Invoke(new ThreadMappingArgs(id, threadObjectId));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForThreadStart(processThreadObjectId);
    }
    
    [RecordedEventBind((ushort)RecordedEventType.ThreadJoinAttempt)]
    public void ThreadJoinAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
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
    public void ThreadJoinResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = GetProcessThreadId(metadata);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var joinedThreadObjectId = frame.Arguments!.Value[0].Value.AsT1;
        var processJoinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, joinedThreadObjectId);
        var processJoinedThreadId = _threadObjectRegistry.GetThreadId(processJoinedThreadObjectId);
        
        ThreadJoinReturned?.Invoke(new ThreadJoinResultArgs(id, processJoinedThreadId, args.ModuleId, args.MethodToken, IsSuccess: true));
    }

    private Lock GetOrAddLockFromArguments(ProcessThreadId processThreadId, RuntimeArgumentList arguments)
    {
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(processThreadId.ProcessId, lockObjectId);
        return _lockRegistry.GetOrAdd(processLockObjectId);
    }
    
    private Lock GetLockFromFrame(ProcessThreadId processThreadId, StackFrame frame)
    {
        var lockObjectId = frame.Arguments!.Value[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(processThreadId.ProcessId, lockObjectId);
        return _lockRegistry.Get(processLockObjectId);
    }
    
    private bool TryConsumeOrDeferLockAcquire(ProcessThreadId processThreadId, Lock lockObj)
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

    private bool TryConsumeOrDeferLockAcquireMultiple(ProcessThreadId processThreadId, Lock lockObj, int count)
    {
        if (lockObj.Owner is null || lockObj.Owner.Value == processThreadId)
        {
            // It can be executed right away
            lockObj.AcquireMultiple(processThreadId, count);
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
        IReadOnlySet<Lock> Locks);
}
