// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

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

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract class PerThreadOrderingPluginBase : PluginBase
{
    private readonly ThreadCallStackTracker _callStackTracker = new();
    private readonly ThreadObjectRegistry _threadObjectRegistry = new();
    private readonly IMetadataContext _metadataContext;
    private readonly IArgumentsParser _argumentsParser;

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
    public event Action<StaticFieldReadArgs>? StaticFieldRead;
    public event Action<StaticFieldWriteArgs>? StaticFieldWritten;

    protected PerThreadOrderingPluginBase(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        TimeProvider timeProvider,
        ILogger logger)
        : base(moduleBindContext, metadataContext, profilerCommandSenderProvider, timeProvider, logger)
    {
        _metadataContext = metadataContext;
        _argumentsParser = argumentsParser;
    }

    protected void RaiseLockAcquireAttempted(LockAcquireAttemptArgs args) => LockAcquireAttempted?.Invoke(args);
    protected void RaiseLockAcquireReturned(LockAcquireResultArgs args) => LockAcquireReturned?.Invoke(args);
    protected void RaiseLockReleased(LockReleaseArgs args) => LockReleased?.Invoke(args);
    protected void RaisePulsedOne(ObjectPulseOneArgs args) => ObjectPulsedOne?.Invoke(args);
    protected void RaisePulsedAll(ObjectPulseAllArgs args) => ObjectPulsedAll?.Invoke(args);
    protected void RaiseObjectWaitAttempted(ObjectWaitAttemptArgs args) => ObjectWaitAttempted?.Invoke(args);
    protected void RaiseObjectWaitReturned(ObjectWaitResultArgs args) => ObjectWaitReturned?.Invoke(args);
    protected void RaiseThreadStarted(ThreadStartArgs args) => ThreadStarted?.Invoke(args);
    protected void RaiseThreadMappingUpdated(ThreadMappingArgs args) => ThreadMappingUpdated?.Invoke(args);
    protected void RaiseThreadJoinAttempted(ThreadJoinAttemptArgs args) => ThreadJoinAttempted?.Invoke(args);
    protected void RaiseThreadJoinReturned(ThreadJoinResultArgs args) => ThreadJoinReturned?.Invoke(args);
    protected void RaiseStaticFieldRead(StaticFieldReadArgs args) => StaticFieldRead?.Invoke(args);
    protected void RaiseStaticFieldWritten(StaticFieldWriteArgs args) => StaticFieldWritten?.Invoke(args);

    [RecordedEventBind((ushort)RecordedEventType.LockAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.LockTryAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockTryAcquire)]
    public void OnLockAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, lockId) = ExtractLockContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, lockId));
    }

    [RecordedEventBind((ushort)RecordedEventType.LockAcquireResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void OnLockAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken: true);

    [RecordedEventBind((ushort)RecordedEventType.LockAcquireResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquireResult)]
    public void OnLockAcquireExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var lockTaken = DetermineLockTakenFromExitEvent(metadata, args);
        HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken);
    }

    [RecordedEventBind((ushort)RecordedEventType.LockRelease)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockRelease)]
    public void OnLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.LockReleaseResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void OnLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockId) = PopLockContext(metadata, args);
        ProcessLockRelease(id, args.ModuleId, args.MethodToken, lockId);
    }

    [RecordedEventBind((ushort)RecordedEventType.LockReleaseResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void OnLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var (id, lockId, lockTaken) = PopLockContextWithFlag(metadata, args);
        if (lockTaken)
            ProcessLockRelease(id, args.ModuleId, args.MethodToken, lockId);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneAttempt)]
    public void OnMonitorPulseOneAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseOneResult)]
    public void OnMonitorPulseOneResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockId) = PopLockContext(metadata, args);
        ProcessPulseOne(id, args.ModuleId, args.MethodToken, lockId);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllAttempt)]
    public void OnMonitorPulseAllAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    [RecordedEventBind((ushort)RecordedEventType.MonitorPulseAllResult)]
    public void OnMonitorPulseAllResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockId) = PopLockContext(metadata, args);
        ProcessPulseAll(id, args.ModuleId, args.MethodToken, lockId);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitAttempt)]
    public void OnMonitorWaitAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, lockId) = ExtractLockContext(metadata, args);
        OnBeforeWaitAttempt(id, lockId);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        ProcessWaitAttempt(id, args.ModuleId, args.MethodToken, lockId);
    }

    [RecordedEventBind((ushort)RecordedEventType.MonitorWaitResult)]
    public void OnMonitorWaitResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Peek(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var lockId = ExtractLockIdFromFrame(id, frame);
        var success = MemoryMarshal.Read<bool>(args.ReturnValue);
        ProcessWaitReturn(id, args.ModuleId, args.MethodToken, lockId, success);
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadStart)]
    public void OnThreadStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var threadObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        _threadObjectRegistry.RegisterMapping(threadObjectId, id);
        ProcessThreadStart(id, threadObjectId);
        Logger.LogInformation("Thread started {Name}.", Threads[id]);
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadMapping)]
    public void OnThreadMapping(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var threadObjectId = new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ReturnValue)));
        _threadObjectRegistry.RegisterMapping(threadObjectId, id);
        ProcessThreadMapping(id, threadObjectId);
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadJoinAttempt)]
    public void OnThreadJoinAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var joinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        ProcessThreadJoinAttempt(id, joinedThreadObjectId, args.ModuleId, args.MethodToken);
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadJoinResult)]
    public void OnThreadJoinResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var joinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
        var joinedThreadId = _threadObjectRegistry.GetThreadId(joinedThreadObjectId);
        ThreadJoinReturned?.Invoke(new ThreadJoinResultArgs(id, joinedThreadId, args.ModuleId, args.MethodToken, IsSuccess: true));
    }

    [RecordedEventBind((ushort)RecordedEventType.StaticFieldRead)]
    public void OnStaticFieldRead(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var fieldAccess = GetInstrumentedStaticFieldAccess(metadata, args);
        StaticFieldRead?.Invoke(new StaticFieldReadArgs(id, fieldAccess.ModuleId, fieldAccess.MethodToken, fieldAccess.FieldToken));
    }

    [RecordedEventBind((ushort)RecordedEventType.StaticFieldWrite)]
    public void OnStaticFieldWrite(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var fieldAccess = GetInstrumentedStaticFieldAccess(metadata, args);
        StaticFieldWritten?.Invoke(new StaticFieldWriteArgs(id, fieldAccess.ModuleId, fieldAccess.MethodToken, fieldAccess.FieldToken));
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        _callStackTracker.InitializeCallStack(new ProcessThreadId(metadata.Pid, args.ThreadId));
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

    protected virtual void HandleLockAcquireExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool lockTaken)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Peek(id);
        EnsureCallStackIntegrity(frame, moduleId, functionToken);
        var lockId = ExtractLockIdFromFrame(id, frame);

        if (!lockTaken)
        {
            _callStackTracker.Pop(id);
            LockAcquireReturned?.Invoke(new(id, moduleId, functionToken, lockId, IsSuccess: false));
            return;
        }

        ProcessLockAcquire(id, moduleId, functionToken, lockId);
    }

    protected virtual void ProcessLockAcquire(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef functionToken,
        ProcessTrackedObjectId lockId)
    {
        _callStackTracker.Pop(id);
        LockAcquireReturned?.Invoke(new(id, moduleId, functionToken, lockId, true));
    }

    protected virtual void ProcessLockRelease(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        LockReleased?.Invoke(new(id, moduleId, methodToken, lockId));
    }

    protected virtual void ProcessPulseOne(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        ObjectPulsedOne?.Invoke(new ObjectPulseOneArgs(id, moduleId, methodToken, lockId));
    }

    protected virtual void ProcessPulseAll(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        ObjectPulsedAll?.Invoke(new ObjectPulseAllArgs(id, moduleId, methodToken, lockId));
    }

    protected virtual void OnBeforeWaitAttempt(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
    }

    protected virtual void ProcessWaitAttempt(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        ObjectWaitAttempted?.Invoke(new ObjectWaitAttemptArgs(id, moduleId, methodToken, lockId));
    }

    protected virtual void ProcessWaitReturn(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId,
        bool success)
    {
        OnAfterWaitReturn(id, lockId);
        _callStackTracker.Pop(id);
        ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(id, moduleId, methodToken, lockId, success));
    }

    protected virtual void OnAfterWaitReturn(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
    }

    protected virtual void ProcessThreadStart(ProcessThreadId id, ProcessTrackedObjectId threadObjectId)
    {
        ThreadStarted?.Invoke(new ThreadStartArgs(id, threadObjectId.ObjectId));
    }

    protected virtual void ProcessThreadMapping(ProcessThreadId id, ProcessTrackedObjectId threadObjectId)
    {
        ThreadMappingUpdated?.Invoke(new ThreadMappingArgs(id, threadObjectId.ObjectId));
    }

    protected virtual void ProcessThreadJoinAttempt(
        ProcessThreadId id,
        ProcessTrackedObjectId joinedThreadObjectId,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        if (_threadObjectRegistry.TryGetThreadId(joinedThreadObjectId, out var joiningThreadId))
            ThreadJoinAttempted?.Invoke(new ThreadJoinAttemptArgs(id, joiningThreadId, moduleId, methodToken));
    }

    protected bool TryGetThreadId(ProcessTrackedObjectId threadObjectId, out ProcessThreadId threadId)
        => _threadObjectRegistry.TryGetThreadId(threadObjectId, out threadId);

    protected ProcessThreadId GetThreadIdFromRegistry(ProcessTrackedObjectId threadObjectId)
        => _threadObjectRegistry.GetThreadId(threadObjectId);

    protected IReadOnlyDictionary<ProcessThreadId, Callstack> GetCallstacksSnapshot()
        => _callStackTracker.GetSnapshot();

    protected IReadOnlySet<ProcessThreadId> GetTrackedThreadIds()
        => _callStackTracker.GetThreadIds();

    private (ProcessThreadId Id, RuntimeArgumentList Arguments, ProcessTrackedObjectId LockId) ExtractLockContext(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var lockId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        return (id, arguments, lockId);
    }

    private (ProcessThreadId Id, ProcessTrackedObjectId LockId) PopLockContext(
        RecordedEventMetadata metadata,
        MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        return (id, ExtractLockIdFromFrame(id, frame));
    }

    private (ProcessThreadId Id, ProcessTrackedObjectId LockId, bool LockTaken) PopLockContextWithFlag(
        RecordedEventMetadata metadata,
        MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var lockId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
        var lockTaken = frame.Arguments!.Value.Count == 1 || (bool)frame.Arguments!.Value[1].Value.AsT0;
        return (id, lockId, lockTaken);
    }

    private bool DetermineLockTakenFromExitEvent(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        if (args.ReturnValue.Length > 0)
            return MemoryMarshal.Read<bool>(args.ReturnValue);

        if (args.ByRefArgumentValues.Length > 0)
        {
            var byRefArguments = ParseArguments(metadata, args);
            return (bool)byRefArguments[0].Value.AsT0;
        }

        return true;
    }

    private static ProcessTrackedObjectId ExtractLockIdFromFrame(ProcessThreadId id, StackFrame frame)
        => new(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);

    private void PushArgumentsOnCallStack(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
    }

    private RuntimeArgumentList ParseArguments(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => ParseArgumentsCore(metadata, args.ModuleId, args.MethodToken, args.ArgumentValues, args.ArgumentInfos);

    private RuntimeArgumentList ParseArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => ParseArgumentsCore(metadata, args.ModuleId, args.MethodToken, args.ByRefArgumentValues, args.ByRefArgumentInfos);

    private RuntimeArgumentList ParseArgumentsCore(
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

    private InstrumentedFieldAccess GetInstrumentedStaticFieldAccess(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var instrumentationId = MemoryMarshal.Read<ulong>(args.ArgumentValues);
        return InstrumentedFieldAccesses[new InstrumentationPointId(metadata.Pid, instrumentationId)];
    }

    private static void EnsureCallStackIntegrity(StackFrame frame, ModuleId moduleId, MdMethodDef methodToken)
    {
        if (frame.ModuleId != moduleId || frame.MethodToken != methodToken)
            throw new PluginException("Call stack frame does not match the expected method.");
    }
}
