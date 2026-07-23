// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private enum WaitHandleKind
    {
        Mutex,
        Semaphore,
        AutoResetEvent,
        ManualResetEvent
    }

    private readonly Dictionary<ProcessTrackedObjectId, WaitHandleKind> _waitHandleKinds = [];
    private readonly Dictionary<ProcessTrackedObjectId, ProcessThreadId> _lastMutexAcquirers = [];
    private readonly Dictionary<ProcessThreadId, ProcessTrackedObjectId?> _pendingAbandonedMutexes = [];

    public event Action<EventWaitHandleCreatedArgs>? EventWaitHandleCreated;
    public event Action<EventWaitHandleSetArgs>? EventWaitHandleSignaled;
    public event Action<EventWaitHandleResetArgs>? EventWaitHandleWasReset;
    public event Action<EventWaitHandleWaitResultArgs>? EventWaitHandleWaitReturned;

    private void RegisterWaitHandleBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MutexCreate, OnMutexCreateEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.MutexRelease, OnMutexReleaseEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.MutexReleaseResult, OnMutexReleaseExit);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.AutoResetEventCreate, OnAutoResetEventCreateEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.ManualResetEventCreate, OnManualResetEventCreateEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.EventWaitHandleCreate, OnEventWaitHandleCreateEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.EventWaitHandleSet, OnEventWaitHandleSetEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.EventWaitHandleSetResult, OnEventWaitHandleSetExit);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.EventWaitHandleReset, OnEventWaitHandleResetEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.EventWaitHandleResetResult, OnEventWaitHandleResetExit);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.WaitHandleWait, OnWaitHandleWaitEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.WaitHandleWaitResult, OnWaitHandleWaitExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.WaitHandleWaitResult, OnWaitHandleWaitExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.WaitHandleSignalAndWait, OnWaitHandleSignalAndWaitEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.WaitHandleSignalAndWaitResult, OnWaitHandleSignalAndWaitExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.WaitHandleSignalAndWaitResult, OnWaitHandleSignalAndWaitExitWithArguments);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.AbandonedMutexExceptionCreate, OnAbandonedMutexExceptionCreateEnter);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.WaitHandleWaitMultiple, OnWaitHandleWaitMultipleEnter);
        Bind<MethodExitRecordedEvent>(RecordedEventType.WaitHandleWaitMultipleResult, OnWaitHandleWaitMultipleExit);
        Bind<MethodExitWithArgumentsRecordedEvent>(RecordedEventType.WaitHandleWaitMultipleResult, OnWaitHandleWaitMultipleExitWithArguments);
    }

    private void OnMutexCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (_, arguments, mutexId) = ExtractSynchronizationContext(metadata, args);
        using var _ = arguments;
        _waitHandleKinds[mutexId] = WaitHandleKind.Mutex;
    }

    private void OnMutexReleaseEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, mutexId) = PushSynchronizationContext(metadata, args);
        // ReleaseMutex proves the kind even when the constructor was not observed (for example, OpenExisting)
        _waitHandleKinds[mutexId] = WaitHandleKind.Mutex;
        LockReleased?.Invoke(new(id, args.ModuleId, args.MethodToken, mutexId));
    }

    private void OnMutexReleaseExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnAutoResetEventCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleEventWaitHandleCreate(metadata, args, isAutoReset: true, isBaseConstructor: false);

    private void OnManualResetEventCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleEventWaitHandleCreate(metadata, args, isAutoReset: false, isBaseConstructor: false);

    private void OnEventWaitHandleCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => HandleEventWaitHandleCreate(metadata, args, isAutoReset: false, isBaseConstructor: true);

    private void OnEventWaitHandleSetEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, eventId) = PushSynchronizationContext(metadata, args);
        if (!_waitHandleKinds.TryGetValue(eventId, out var kind))
        {
            // Unknown kind (for example, OpenExisting)
            // Manual-reset should over-approximate happens-before
            _waitHandleKinds[eventId] = kind = WaitHandleKind.ManualResetEvent;
        }

        EventWaitHandleSignaled?.Invoke(new(id, args.ModuleId, args.MethodToken, eventId, kind == WaitHandleKind.AutoResetEvent));
    }

    private void OnEventWaitHandleSetExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnEventWaitHandleResetEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, eventId) = PushSynchronizationContext(metadata, args);
        EventWaitHandleWasReset?.Invoke(new(id, args.ModuleId, args.MethodToken, eventId));
    }

    private void OnEventWaitHandleResetExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnWaitHandleWaitEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, handleId) = PushSynchronizationContext(metadata, args);
        _pendingAbandonedMutexes.Remove(id);
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

    private void OnWaitHandleWaitExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleWaitHandleWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess: true);

    private void OnWaitHandleWaitExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleWaitHandleWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    private void OnWaitHandleSignalAndWaitEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, arguments, toSignalId) = PushSynchronizationContext(metadata, args);
        var toWaitOnId = new ProcessTrackedObjectId(id.ProcessId, arguments[1].Value.AsTrackedObject);
        _pendingAbandonedMutexes.Remove(id);

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

    private void OnWaitHandleSignalAndWaitExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => HandleWaitHandleSignalAndWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess: true);

    private void OnWaitHandleSignalAndWaitExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var isSuccess = MemoryMarshal.Read<bool>(args.ReturnValue);
        HandleWaitHandleSignalAndWaitExit(metadata, args.ModuleId, args.MethodToken, isSuccess);
    }

    private void OnAbandonedMutexExceptionCreateEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        _pendingAbandonedMutexes[id] = arguments.Count > 1
            ? new ProcessTrackedObjectId(id.ProcessId, arguments[1].Value.AsTrackedObject)
            : null;
    }

    private void OnWaitHandleWaitMultipleEnter(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        _pendingAbandonedMutexes.Remove(id);
        PushArgumentsOnCallStack(id, metadata, args);
    }

    private void OnWaitHandleWaitMultipleExit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => PopFrame(metadata, args.ModuleId, args.MethodToken);

    private void OnWaitHandleWaitMultipleExitWithArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, args.ModuleId, args.MethodToken);
        var handles = frameLease.Frame.Arguments![0].Value.AsTrackedObjectArray;
        var waitAll = (bool)frameLease.Frame.Arguments![1].Value.AsPrimitive;
        var result = MemoryMarshal.Read<int>(args.ReturnValue);
        DispatchWaitMultipleResult(id, args.ModuleId, args.MethodToken, handles, waitAll, result);
    }

    private void HandleEventWaitHandleCreate(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args,
        bool isAutoReset,
        bool isBaseConstructor)
    {
        var (id, arguments, eventId) = ExtractSynchronizationContext(metadata, args);
        using var _ = arguments;
        if (isBaseConstructor && _waitHandleKinds.ContainsKey(eventId))
            return;

        var initialState = (bool)arguments[1].Value.AsPrimitive;
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
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, functionToken);
        var handleId = ExtractSynchronizationObjectIdFromFrame(id, frameLease.Frame);
        DispatchWaitHandleWaitResult(id, moduleId, functionToken, handleId, isSuccess);
    }

    private void HandleWaitHandleSignalAndWaitExit(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef functionToken,
        bool isSuccess)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, moduleId, functionToken);
        var awaitedHandleId = new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![1].Value.AsTrackedObject);
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
}
