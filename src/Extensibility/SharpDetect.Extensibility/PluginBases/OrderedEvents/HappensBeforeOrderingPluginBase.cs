// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;
using SharpDetect.Extensibility.Descriptors;
using SharpDetect.Extensibility.Models;
using SharpDetect.Extensibility.PluginBases.MethodDescriptors;
using SharpDetect.Loaders;
using SharpDetect.Metadata;
using SharpDetect.Serialization;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace SharpDetect.Extensibility.PluginBases.OrderedEvents;

public abstract class HappensBeforeOrderingPluginBase : PluginBase
{
    public readonly record struct LockAcquireAttemptArgs(uint ProcessId, ThreadId ThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct LockAcquireResultArgs(uint ProcessId, ThreadId ThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj, bool IsSuccess);
    public readonly record struct LockReleaseArgs(uint ProcessId, ThreadId ThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectPulseOneArgs(uint ProcessId, ThreadId ThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectPulseAllArgs(uint ProcessId, ThreadId ThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectWaitAttemptArgs(uint ProcessId, ThreadId ThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj);
    public readonly record struct ObjectWaitResultArgs(uint ProcessId, ThreadId ThreadId, ModuleId ModuleId, MdMethodDef MethodToken, Lock LockObj, bool IsSuccess);

    public event Action<LockAcquireAttemptArgs>? LockAcquireAttempted;
    public event Action<LockAcquireResultArgs>? LockAcquireReturned;
    public event Action<LockReleaseArgs>? LockReleased;
    public event Action<ObjectPulseOneArgs>? ObjectPulsedOne;
    public event Action<ObjectPulseAllArgs>? ObjectPulsedAll;
    public event Action<ObjectWaitAttemptArgs>? ObjectWaitAttempted;
    public event Action<ObjectWaitResultArgs>? ObjectWaitReturned;

    private readonly Dictionary<ThreadId, Stack<RuntimeArgumentList>> _callstackArguments = [];
    private readonly Dictionary<TrackedObjectId, Lock> _locks = [];
    private readonly IMetadataContext _metadataContext;
    private readonly IArgumentsParser _argumentsParser;
    private readonly IEventsDeliveryContext _eventsDeliveryContext;

    protected HappensBeforeOrderingPluginBase(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IEventsDeliveryContext eventsDeliveryContext,
        ILogger logger)
        : base(moduleBindContext, logger)
    {
        _metadataContext = metadataContext;
        _argumentsParser = argumentsParser;
        _eventsDeliveryContext = eventsDeliveryContext;
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquire)]
    public void MonitorLockAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = ParseArguments(metadata, args);
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetOrAddLock(lockObjectId);
        _callstackArguments[threadId].Push(arguments);
        LockAcquireAttempted?.Invoke(new(metadata.Pid, threadId, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockTryAcquire)]
    public void MonitorLockTryAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = ParseArguments(metadata, args);
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetOrAddLock(lockObjectId);
        _callstackArguments[threadId].Push(arguments);
        LockAcquireAttempted?.Invoke(new(metadata.Pid, threadId, args.ModuleId, args.MethodToken, lockObj));
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
        var threadId = metadata.Tid;
        var arguments = _callstackArguments[threadId].Peek();
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetLock(lockObjectId);
        if (!lockTaken)
        {
            LockAcquireReturned?.Invoke(new(metadata.Pid, threadId, moduleId, functionToken, lockObj, false));
            return;
        }

        if (TryConsumeOrDeferLockAcquire(threadId, lockObj))
            LockAcquireReturned?.Invoke(new(metadata.Pid, threadId, moduleId, functionToken, lockObj, true));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockRelease)]
    public void MonitorLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = ParseArguments(metadata, args);
        _callstackArguments[threadId].Push(arguments);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void MonitorLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = _callstackArguments[threadId].Pop();
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetLock(lockObjectId);
        lockObj.Release(threadId);
        LockReleased?.Invoke(new(metadata.Pid, threadId, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneAttempt)]
    public void MonitorPulseOneAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = ParseArguments(metadata, args);
        _callstackArguments[threadId].Push(arguments);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneAttempt)]
    public void MonitorPulseOneResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = _callstackArguments[threadId].Pop();
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetLock(lockObjectId);
        _callstackArguments[threadId].Push(arguments);
        ObjectPulsedOne?.Invoke(new ObjectPulseOneArgs(metadata.Pid, threadId, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllAttempt)]
    public void MonitorPulseAllAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = ParseArguments(metadata, args);
        _callstackArguments[threadId].Push(arguments);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllResult)]
    public void MonitorPulseAllResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = _callstackArguments[threadId].Pop();
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetLock(lockObjectId);
        ObjectPulsedAll?.Invoke(new ObjectPulseAllArgs(metadata.Pid, threadId, args.ModuleId, args.MethodToken, lockObj));
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitAttempt)]
    public void MonitorWaitAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = ParseArguments(metadata, args);
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetOrAddLock(lockObjectId);
        lockObj.Release(threadId);
        _callstackArguments[threadId].Push(arguments);
        ObjectWaitAttempted?.Invoke(new ObjectWaitAttemptArgs(metadata.Pid, threadId, args.ModuleId, args.MethodToken, lockObj));
        _eventsDeliveryContext.UnblockEventsDeliveryForThreadWaitingForObject(lockObj);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitResult)]
    public void MonitorWaitResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var threadId = metadata.Tid;
        var arguments = _callstackArguments[threadId].Peek();
        var lockObjectId = arguments[0].Value.AsT1;
        var lockObj = GetLock(lockObjectId);
        var success = MemoryMarshal.Read<bool>(args.ReturnValue);
        if (TryConsumeOrDeferLockAcquire(threadId, lockObj))
            ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(metadata.Pid, threadId, args.ModuleId, args.MethodToken, lockObj, success));
    }

    private bool TryConsumeOrDeferLockAcquire(ThreadId threadId, Lock lockObj)
    {
        if (lockObj.Owner is null || lockObj.Owner.Value == threadId)
        {
            // It can be executed right away
            lockObj.Acquire(threadId);
            _callstackArguments[threadId].Pop();
            return true;
        }

        // It needs to be deferred
        _eventsDeliveryContext.BlockEventsDeliveryForThreadWaitingForObject(threadId, lockObj);
        return false;
    }

    protected static ImmutableArray<MethodDescriptor> GetRequiredMethodDescriptors()
        => MonitorMethodDescriptors.GetAllMethods().ToImmutableArray();

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        _callstackArguments[args.ThreadId] = new Stack<RuntimeArgumentList>();
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

    private Lock GetOrAddLock(TrackedObjectId lockObjectId)
    {
        if (_locks.TryGetValue(lockObjectId, out var lockObj))
            return lockObj;

        lockObj = new Lock(lockObjectId);
        _locks.Add(lockObjectId, lockObj);
        return lockObj;
    }

    private Lock GetLock(TrackedObjectId lockObjectId)
    {
        if (!_locks.TryGetValue(lockObjectId, out var lockObj))
            throw new PluginException($"Could not resolve objectId {lockObjectId.Value} to a known lock.");

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
}
