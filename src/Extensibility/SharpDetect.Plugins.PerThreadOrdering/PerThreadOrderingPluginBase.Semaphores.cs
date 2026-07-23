// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private readonly Dictionary<ProcessTrackedObjectId, ProcessTrackedObjectId> _waitAsyncTaskToSemaphore = [];

    public event Action<SemaphoreCreatedArgs>? SemaphoreCreated;
    public event Action<SemaphoreAcquireAttemptArgs>? SemaphoreAcquireAttempted;
    public event Action<SemaphoreAcquireResultArgs>? SemaphoreAcquireReturned;
    public event Action<SemaphoreReleaseArgs>? SemaphoreReleased;
    public event Action<SemaphoreWaitAsyncArgs>? SemaphoreWaitAsyncReturned;

    private void RegisterSemaphoreBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SemaphoreCreate, OnSemaphoreCreateEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SemaphoreAcquire, OnSemaphoreAcquireEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SemaphoreTryAcquire, OnSemaphoreAcquireEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SemaphoreAcquireResult, OnSemaphoreAcquireExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.SemaphoreAcquireResult, OnSemaphoreAcquireExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SemaphoreRelease, OnSemaphoreReleaseEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SemaphoreReleaseResult, OnSemaphoreReleaseExit);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.SemaphoreWaitAsync, OnSemaphoreWaitAsyncEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.SemaphoreWaitAsyncResult, OnSemaphoreWaitAsyncExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.SemaphoreWaitAsyncResult, OnSemaphoreWaitAsyncExitWithArguments);
    }

    private void OnSemaphoreCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, semaphoreId) = ExtractSynchronizationContext(metadata, args);
        using var _ = arguments;
        var initialCount = (int)arguments[1].Value.AsPrimitive;
        _waitHandleKinds[semaphoreId] = WaitHandleKind.Semaphore;
        SemaphoreCreated?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId, initialCount));
    }

    private void OnSemaphoreAcquireEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, semaphoreId) = PushSynchronizationContext(metadata, args);
        SemaphoreAcquireAttempted?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId));
    }

    private void OnSemaphoreAcquireExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleSemaphoreAcquireExit(metadata, args.ModuleId, args.MethodToken, isSuccess: true);

    private void OnSemaphoreAcquireExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleSemaphoreAcquireExit(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    private void OnSemaphoreReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, semaphoreId) = PushSynchronizationContext(metadata, args);
        var releaseCount = (int)arguments[1].Value.AsPrimitive;
        SemaphoreReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId, releaseCount));
    }

    private void OnSemaphoreReleaseExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnSemaphoreWaitAsyncEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        PushSynchronizationContext(metadata, args);
    }

    private void OnSemaphoreWaitAsyncExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnSemaphoreWaitAsyncExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, args.ModuleId, args.MethodToken);
        var semaphoreId = new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![0].Value.AsTrackedObject);
        var waiterTaskId = new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ReturnValue)));
        _waitAsyncTaskToSemaphore[waiterTaskId] = semaphoreId;
    }

    private void HandleSemaphoreAcquireExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, functionToken);
        var semaphoreId = ExtractSynchronizationObjectIdFromFrame(id, frameLease.Frame);
        SemaphoreAcquireReturned?.Invoke(new(id, moduleId, functionToken, semaphoreId, isSuccess));
    }
}
