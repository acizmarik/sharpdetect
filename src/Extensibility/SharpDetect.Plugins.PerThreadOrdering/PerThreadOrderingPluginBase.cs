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
    private enum WaitHandleKind
    {
        Mutex,
        Semaphore,
        AutoResetEvent,
        ManualResetEvent
    }

    private readonly ThreadCallStackTracker _callStackTracker = new();
    private readonly ThreadObjectRegistry _threadObjectRegistry = new();
    private readonly Dictionary<ProcessTrackedObjectId, List<(ProcessThreadId JoiningThread, ModuleId ModuleId, MdMethodDef MethodToken)>> _pendingJoinAttempts = [];
    private readonly Dictionary<ProcessTrackedObjectId, ProcessTrackedObjectId> _waitAsyncTaskToSemaphore = [];
    private readonly Dictionary<ProcessTrackedObjectId, WaitHandleKind> _waitHandleKinds = [];
    private readonly Dictionary<ProcessTrackedObjectId, ProcessThreadId> _lastMutexAcquirers = [];
    private readonly Dictionary<ProcessThreadId, ProcessTrackedObjectId?> _pendingAbandonedMutexes = [];
    private readonly IArgumentsParser _argumentsParser;

    public event Action<LockAcquireAttemptArgs>? LockAcquireAttempted;
    public event Action<LockAcquireResultArgs>? LockAcquireReturned;
    public event Action<LockReleaseArgs>? LockReleased;
    public event Action<ObjectPulseOneArgs>? ObjectPulsedOne;
    public event Action<ObjectPulseAllArgs>? ObjectPulsedAll;
    public event Action<ObjectWaitAttemptArgs>? ObjectWaitAttempted;
    public event Action<ObjectWaitResultArgs>? ObjectWaitReturned;
    public event Action<ThreadStartingArgs>? ThreadStarting;
    public event Action<ThreadStartArgs>? ThreadStarted;
    public event Action<ThreadJoinAttemptArgs>? ThreadJoinAttempted;
    public event Action<ThreadJoinResultArgs>? ThreadJoinReturned;
    public event Action<TaskScheduleArgs>? TaskScheduled;
    public event Action<TaskStartArgs>? TaskStarted;
    public event Action<TaskCompleteArgs>? TaskCompleted;
    public event Action<TaskJoinFinishArgs>? TaskJoinFinished;
    public event Action<StaticFieldReadArgs>? StaticFieldRead;
    public event Action<StaticFieldWriteArgs>? StaticFieldWritten;
    public event Action<InstanceFieldReadArgs>? InstanceFieldRead;
    public event Action<InstanceFieldWriteArgs>? InstanceFieldWritten;
    public event Action<SemaphoreCreatedArgs>? SemaphoreCreated;
    public event Action<SemaphoreAcquireAttemptArgs>? SemaphoreAcquireAttempted;
    public event Action<SemaphoreAcquireResultArgs>? SemaphoreAcquireReturned;
    public event Action<SemaphoreReleaseArgs>? SemaphoreReleased;
    public event Action<SemaphoreWaitAsyncArgs>? SemaphoreWaitAsyncReturned;
    public event Action<EventWaitHandleCreatedArgs>? EventWaitHandleCreated;
    public event Action<EventWaitHandleSetArgs>? EventWaitHandleSignaled;
    public event Action<EventWaitHandleResetArgs>? EventWaitHandleWasReset;
    public event Action<EventWaitHandleWaitResultArgs>? EventWaitHandleWaitReturned;

    protected PerThreadOrderingPluginBase(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver,
        IArgumentsParser argumentsParser,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        TimeProvider timeProvider,
        ILogger logger)
        : base(moduleBindContext, metadataContext, symbolResolver, profilerCommandSenderProvider, timeProvider, logger)
    {
        _argumentsParser = argumentsParser;
    }

    protected void RaiseLockAcquireAttempted(LockAcquireAttemptArgs args) => LockAcquireAttempted?.Invoke(args);
    protected void RaiseLockAcquireReturned(LockAcquireResultArgs args) => LockAcquireReturned?.Invoke(args);
    protected void RaiseLockReleased(LockReleaseArgs args) => LockReleased?.Invoke(args);
    protected void RaisePulsedOne(ObjectPulseOneArgs args) => ObjectPulsedOne?.Invoke(args);
    protected void RaisePulsedAll(ObjectPulseAllArgs args) => ObjectPulsedAll?.Invoke(args);
    protected void RaiseObjectWaitAttempted(ObjectWaitAttemptArgs args) => ObjectWaitAttempted?.Invoke(args);
    protected void RaiseObjectWaitReturned(ObjectWaitResultArgs args) => ObjectWaitReturned?.Invoke(args);
    protected void RaiseThreadStarting(ThreadStartingArgs args) => ThreadStarting?.Invoke(args);
    protected void RaiseThreadStarted(ThreadStartArgs args) => ThreadStarted?.Invoke(args);
    protected void RaiseThreadJoinAttempted(ThreadJoinAttemptArgs args) => ThreadJoinAttempted?.Invoke(args);
    protected void RaiseThreadJoinReturned(ThreadJoinResultArgs args) => ThreadJoinReturned?.Invoke(args);
    protected void RaiseTaskScheduled(TaskScheduleArgs args) => TaskScheduled?.Invoke(args);
    protected void RaiseTaskStarted(TaskStartArgs args) => TaskStarted?.Invoke(args);
    protected void RaiseTaskCompleted(TaskCompleteArgs args) => TaskCompleted?.Invoke(args);
    protected void RaiseTaskJoinFinished(TaskJoinFinishArgs args) => TaskJoinFinished?.Invoke(args);
    protected void RaiseStaticFieldRead(StaticFieldReadArgs args) => StaticFieldRead?.Invoke(args);
    protected void RaiseStaticFieldWritten(StaticFieldWriteArgs args) => StaticFieldWritten?.Invoke(args);
    protected void RaiseInstanceFieldRead(InstanceFieldReadArgs args) => InstanceFieldRead?.Invoke(args);
    protected void RaiseInstanceFieldWritten(InstanceFieldWriteArgs args) => InstanceFieldWritten?.Invoke(args);
    protected void RaiseSemaphoreCreated(SemaphoreCreatedArgs args) => SemaphoreCreated?.Invoke(args);
    protected void RaiseSemaphoreAcquireAttempted(SemaphoreAcquireAttemptArgs args) => SemaphoreAcquireAttempted?.Invoke(args);
    protected void RaiseSemaphoreAcquireReturned(SemaphoreAcquireResultArgs args) => SemaphoreAcquireReturned?.Invoke(args);
    protected void RaiseSemaphoreReleased(SemaphoreReleaseArgs args) => SemaphoreReleased?.Invoke(args);
    protected void RaiseSemaphoreWaitAsyncReturned(SemaphoreWaitAsyncArgs args) => SemaphoreWaitAsyncReturned?.Invoke(args);
    protected void RaiseEventWaitHandleCreated(EventWaitHandleCreatedArgs args) => EventWaitHandleCreated?.Invoke(args);
    protected void RaiseEventWaitHandleSignaled(EventWaitHandleSetArgs args) => EventWaitHandleSignaled?.Invoke(args);
    protected void RaiseEventWaitHandleWasReset(EventWaitHandleResetArgs args) => EventWaitHandleWasReset?.Invoke(args);
    protected void RaiseEventWaitHandleWaitReturned(EventWaitHandleWaitResultArgs args) => EventWaitHandleWaitReturned?.Invoke(args);

    [RecordedEventBind((ushort)RecordedEventType.LockAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.LockTryAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockTryAcquire)]
    public void OnLockAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, lockId) = ExtractSynchronizationContext(metadata, args);
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
    {
        var (id, arguments, lockId) = ExtractSynchronizationContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        var lockTaken = arguments.Count == 1 || (bool)arguments[1].Value.AsT0;
        if (lockTaken)
            ProcessLockRelease(id, args.ModuleId, args.MethodToken, lockId);
    }

    [RecordedEventBind((ushort)RecordedEventType.LockReleaseResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void OnLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    [RecordedEventBind((ushort)RecordedEventType.LockReleaseResult)]
    [RecordedEventBind((ushort)RecordedEventType.MonitorLockReleaseResult)]
    public void OnLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

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
        var (id, arguments, lockId) = ExtractSynchronizationContext(metadata, args);
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
        var lockId = ExtractSynchronizationObjectIdFromFrame(id, frame);
        var success = MemoryMarshal.Read<bool>(args.ReturnValue);
        ProcessWaitReturn(id, args.ModuleId, args.MethodToken, lockId, success);
    }

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreCreate)]
    public void OnSemaphoreCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, semaphoreId) = ExtractSynchronizationContext(metadata, args);
        var initialCount = (int)arguments[1].Value.AsT0;
        _waitHandleKinds[semaphoreId] = WaitHandleKind.Semaphore;
        SemaphoreCreated?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId, initialCount));
    }

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreAcquire)]
    [RecordedEventBind((ushort)RecordedEventType.SemaphoreTryAcquire)]
    public void OnSemaphoreAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, semaphoreId) = ExtractSynchronizationContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        SemaphoreAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId));
    }

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreAcquireResult)]
    public void OnSemaphoreAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleSemaphoreAcquireExit(metadata, args.ModuleId, args.MethodToken, isSuccess: true);

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreAcquireResult)]
    public void OnSemaphoreAcquireExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleSemaphoreAcquireExit(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreRelease)]
    public void OnSemaphoreReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, semaphoreId) = ExtractSynchronizationContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        var releaseCount = (int)arguments[1].Value.AsT0;
        SemaphoreReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId, releaseCount));
    }

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreReleaseResult)]
    public void OnSemaphoreReleaseExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreWaitAsync)]
    public void OnSemaphoreWaitAsyncEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, _) = ExtractSynchronizationContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
    }

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreWaitAsyncResult)]
    public void OnSemaphoreWaitAsyncExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    [RecordedEventBind((ushort)RecordedEventType.SemaphoreWaitAsyncResult)]
    public void OnSemaphoreWaitAsyncExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var semaphoreId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
        var waiterTaskId = new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ReturnValue)));
        _waitAsyncTaskToSemaphore[waiterTaskId] = semaphoreId;
    }

    [RecordedEventBind((ushort)RecordedEventType.MutexCreate)]
    public void OnMutexCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (_, _, mutexId) = ExtractSynchronizationContext(metadata, args);
        _waitHandleKinds[mutexId] = WaitHandleKind.Mutex;
    }

    [RecordedEventBind((ushort)RecordedEventType.MutexRelease)]
    public void OnMutexReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, mutexId) = ExtractSynchronizationContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        // ReleaseMutex proves the kind even when the constructor was not observed (for example, OpenExisting)
        _waitHandleKinds[mutexId] = WaitHandleKind.Mutex;
        LockReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, mutexId));
    }

    [RecordedEventBind((ushort)RecordedEventType.MutexReleaseResult)]
    public void OnMutexReleaseExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    [RecordedEventBind((ushort)RecordedEventType.AutoResetEventCreate)]
    public void OnAutoResetEventCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleEventWaitHandleCreate(metadata, args, isAutoReset: true, isBaseConstructor: false);

    [RecordedEventBind((ushort)RecordedEventType.ManualResetEventCreate)]
    public void OnManualResetEventCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleEventWaitHandleCreate(metadata, args, isAutoReset: false, isBaseConstructor: false);

    [RecordedEventBind((ushort)RecordedEventType.EventWaitHandleCreate)]
    public void OnEventWaitHandleCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleEventWaitHandleCreate(metadata, args, isAutoReset: false, isBaseConstructor: true);

    [RecordedEventBind((ushort)RecordedEventType.EventWaitHandleSet)]
    public void OnEventWaitHandleSetEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, eventId) = ExtractSynchronizationContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        if (!_waitHandleKinds.TryGetValue(eventId, out var kind))
        {
            // Unknown kind (for example, OpenExisting)
            // Manual-reset should over-approximate happens-before
            _waitHandleKinds[eventId] = kind = WaitHandleKind.ManualResetEvent;
        }

        EventWaitHandleSignaled?.Invoke(new(id, args.ModuleId, args.MethodToken, eventId, kind == WaitHandleKind.AutoResetEvent));
    }

    [RecordedEventBind((ushort)RecordedEventType.EventWaitHandleSetResult)]
    public void OnEventWaitHandleSetExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    [RecordedEventBind((ushort)RecordedEventType.EventWaitHandleReset)]
    public void OnEventWaitHandleResetEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, eventId) = ExtractSynchronizationContext(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        EventWaitHandleWasReset?.Invoke(new(id, args.ModuleId, args.MethodToken, eventId));
    }

    [RecordedEventBind((ushort)RecordedEventType.EventWaitHandleResetResult)]
    public void OnEventWaitHandleResetExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleWait)]
    public void OnWaitHandleWaitEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, handleId) = ExtractSynchronizationContext(metadata, args);
        _pendingAbandonedMutexes.Remove(id);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        if (!_waitHandleKinds.TryGetValue(handleId, out var kind))
            return;

        switch (kind)
        {
            case WaitHandleKind.Mutex:
                LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, handleId));
                break;

            case WaitHandleKind.Semaphore:
                SemaphoreAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, handleId));
                break;
        }
    }

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleWaitResult)]
    public void OnWaitHandleWaitExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleWaitHandleWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess: true);

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleWaitResult)]
    public void OnWaitHandleWaitExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleWaitHandleWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleSignalAndWait)]
    public void OnWaitHandleSignalAndWaitEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, toSignalId) = ExtractSynchronizationContext(metadata, args);
        var toWaitOnId = new ProcessTrackedObjectId(id.ProcessId, arguments[1].Value.AsT1);
        _pendingAbandonedMutexes.Remove(id);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));

        if (_waitHandleKinds.TryGetValue(toSignalId, out var signalKind))
        {
            switch (signalKind)
            {
                case WaitHandleKind.Mutex:
                    LockReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, toSignalId));
                    break;

                case WaitHandleKind.Semaphore:
                    SemaphoreReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, toSignalId, ReleaseCount: 1));
                    break;

                case WaitHandleKind.AutoResetEvent:
                case WaitHandleKind.ManualResetEvent:
                    EventWaitHandleSignaled?.Invoke(new(id, args.ModuleId, args.MethodToken, toSignalId, signalKind == WaitHandleKind.AutoResetEvent));
                    break;
            }
        }

        if (_waitHandleKinds.TryGetValue(toWaitOnId, out var waitKind))
        {
            switch (waitKind)
            {
                case WaitHandleKind.Mutex:
                    LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, toWaitOnId));
                    break;

                case WaitHandleKind.Semaphore:
                    SemaphoreAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, toWaitOnId));
                    break;
            }
        }
    }

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleSignalAndWaitResult)]
    public void OnWaitHandleSignalAndWaitExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleWaitHandleSignalAndWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess: true);

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleSignalAndWaitResult)]
    public void OnWaitHandleSignalAndWaitExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleWaitHandleSignalAndWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    [RecordedEventBind((ushort)RecordedEventType.AbandonedMutexExceptionCreate)]
    public void OnAbandonedMutexExceptionCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        _pendingAbandonedMutexes[id] = arguments.Count > 1
            ? new ProcessTrackedObjectId(id.ProcessId, arguments[1].Value.AsT1)
            : null;
    }

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleWaitMultiple)]
    public void OnWaitHandleWaitMultipleEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        _pendingAbandonedMutexes.Remove(id);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
    }

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleWaitMultipleResult)]
    public void OnWaitHandleWaitMultipleExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    [RecordedEventBind((ushort)RecordedEventType.WaitHandleWaitMultipleResult)]
    public void OnWaitHandleWaitMultipleExit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var handles = frame.Arguments!.Value[0].Value.AsT2;
        var waitAll = (bool)frame.Arguments!.Value[1].Value.AsT0;
        var result = MemoryMarshal.Read<int>(args.ReturnValue);
        DispatchWaitMultipleResult(id, args.ModuleId, args.MethodToken, handles, waitAll, result);
    }

    [RecordedEventBind((ushort)RecordedEventType.ThreadStartCore)]
    public void OnThreadStartCore(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Note: this method is invoked by parent thread
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var threadObjectId = new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ArgumentValues)));
        ProcessThreadStartCore(id, threadObjectId);
    }
    
    [RecordedEventBind((ushort)RecordedEventType.ThreadStartCallback)]
    public void OnThreadStartCallback(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Note: this method is invoked by the newly started thread
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var threadObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        _threadObjectRegistry.RegisterMapping(threadObjectId, id);
        if (_pendingJoinAttempts.Remove(threadObjectId, out var pendingJoins))
        {
            foreach (var (joiningThread, moduleId, methodToken) in pendingJoins)
                ProcessThreadJoinAttempt(joiningThread, threadObjectId, moduleId, methodToken);
        }
        ProcessThreadStartCallback(id, threadObjectId);
        Logger.LogInformation("Thread started {Name}.", Threads.GetValueOrDefault(id, id.ToString()));
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

    [RecordedEventBind((ushort)RecordedEventType.TaskSchedule)]
    public void OnTaskSchedule(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Called on the parent thread when a task is scheduled (e.g. Task.Run)
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        ProcessTaskSchedule(id, taskObjectId);
    }

    [RecordedEventBind((ushort)RecordedEventType.TaskStart)]
    public void OnTaskStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Called on the worker thread when the task body begins executing
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        ProcessTaskStart(id, taskObjectId);
    }

    [RecordedEventBind((ushort)RecordedEventType.TaskComplete)]
    public void OnTaskComplete(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        // Called on the worker thread when the task body finishes executing
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
        ProcessTaskComplete(id, taskObjectId);
    }

    [RecordedEventBind((ushort)RecordedEventType.TaskJoinStart)]
    public void OnTaskJoinStart(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
    }
    
    [RecordedEventBind((ushort)RecordedEventType.TaskJoinFinish)]
    public void OnTaskJoinFinish(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        HandleTaskJoinFinish(metadata, args.ModuleId, args.MethodToken, isSuccess: true);
    }

    [RecordedEventBind((ushort)RecordedEventType.TaskJoinFinish)]
    public void OnTaskJoinFinish(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleTaskJoinFinish(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    [RecordedEventBind((ushort)RecordedEventType.StaticFieldRead)]
    public void OnStaticFieldRead(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var fieldAccess = GetInstrumentedStaticFieldAccessFromArguments(metadata, args);
        StaticFieldRead?.Invoke(new StaticFieldReadArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            fieldAccess.IsVolatile,
            BuildStack(fieldAccess, args.StackFrames)));
    }

    [RecordedEventBind((ushort)RecordedEventType.StaticFieldWrite)]
    public void OnStaticFieldWrite(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var fieldAccess = GetInstrumentedStaticFieldAccessFromArguments(metadata, args);
        StaticFieldWritten?.Invoke(new StaticFieldWriteArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            fieldAccess.IsVolatile,
            BuildStack(fieldAccess, args.StackFrames)));
    }
    
    [RecordedEventBind((ushort)RecordedEventType.InstanceFieldRead)]
    public void OnInstanceFieldRead(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var (fieldAccess, instance) = GetInstrumentedInstanceFieldAccessFromArguments(metadata, args);
        InstanceFieldRead?.Invoke(new InstanceFieldReadArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            instance,
            fieldAccess.IsVolatile,
            BuildStack(fieldAccess, args.StackFrames)));
    }

    [RecordedEventBind((ushort)RecordedEventType.InstanceFieldWrite)]
    public void OnInstanceFieldWrite(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var (fieldAccess, instance) = GetInstrumentedInstanceFieldAccessFromArguments(metadata, args);
        InstanceFieldWritten?.Invoke(new InstanceFieldWriteArgs(
            id,
            fieldAccess.MethodOffset,
            fieldAccess.FieldToken,
            instance,
            fieldAccess.IsVolatile,
            BuildStack(fieldAccess, args.StackFrames)));
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        _callStackTracker.InitializeCallStack(new ProcessThreadId(metadata.Pid, args.ThreadId));
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectedTrackedObjectsRecordedEvent args)
    {
        if (_waitAsyncTaskToSemaphore.Count > 0)
        {
            foreach (var objectId in args.RemovedTrackedObjectIds)
                _waitAsyncTaskToSemaphore.Remove(new ProcessTrackedObjectId(metadata.Pid, objectId));
        }

        if (_waitHandleKinds.Count > 0)
        {
            foreach (var objectId in args.RemovedTrackedObjectIds)
                _waitHandleKinds.Remove(new ProcessTrackedObjectId(metadata.Pid, objectId));
        }

        if (_lastMutexAcquirers.Count > 0)
        {
            foreach (var objectId in args.RemovedTrackedObjectIds)
                _lastMutexAcquirers.Remove(new ProcessTrackedObjectId(metadata.Pid, objectId));
        }

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodUnwoundRecordedEvent args)
    {
        // The instrumented method exited via an exception, so no method-exit event was produced
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);

        switch ((RecordedEventType)args.Interpretation)
        {
            case RecordedEventType.MonitorLockAcquireResult:
            case RecordedEventType.LockAcquireResult:
            {
                var lockId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                LockAcquireReturned?.Invoke(new(id, args.ModuleId, args.MethodToken, lockId, IsSuccess: false));
                break;
            }

            case RecordedEventType.SemaphoreAcquireResult:
            {
                var semaphoreId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                SemaphoreAcquireReturned?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId, IsSuccess: false));
                break;
            }

            case RecordedEventType.WaitHandleWaitResult:
            {
                var handleId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                if (_pendingAbandonedMutexes.Remove(id))
                    ProcessAbandonedMutexAcquireReturn(id, args.ModuleId, args.MethodToken, handleId);
                else
                    DispatchWaitHandleWaitResult(id, args.ModuleId, args.MethodToken, handleId, isSuccess: false);
                break;
            }

            case RecordedEventType.WaitHandleSignalAndWaitResult:
            {
                var awaitedHandleId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[1].Value.AsT1);
                if (_pendingAbandonedMutexes.Remove(id))
                    ProcessAbandonedMutexAcquireReturn(id, args.ModuleId, args.MethodToken, awaitedHandleId);
                else
                    DispatchWaitHandleWaitResult(id, args.ModuleId, args.MethodToken, awaitedHandleId, isSuccess: false);
                break;
            }

            case RecordedEventType.WaitHandleWaitMultipleResult:
            {
                if (!_pendingAbandonedMutexes.Remove(id, out var abandonedHandle))
                    break;

                var waitAll = (bool)frame.Arguments!.Value[1].Value.AsT0;
                if (waitAll)
                {
                    var handles = frame.Arguments!.Value[0].Value.AsT2;
                    foreach (var handle in handles)
                        DispatchWaitHandleWaitResult(id, args.ModuleId, args.MethodToken, new ProcessTrackedObjectId(id.ProcessId, handle), isSuccess: true);
                }
                else if (abandonedHandle is { } mutexId)
                {
                    ProcessAbandonedMutexAcquireReturn(id, args.ModuleId, args.MethodToken, mutexId);
                }

                break;
            }

            case RecordedEventType.MonitorWaitResult:
            {
                var lockId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                OnAfterWaitReturn(id, lockId);
                ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(id, args.ModuleId, args.MethodToken, lockId, IsSuccess: false));
                break;
            }

            case RecordedEventType.ThreadJoinResult:
            {
                var joinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
                if (_threadObjectRegistry.TryGetThreadId(joinedThreadObjectId, out var joinedThreadId))
                    ThreadJoinReturned?.Invoke(new ThreadJoinResultArgs(id, joinedThreadId, args.ModuleId, args.MethodToken, IsSuccess: false));
                break;
            }

            case RecordedEventType.TaskComplete:
            {
                // A task whose body faulted completes and release its waiters.
                var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
                ProcessTaskComplete(id, taskObjectId);
                break;
            }

            case RecordedEventType.TaskJoinFinish:
            {
                var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
                if (_waitAsyncTaskToSemaphore.Remove(taskObjectId))
                    break;
                
                ProcessTaskJoinFinish(id, taskObjectId, isSuccess: false);
                break;
            }
        }
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
        var lockId = ExtractSynchronizationObjectIdFromFrame(id, frame);

        if (!lockTaken)
        {
            _callStackTracker.Pop(id);
            LockAcquireReturned?.Invoke(new(id, moduleId, functionToken, lockId, IsSuccess: false));
            return;
        }

        ProcessLockAcquire(id, moduleId, functionToken, lockId);
    }
    
    public void HandleTaskJoinFinish(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, moduleId, methodToken);
        var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);
        
        if (_waitAsyncTaskToSemaphore.Remove(taskObjectId, out var semaphoreId) && isSuccess)
        {
            SemaphoreWaitAsyncReturned?.Invoke(new(id, moduleId, methodToken, semaphoreId, taskObjectId));
        }
        else
        {
            ProcessTaskJoinFinish(id, taskObjectId, isSuccess);
        }
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

    protected virtual void ProcessThreadStartCore(ProcessThreadId id, ProcessTrackedObjectId threadObjectId)
    {
        ThreadStarting?.Invoke(new ThreadStartingArgs(id, threadObjectId));
    }
    
    protected virtual void ProcessThreadStartCallback(ProcessThreadId id, ProcessTrackedObjectId threadObjectId)
    {
        ThreadStarted?.Invoke(new ThreadStartArgs(id, threadObjectId));
    }

    protected virtual void ProcessThreadJoinAttempt(
        ProcessThreadId id,
        ProcessTrackedObjectId joinedThreadObjectId,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        if (_threadObjectRegistry.TryGetThreadId(joinedThreadObjectId, out var joiningThreadId))
            ThreadJoinAttempted?.Invoke(new ThreadJoinAttemptArgs(id, joiningThreadId, moduleId, methodToken));
        else
        {
            if (!_pendingJoinAttempts.TryGetValue(joinedThreadObjectId, out var pending))
                _pendingJoinAttempts[joinedThreadObjectId] = pending = [];
            pending.Add((id, moduleId, methodToken));
        }
    }

    protected virtual void ProcessTaskSchedule(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
    {
        TaskScheduled?.Invoke(new TaskScheduleArgs(id, taskObjectId));
    }

    protected virtual void ProcessTaskStart(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
    {
        TaskStarted?.Invoke(new TaskStartArgs(id, taskObjectId));
    }

    protected virtual void ProcessTaskComplete(ProcessThreadId id, ProcessTrackedObjectId taskObjectId)
    {
        TaskCompleted?.Invoke(new TaskCompleteArgs(id, taskObjectId));
    }

    protected virtual void ProcessTaskJoinFinish(ProcessThreadId id, ProcessTrackedObjectId taskObjectId, bool isSuccess)
    {
        TaskJoinFinished?.Invoke(new TaskJoinFinishArgs(id, taskObjectId, isSuccess));
    }

    protected bool TryGetThreadId(ProcessTrackedObjectId threadObjectId, out ProcessThreadId threadId)
        => _threadObjectRegistry.TryGetThreadId(threadObjectId, out threadId);

    protected ProcessThreadId GetThreadIdFromRegistry(ProcessTrackedObjectId threadObjectId)
        => _threadObjectRegistry.GetThreadId(threadObjectId);

    protected IReadOnlyDictionary<ProcessThreadId, Callstack> GetCallstacksSnapshot()
        => _callStackTracker.GetSnapshot();

    protected IReadOnlySet<ProcessThreadId> GetTrackedThreadIds()
        => _callStackTracker.GetThreadIds();

    private (ProcessThreadId Id, RuntimeArgumentList Arguments, ProcessTrackedObjectId SynchronizationObjectId) ExtractSynchronizationContext(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        var synchronizationObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsT1);
        return (id, arguments, synchronizationObjectId);
    }

    private (ProcessThreadId Id, ProcessTrackedObjectId LockId) PopLockContext(
        RecordedEventMetadata metadata,
        MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, args.ModuleId, args.MethodToken);
        return (id, ExtractSynchronizationObjectIdFromFrame(id, frame));
    }

    private void PopFrame(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, moduleId, methodToken);
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

    private static ProcessTrackedObjectId ExtractSynchronizationObjectIdFromFrame(ProcessThreadId id, StackFrame frame)
        => new(id.ProcessId, frame.Arguments!.Value[0].Value.AsT1);

    private void HandleEventWaitHandleCreate(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args,
        bool isAutoReset,
        bool isBaseConstructor)
    {
        var (id, arguments, eventId) = ExtractSynchronizationContext(metadata, args);
        if (isBaseConstructor && _waitHandleKinds.ContainsKey(eventId))
            return;

        var initialState = (bool)arguments[1].Value.AsT0;
        _waitHandleKinds[eventId] = isAutoReset ? WaitHandleKind.AutoResetEvent : WaitHandleKind.ManualResetEvent;
        EventWaitHandleCreated?.Invoke(new(id, args.ModuleId, args.MethodToken, eventId, initialState, isAutoReset));
    }

    private void HandleWaitHandleWaitExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, moduleId, functionToken);
        var handleId = ExtractSynchronizationObjectIdFromFrame(id, frame);
        DispatchWaitHandleWaitResult(id, moduleId, functionToken, handleId, isSuccess);
    }

    private void HandleWaitHandleSignalAndWaitExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Pop(id);
        EnsureCallStackIntegrity(frame, moduleId, functionToken);
        var awaitedHandleId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments!.Value[1].Value.AsT1);
        DispatchWaitHandleWaitResult(id, moduleId, functionToken, awaitedHandleId, isSuccess);
    }

    private void ProcessAbandonedMutexAcquireReturn(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId mutexId)
    {
        _waitHandleKinds[mutexId] = WaitHandleKind.Mutex;
        if (_lastMutexAcquirers.TryGetValue(mutexId, out var abandonerId) && abandonerId != id)
            LockReleased?.Invoke(new(abandonerId, moduleId, methodToken, mutexId));

        _lastMutexAcquirers[mutexId] = id;
        LockAcquireReturned?.Invoke(new(id, moduleId, methodToken, mutexId, IsSuccess: true));
    }

    private void DispatchWaitMultipleResult(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        TrackedObjectId[] handles,
        bool waitAll,
        int result)
    {
        if (waitAll)
        {
            if (result == WaitHandle.WaitTimeout)
                return;

            foreach (var handle in handles)
                DispatchWaitHandleWaitResult(id, moduleId, methodToken, new ProcessTrackedObjectId(id.ProcessId, handle), isSuccess: true);
        }
        else
        {
            if ((uint)result >= (uint)handles.Length)
                return;

            DispatchWaitHandleWaitResult(id, moduleId, methodToken, new ProcessTrackedObjectId(id.ProcessId, handles[result]), isSuccess: true);
        }
    }

    private void DispatchWaitHandleWaitResult(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId handleId,
        bool isSuccess)
    {
        // Handles without a tracked constructor (process waits, thread-pool internals, ...) are ignored
        if (!_waitHandleKinds.TryGetValue(handleId, out var kind))
            return;

        switch (kind)
        {
            case WaitHandleKind.Mutex:
                if (isSuccess)
                    _lastMutexAcquirers[handleId] = id;
                LockAcquireReturned?.Invoke(new(id, moduleId, methodToken, handleId, isSuccess));
                break;

            case WaitHandleKind.Semaphore:
                SemaphoreAcquireReturned?.Invoke(new(id, moduleId, methodToken, handleId, isSuccess));
                break;

            case WaitHandleKind.AutoResetEvent:
            case WaitHandleKind.ManualResetEvent:
                EventWaitHandleWaitReturned?.Invoke(new(id, moduleId, methodToken, handleId, kind == WaitHandleKind.AutoResetEvent, isSuccess));
                break;
        }
    }

    private void HandleSemaphoreAcquireExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var frame = _callStackTracker.Peek(id);
        EnsureCallStackIntegrity(frame, moduleId, functionToken);
        var semaphoreId = ExtractSynchronizationObjectIdFromFrame(id, frame);
        _callStackTracker.Pop(id);
        SemaphoreAcquireReturned?.Invoke(new(id, moduleId, functionToken, semaphoreId, isSuccess));
    }

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

    private static CapturedStackTrace BuildStack(InstrumentedFieldAccess fieldAccess, byte[]? deeperFramesBlob)
        => new(new CapturedStackFrame(fieldAccess.ModuleId, fieldAccess.MethodToken), deeperFramesBlob);

    private InstrumentedFieldAccess GetInstrumentedStaticFieldAccessFromArguments(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var instrumentationId = MemoryMarshal.Read<ulong>(args.ArgumentValues);
        return InstrumentedFieldAccesses[new InstrumentationPointId(metadata.Pid, instrumentationId)];
    }
    
    private (InstrumentedFieldAccess Access, ProcessTrackedObjectId Instance) GetInstrumentedInstanceFieldAccessFromArguments(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var instrumentationId = MemoryMarshal.Read<ulong>(args.ArgumentValues);
        var access = InstrumentedFieldAccesses[new InstrumentationPointId(metadata.Pid, instrumentationId)];
        var instanceId = MemoryMarshal.Read<nuint>(args.ArgumentValues.AsSpan()[sizeof(ulong)..]);
        var instance = new ProcessTrackedObjectId(metadata.Pid, new TrackedObjectId(instanceId));
        return (access, instance);
    }

    private static void EnsureCallStackIntegrity(StackFrame frame, ModuleId moduleId, MdMethodDef methodToken)
    {
        if (frame.ModuleId != moduleId || frame.MethodToken != methodToken)
            throw new PluginException("Call stack frame does not match the expected method.");
    }
}
