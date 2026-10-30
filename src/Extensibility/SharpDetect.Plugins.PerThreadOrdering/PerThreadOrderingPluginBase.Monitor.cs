// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private void RegisterMonitorBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.LockAcquire, OnLockAcquireEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.LockTryAcquire, OnLockAcquireEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.LockAcquireResult, OnLockAcquireExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.LockAcquireResult, OnLockAcquireExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.LockRelease, OnLockReleaseEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.LockReleaseResult, OnLockReleaseResultExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.LockReleaseResult, OnLockReleaseResultExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SharedLockAcquire, OnSharedLockAcquireEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SharedLockAcquireResult, OnSharedLockAcquireExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.SharedLockAcquireResult, OnSharedLockAcquireExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SharedLockRelease, OnSharedLockReleaseEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SharedLockReleaseResult, OnLockReleaseResultExit);
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
        RaiseLockAcquireAttempted(new(id, args.ModuleId, args.MethodToken, lockId));
    }

    private void OnLockAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken: true);

    private void OnLockAcquireExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var lockTaken = DetermineSuccessFromExitEvent(metadata, args);
        HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken);
    }

    private void OnLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, lockId) = PushSynchronizationContext(metadata, args);
        var lockTaken = arguments.Count == 1 || (bool)arguments[1].Value.AsPrimitive;
        if (lockTaken)
            ProcessLockRelease(id, args.ModuleId, args.MethodToken, lockId);
    }

    private void OnSharedLockAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, lockId) = PushSynchronizationContext(metadata, args);
        RaiseLockAcquireAttempted(new(id, args.ModuleId, args.MethodToken, lockId, IsShared: true));
    }

    private void OnSharedLockAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken: true, isShared: true);

    private void OnSharedLockAcquireExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var lockTaken = DetermineSuccessFromExitEvent(metadata, args);
        HandleLockAcquireExit(metadata, args.ModuleId, args.MethodToken, lockTaken, isShared: true);
    }

    private void OnSharedLockReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, lockId) = PushSynchronizationContext(metadata, args);
        RaiseLockReleased(new(id, args.ModuleId, args.MethodToken, lockId, IsShared: true));
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

    private void HandleLockAcquireExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool lockTaken,
        bool isShared = false)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, functionToken);
        var lockId = ExtractSynchronizationObjectIdFromFrame(id, frameLease.Frame);
        RaiseLockAcquireReturned(new(id, moduleId, functionToken, lockId, lockTaken, isShared));
    }

    private void ProcessLockRelease(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        RaiseLockReleased(new(id, moduleId, methodToken, lockId));
    }

    private void ProcessPulseOne(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        RaiseObjectPulsedOne(new ObjectPulseOneArgs(id, moduleId, methodToken, lockId));
    }

    private void ProcessPulseAll(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        RaiseObjectPulsedAll(new ObjectPulseAllArgs(id, moduleId, methodToken, lockId));
    }

    private void ProcessWaitAttempt(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId)
    {
        RaiseObjectWaitAttempted(new ObjectWaitAttemptArgs(id, moduleId, methodToken, lockId));
    }

    private void ProcessWaitReturn(
        ProcessThreadId id,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ProcessTrackedObjectId lockId,
        bool success)
    {
        RaiseObjectWaitReturned(new ObjectWaitResultArgs(id, moduleId, methodToken, lockId, success));
    }

    private bool DetermineSuccessFromExitEvent(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
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
