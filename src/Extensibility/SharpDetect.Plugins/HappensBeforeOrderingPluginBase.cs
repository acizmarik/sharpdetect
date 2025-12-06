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

    private readonly Dictionary<ProcessThreadId, Stack<RuntimeArgumentList>> _callstackArguments = [];
    private readonly Dictionary<ProcessTrackedObjectId, Lock> _locks = [];
    private readonly Dictionary<ProcessTrackedObjectId, ProcessThreadId> _objectIdToThreadIdLookup = [];
    private readonly Dictionary<ProcessThreadId, Stack<int>> _monitorWaitReentrancyCounts = [];
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
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetOrAddLock(processLockObjectId);
        _callstackArguments[id].Push(arguments);
        LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockTryAcquire)]
    public void MonitorLockTryAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetOrAddLock(processLockObjectId);
        _callstackArguments[id].Push(arguments);
        LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void MonitorLockAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => MonitorLockAcquireExit(metadata, args.ModuleId, args.MethodToken, true);

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void MonitorLockAcquireExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var byRefArguments = args.ByRefArgumentValues.Length > 0 ? ParseArguments(metadata, args) : default;
        var lockTaken = byRefArguments == default || (bool)byRefArguments[0].Value.AsT0;
        MonitorLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken);
    }

    private void MonitorLockAcquireExit(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef functionToken, bool lockTaken)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = _callstackArguments[id].Peek();
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetLock(processLockObjectId);
        if (!lockTaken)
        {
            _callstackArguments[id].Pop();
            LockAcquireReturned?.Invoke(new(id, moduleId, functionToken, lockObj, false));
            return;
        }

        if (TryConsumeOrDeferLockAcquire(id, lockObj))
            LockAcquireReturned?.Invoke(new(id, moduleId, functionToken, lockObj, true));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockRelease)]
    public void MonitorLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        _callstackArguments[id].Push(arguments);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void MonitorLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = _callstackArguments[id].Pop();
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetLock(processLockObjectId);
        lockObj.Release(id);
        LockReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneAttempt)]
    public void MonitorPulseOneAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        _callstackArguments[id].Push(arguments);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneResult)]
    public void MonitorPulseOneResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = _callstackArguments[id].Pop();
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetLock(processLockObjectId);
        ObjectPulsedOne?.Invoke(new ObjectPulseOneArgs(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllAttempt)]
    public void MonitorPulseAllAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        _callstackArguments[id].Push(arguments);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllResult)]
    public void MonitorPulseAllResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = _callstackArguments[id].Pop();
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetLock(processLockObjectId);
        ObjectPulsedAll?.Invoke(new ObjectPulseAllArgs(id, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitAttempt)]
    public void MonitorWaitAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetOrAddLock(processLockObjectId);
        var reentrancyCount = lockObj.ReleaseAll(id);
        
        // We must store reentrancy count to be able to reacquire the lock correctly in MonitorWaitResult
        if (!_monitorWaitReentrancyCounts.TryGetValue(id, out var stack))
        {
            stack = new Stack<int>();
            _monitorWaitReentrancyCounts[id] = stack;
        }
        stack.Push(reentrancyCount);
        
        _callstackArguments[id].Push(arguments);
        ObjectWaitAttempted?.Invoke(new ObjectWaitAttemptArgs(id, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitResult)]
    public void MonitorWaitResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = _callstackArguments[id].Peek();
        var lockObjectId = arguments[0].Value.AsT1;
        var processLockObjectId = new ProcessTrackedObjectId(id.ProcessId, lockObjectId);
        var lockObj = GetLock(processLockObjectId);
        var success = MemoryMarshal.Read<bool>(args.ReturnValue);
        var reentrancyCount = _monitorWaitReentrancyCounts[id].Peek();
        
        if (TryConsumeOrDeferLockAcquireMultiple(id, lockObj, reentrancyCount))
        {
            _monitorWaitReentrancyCounts[id].Pop();
            ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(id, args.ModuleId, args.MethodToken, lockObj, success));
        }
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadStart)]
    public void ThreadStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var processThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        _objectIdToThreadIdLookup[processThreadObjectId] = id;
        ThreadStarted?.Invoke(new ThreadStartArgs(id, processThreadObjectId.ObjectId));
        Logger.LogInformation("Thread started {Name}.", Threads[id]);
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForThreadStart(processThreadObjectId);
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadMapping)]
    public void ThreadMapping(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var threadObjectId = new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ReturnValue));
        var processThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, threadObjectId);
        _objectIdToThreadIdLookup[processThreadObjectId] = id;
        ThreadMappingUpdated?.Invoke(new ThreadMappingArgs(id, threadObjectId));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForThreadStart(processThreadObjectId);
    }
    
    [RecordedEventBind((ushort)RecordedEventType.ThreadJoinAttempt)]
    public void ThreadJoinAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var joinedThreadObjectId = arguments[0].Value.AsT1;
        var processJoinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, joinedThreadObjectId);
        if (TryConsumeOrDeferThreadJoin(id, processJoinedThreadObjectId, out var processJoinedThreadId))
        {
            _callstackArguments[id].Push(arguments);
            ThreadJoinAttempted?.Invoke(new ThreadJoinAttemptArgs(id, processJoinedThreadId, args.ModuleId, args.MethodToken));
        }
    }
    
    [RecordedEventBind((ushort)RecordedEventType.ThreadJoinResult)]
    public void ThreadJoinResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = _callstackArguments[id].Pop();
        var joinedThreadObjectId = arguments[0].Value.AsT1;
        var processJoinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, joinedThreadObjectId);
        var processJoinedThreadId = _objectIdToThreadIdLookup[processJoinedThreadObjectId];
        ThreadJoinReturned?.Invoke(new ThreadJoinResultArgs(id, processJoinedThreadId, args.ModuleId, args.MethodToken, IsSuccess: true));
    }

    private bool TryConsumeOrDeferLockAcquire(ProcessThreadId processThreadId, Lock lockObj)
    {
        if (lockObj.Owner is null || lockObj.Owner.Value == processThreadId)
        {
            // It can be executed right away
            lockObj.Acquire(processThreadId);
            _callstackArguments[processThreadId].Pop();
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
            _callstackArguments[processThreadId].Pop();
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
        if (_objectIdToThreadIdLookup.TryGetValue(processJoiningThreadObjectId, out joiningProcessThreadId))
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
        _callstackArguments[id] = new Stack<RuntimeArgumentList>();
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
        foreach (var removedTrackedObjectId in args.RemovedTrackedObjectIds)
            _locks.Remove(new ProcessTrackedObjectId(metadata.Pid, removedTrackedObjectId));

        base.Visit(metadata, args);
    }

    private Lock GetOrAddLock(ProcessTrackedObjectId processLockObjectId)
    {
        if (_locks.TryGetValue(processLockObjectId, out var lockObj))
            return lockObj;

        lockObj = new Lock(processLockObjectId);
        _locks.Add(processLockObjectId, lockObj);
        return lockObj;
    }

    private Lock GetLock(ProcessTrackedObjectId processLockObjectId)
    {
        if (!_locks.TryGetValue(processLockObjectId, out var lockObj))
            throw new PluginException($"Could not resolve objectId {processLockObjectId.ObjectId.Value} to a known lock.");

        return lockObj;
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
            Callstacks: _callstackArguments.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            Threads: _callstackArguments.Keys.ToHashSet(),
            Locks: _locks.Values.ToHashSet());
    }
    
    internal record RuntimeStateSnapshot(
        IReadOnlyDictionary<ProcessThreadId, Stack<RuntimeArgumentList>> Callstacks,
        IReadOnlySet<ProcessThreadId> Threads,
        IReadOnlySet<Lock> Locks);
}
