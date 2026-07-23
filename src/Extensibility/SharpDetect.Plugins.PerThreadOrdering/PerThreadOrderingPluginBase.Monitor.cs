// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    public event Action<LockAcquireAttemptArgs>? LockAcquireAttempted;
    public event Action<LockAcquireResultArgs>? LockAcquireReturned;
    public event Action<LockReleaseArgs>? LockReleased;
    public event Action<ObjectPulseOneArgs>? ObjectPulsedOne;
    public event Action<ObjectPulseAllArgs>? ObjectPulsedAll;
    public event Action<ObjectWaitAttemptArgs>? ObjectWaitAttempted;
    public event Action<ObjectWaitResultArgs>? ObjectWaitReturned;

    private void RegisterMonitorBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.LockAcquire, OnLockAcquireEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.LockTryAcquire, OnLockAcquireEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MonitorLockAcquire, OnLockAcquireEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MonitorLockTryAcquire, OnLockAcquireEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.LockAcquireResult, OnLockAcquireExit);
        Bind<MethodExitRecordedEvent>(RecordedEventType.MonitorLockAcquireResult, OnLockAcquireExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.LockAcquireResult, OnLockAcquireExitWithArguments);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.MonitorLockAcquireResult, OnLockAcquireExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.LockRelease, OnLockReleaseEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MonitorLockRelease, OnLockReleaseEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.LockReleaseResult, OnLockReleaseResultExit);
        Bind<MethodExitRecordedEvent>(RecordedEventType.MonitorLockReleaseResult, OnLockReleaseResultExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.LockReleaseResult, OnLockReleaseResultExitWithArguments);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.MonitorLockReleaseResult, OnLockReleaseResultExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MonitorPulseOneAttempt, OnMonitorPulseOneAttempt);
        Bind<MethodExitRecordedEvent>(RecordedEventType.MonitorPulseOneResult, OnMonitorPulseOneResult);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MonitorPulseAllAttempt, OnMonitorPulseAllAttempt);
        Bind<MethodExitRecordedEvent>(RecordedEventType.MonitorPulseAllResult, OnMonitorPulseAllResult);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MonitorWaitAttempt, OnMonitorWaitAttempt);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.MonitorWaitResult, OnMonitorWaitResult);
    }

    private void OnLockAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, lockId) = PushSynchronizationContext(metadata, args);
        LockAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, lockId));
    }

    private void OnLockAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken: true);

    private void OnLockAcquireExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var lockTaken = DetermineLockTakenFromExitEvent(metadata, args);
        HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken);
    }

    private void OnLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, lockId) = PushSynchronizationContext(metadata, args);
        var lockTaken = arguments.Count == 1 || (bool)arguments[1].Value.AsPrimitive;
        if (lockTaken)
            ProcessLockRelease(id, args.ModuleId, args.MethodToken, lockId);
    }

    private void OnLockReleaseResultExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnLockReleaseResultExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnMonitorPulseOneAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    private void OnMonitorPulseOneResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockId) = PopLockContext(metadata, args);
        ProcessPulseOne(id, args.ModuleId, args.MethodToken, lockId);
    }

    private void OnMonitorPulseAllAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(metadata, args);

    private void OnMonitorPulseAllResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var (id, lockId) = PopLockContext(metadata, args);
        ProcessPulseAll(id, args.ModuleId, args.MethodToken, lockId);
    }

    private void OnMonitorWaitAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, lockId) = PushSynchronizationContext(metadata, args);
        OnBeforeWaitAttempt(id, lockId);
        ProcessWaitAttempt(id, args.ModuleId, args.MethodToken, lockId);
    }

    private void OnMonitorWaitResult(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, args.ModuleId, args.MethodToken);
        var lockId = ExtractSynchronizationObjectIdFromFrame(id, frameLease.Frame);
        var success = MemoryMarshal.Read<bool>(args.ReturnValue);
        ProcessWaitReturn(id, args.ModuleId, args.MethodToken, lockId, success);
    }

    protected virtual void HandleLockAcquireExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool lockTaken)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, functionToken);
        var lockId = ExtractSynchronizationObjectIdFromFrame(id, frameLease.Frame);

        if (!lockTaken)
        {
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
        ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(id, moduleId, methodToken, lockId, success));
    }

    protected virtual void OnAfterWaitReturn(ProcessThreadId id, ProcessTrackedObjectId lockId)
    {
    }

    private bool DetermineLockTakenFromExitEvent(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        if (args.ReturnValue.Length > 0)
            return MemoryMarshal.Read<bool>(args.ReturnValue);

        if (args.ByRefArgumentValues.Length > 0)
        {
            using var byRefArguments = ParseArguments(metadata, args);
            return (bool)byRefArguments[0].Value.AsPrimitive;
        }

        return true;
    }
}
