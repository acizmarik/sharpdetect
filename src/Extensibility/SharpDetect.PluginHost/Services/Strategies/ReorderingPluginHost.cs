// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.PluginHost.Services.Strategies.Shadows;

namespace SharpDetect.PluginHost.Services.Strategies;

internal sealed class ReorderingPluginHost(IPluginHost inner, ILogger<ReorderingPluginHost> logger) : IPluginHost, IDisposable
{
    private readonly Dictionary<ProcessTrackedObjectId, ShadowLock> _locks = [];
    private readonly Dictionary<ProcessTrackedObjectId, ShadowSemaphore> _semaphores = [];
    private readonly Dictionary<ProcessTrackedObjectId, ShadowTask> _tasks = [];
    private readonly Dictionary<ProcessThreadId, ShadowThread> _threads = [];
    private readonly Queue<ProcessThreadId> _drainQueue = [];
    private readonly HashSet<ProcessTrackedObjectId> _pendingThreadStarts = [];
    private readonly Dictionary<ProcessTrackedObjectId, ShadowThread> _pendingThreadStartWaiters = [];
    private bool _isDisposed;

    internal int ShadowLockCount => _locks.Count;
    internal int ShadowSemaphoreCount => _semaphores.Count;
    internal int ShadowTaskCount => _tasks.Count;

    public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        var tid = new ProcessThreadId(recordedEvent.Metadata.Pid, recordedEvent.Metadata.Tid);
        var thread = GetOrCreateThread(tid);
        
        var topResult = ProcessOne(thread, recordedEvent);
        if (topResult == RecordedEventState.Deferred)
            thread.EnqueuePendingEvent(recordedEvent);
        
        if (topResult == RecordedEventState.Failed)
            return RecordedEventState.Failed;

        var drainResult = DrainPendingThreads();
        return drainResult == RecordedEventState.Failed
            ? RecordedEventState.Failed
            : topResult;
    }

    private RecordedEventState ProcessOne(ShadowThread thread, RecordedEvent recordedEvent)
    {
        if (thread.BlockedOn is not null)
        {
            if (recordedEvent.EventArgs is MethodEnterWithArgumentsRecordedEvent deferredEnter &&
                (RecordedEventType)deferredEnter.Interpretation == RecordedEventType.ThreadStartCore)
            {
                _pendingThreadStarts.Add(ReadTargetId(deferredEnter.ArgumentValues, thread.Id.ProcessId));
            }
            return RecordedEventState.Deferred;
        }

        return recordedEvent.EventArgs switch
        {
            MethodEnterWithArgumentsRecordedEvent enter => HandleEnter(recordedEvent, enter, thread.Id, thread),
            MethodExitRecordedEvent exit => HandleExit(recordedEvent, exit.Interpretation, thread.Id, thread, returnValue: null),
            MethodExitWithArgumentsRecordedEvent exit => HandleExit(recordedEvent, exit.Interpretation, thread.Id, thread, exit.ReturnValue),
            ThreadDestroyRecordedEvent destroy => HandleThreadDestroy(recordedEvent, destroy),
            GarbageCollectedTrackedObjectsRecordedEvent gc => HandleGarbageCollectedTrackedObjects(recordedEvent, gc),
            _ => inner.ProcessEvent(recordedEvent)
        };
    }

    private RecordedEventState HandleEnter(
        RecordedEvent recordedEvent,
        MethodEnterWithArgumentsRecordedEvent enter,
        ProcessThreadId tid,
        ShadowThread thread)
    {
        var kind = (RecordedEventType)enter.Interpretation;
        switch (kind)
        {
            case RecordedEventType.SemaphoreCreate:
            {
                var semaphoreId = ReadTargetId(enter.ArgumentValues, tid.ProcessId);
                var initialCount = MemoryMarshal.Read<int>(enter.ArgumentValues.AsSpan()[(byte)nint.Size..]);
                var capacity = MemoryMarshal.Read<int>(enter.ArgumentValues.AsSpan()[((byte)nint.Size + sizeof(int))..]);
                CreateSemaphore(semaphoreId, initialCount, capacity);
                return inner.ProcessEvent(recordedEvent);
            }

            case RecordedEventType.MonitorLockAcquire:
            case RecordedEventType.MonitorLockTryAcquire:
            case RecordedEventType.LockAcquire:
            case RecordedEventType.LockTryAcquire:
            case RecordedEventType.MonitorLockRelease:
            case RecordedEventType.LockRelease:
            case RecordedEventType.SemaphoreAcquire:
            case RecordedEventType.SemaphoreTryAcquire:
            case RecordedEventType.TaskJoinStart:
                thread.SyncTargetStack.Push(ReadTargetId(enter.ArgumentValues, tid.ProcessId));
                return inner.ProcessEvent(recordedEvent);

            case RecordedEventType.SemaphoreRelease:
            {
                thread.SyncTargetStack.Push(ReadTargetId(enter.ArgumentValues, tid.ProcessId));
                thread.PendingReleaseCount = MemoryMarshal.Read<int>(enter.ArgumentValues.AsSpan()[(byte)nint.Size..]);
                return inner.ProcessEvent(recordedEvent);
            }

            case RecordedEventType.TaskStart:
            {
                var taskId = ReadTargetId(enter.ArgumentValues, tid.ProcessId);
                thread.SyncTargetStack.Push(taskId);
                GetOrCreateTask(taskId).AttachOwner(tid);
                return inner.ProcessEvent(recordedEvent);
            }

            case RecordedEventType.MonitorWaitAttempt:
            {
                var target = ReadTargetId(enter.ArgumentValues, tid.ProcessId);
                var shadowLock = GetOrCreateLock(target);
                if (!shadowLock.TryReleaseForWait(tid, out var nextWaiter, out var suspendedCount))
                {
                    // Waiting without ownership throws at runtime
                    // We are not pushing the target (there won't be matching reacquire
                    return inner.ProcessEvent(recordedEvent);
                }

                thread.SyncTargetStack.Push(target);
                thread.SuspendedWaitCount = suspendedCount.Value;
                if (nextWaiter is not null)
                    _drainQueue.Enqueue(nextWaiter.Value);
                return inner.ProcessEvent(recordedEvent);
            }

            case RecordedEventType.ThreadStartCore:
            {
                var objectId = ReadTargetId(enter.ArgumentValues, tid.ProcessId);
                _pendingThreadStarts.Remove(objectId);
                if (_pendingThreadStartWaiters.Remove(objectId, out var childThread))
                    _drainQueue.Enqueue(childThread.Id);
                return inner.ProcessEvent(recordedEvent);
            }

            case RecordedEventType.ThreadStartCallback:
            {
                var objectId = ReadTargetId(enter.ArgumentValues, tid.ProcessId);
                if (!_pendingThreadStarts.Contains(objectId))
                    return inner.ProcessEvent(recordedEvent);
                
                _pendingThreadStartWaiters[objectId] = thread;
                return Defer(thread, objectId);
            }

            default:
                return inner.ProcessEvent(recordedEvent);
        }
    }

    private RecordedEventState HandleExit(
        RecordedEvent recordedEvent,
        ushort interpretation,
        ProcessThreadId tid,
        ShadowThread thread,
        byte[]? returnValue)
    {
        var kind = (RecordedEventType)interpretation;
        switch (kind)
        {
            case RecordedEventType.MonitorLockAcquireResult:
            case RecordedEventType.LockAcquireResult:
                return HandleAcquireResult(recordedEvent, tid, thread, ReadBoolOrDefault(returnValue, true));

            case RecordedEventType.MonitorLockReleaseResult:
            case RecordedEventType.LockReleaseResult:
                return HandleReleaseResult(recordedEvent, tid, thread);

            case RecordedEventType.MonitorWaitResult:
                return HandleWaitResult(recordedEvent, tid, thread);

            case RecordedEventType.SemaphoreAcquireResult:
                return HandleSemaphoreAcquireResult(recordedEvent, tid, thread, ReadBoolOrDefault(returnValue, true));

            case RecordedEventType.SemaphoreReleaseResult:
                return HandleSemaphoreReleaseResult(recordedEvent, tid, thread);

            case RecordedEventType.TaskComplete:
                return HandleTaskComplete(recordedEvent, thread);

            case RecordedEventType.TaskJoinFinish:
                return HandleTaskJoinFinish(recordedEvent, tid, thread, ReadBoolOrDefault(returnValue, true));

            case RecordedEventType.MonitorPulseOneResult:
            case RecordedEventType.MonitorPulseAllResult:
            case RecordedEventType.ThreadJoinResult:
            default:
                return inner.ProcessEvent(recordedEvent);
        }
    }

    private RecordedEventState HandleAcquireResult(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread, bool success)
    {
        if (!TryPeekTarget(thread, out var lockId))
            return inner.ProcessEvent(recordedEvent);

        if (success && !GetOrCreateLock(lockId).TryAcquire(tid))
            return Defer(thread, lockId);
        
        thread.SyncTargetStack.Pop();
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleReleaseResult(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread)
    {
        if (!TryPopTarget(thread, out var lockId))
            return inner.ProcessEvent(recordedEvent);

        var result = inner.ProcessEvent(recordedEvent);
        if (_locks.TryGetValue(lockId, out var shadow))
            EnqueueDrain(shadow.Release(tid));
        
        return result;
    }

    private RecordedEventState HandleWaitResult(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread)
    {
        if (!TryPeekTarget(thread, out var lockId))
            return inner.ProcessEvent(recordedEvent);


        var suspended = thread.SuspendedWaitCount ?? 1;
        if (!GetOrCreateLock(lockId).TryReacquireAfterWait(tid, suspended))
            return Defer(thread, lockId);
        
        thread.SyncTargetStack.Pop();
        thread.SuspendedWaitCount = null;
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleSemaphoreAcquireResult(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread, bool success)
    {
        if (!TryPeekTarget(thread, out var semaphoreId))
            return inner.ProcessEvent(recordedEvent);

        if (success && !_semaphores[semaphoreId].TryAcquire(tid))
            return Defer(thread, semaphoreId);
        
        thread.SyncTargetStack.Pop();
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleSemaphoreReleaseResult(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread)
    {
        if (!TryPopTarget(thread, out var semaphoreId))
            return inner.ProcessEvent(recordedEvent);

        var count = thread.PendingReleaseCount!.Value;
        thread.PendingReleaseCount = null;

        var result = inner.ProcessEvent(recordedEvent);
        foreach (var waiter in _semaphores[semaphoreId].Release(tid, count))
            _drainQueue.Enqueue(waiter);
        return result;
    }

    private RecordedEventState HandleTaskComplete(RecordedEvent recordedEvent, ShadowThread thread)
    {
        if (!TryPopTarget(thread, out var taskId))
            return inner.ProcessEvent(recordedEvent);

        var result = inner.ProcessEvent(recordedEvent);
        foreach (var waiter in GetOrCreateTask(taskId).Complete())
            _drainQueue.Enqueue(waiter);
        
        return result;
    }

    private RecordedEventState HandleTaskJoinFinish(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread, bool success)
    {
        if (!TryPeekTarget(thread, out var taskId))
            return inner.ProcessEvent(recordedEvent);

        if (!success)
        {
            thread.SyncTargetStack.Pop();
            return inner.ProcessEvent(recordedEvent);
        }

        // Only tasks whose start we observed can defer; others like Task.CompletedTask are pass-through
        if (_tasks.TryGetValue(taskId, out var shadow) && shadow.RegisterWaiter(tid))
            return Defer(thread, taskId);

        thread.SyncTargetStack.Pop();
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleThreadDestroy(RecordedEvent recordedEvent, ThreadDestroyRecordedEvent destroy)
    {
        var pid = recordedEvent.Metadata.Pid;
        var destroyedTid = new ProcessThreadId(pid, destroy.ThreadId);

        foreach (var (lockId, shadow) in _locks)
        {
            shadow.RemoveWaiter(destroyedTid);
            var next = shadow.AbandonByDestroy(destroyedTid);
            if (next is null)
                continue;
            
            logger.LogWarning("Thread {Thread} destroyed while still owning lock {Lock}.", destroyedTid, lockId);
            _drainQueue.Enqueue(next.Value);
        }

        foreach (var (semaphoreId, shadow) in _semaphores)
        {
            shadow.RemoveWaiter(destroyedTid);
            var abandonedPermits = shadow.AbandonPermitsByDestroy(destroyedTid);
            if (abandonedPermits > 0)
            {
                logger.LogWarning(
                    "Thread {Thread} destroyed while holding {Permits} outstanding permit(s) on semaphore {Semaphore}; permits will not be released.",
                    destroyedTid, abandonedPermits, semaphoreId);
            }
        }

        foreach (var (taskId, shadow) in _tasks)
        {
            shadow.RemoveWaiter(destroyedTid);
            if (shadow.Completed || shadow.OwnerThreadId != destroyedTid)
                continue;

            logger.LogWarning("Thread {Thread} destroyed mid-task {Task}; completing task and waking joiners.", destroyedTid, taskId);
            foreach (var waiter in shadow.Complete())
                _drainQueue.Enqueue(waiter);
        }

        _threads.Remove(destroyedTid);
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleGarbageCollectedTrackedObjects(
        RecordedEvent recordedEvent,
        GarbageCollectedTrackedObjectsRecordedEvent gc)
    {
        var pid = recordedEvent.Metadata.Pid;
        foreach (var objectId in gc.RemovedTrackedObjectIds)
        {
            var key = new ProcessTrackedObjectId(pid, objectId);

            if (_locks.TryGetValue(key, out var shadowLock))
            {
                if (shadowLock.TryDescribeResidualState(out var description))
                    logger.LogWarning("Lock {Lock} garbage collected with residual state: {State}.", key, description);
                _locks.Remove(key);
                continue;
            }

            if (_semaphores.TryGetValue(key, out var shadowSemaphore))
            {
                if (shadowSemaphore.TryDescribeResidualState(out var description))
                    logger.LogWarning("Semaphore {Semaphore} garbage collected with residual state: {State}.", key, description);
                _semaphores.Remove(key);
                continue;
            }

            if (_tasks.TryGetValue(key, out var shadowTask))
            {
                if (shadowTask.TryDescribeResidualState(out var description))
                    logger.LogWarning("Task {Task} garbage collected with residual state: {State}.", key, description);
                _tasks.Remove(key);
            }
        }

        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState DrainPendingThreads()
    {
        while (_drainQueue.Count > 0)
        {
            var tid = _drainQueue.Dequeue();
            if (!_threads.TryGetValue(tid, out var thread))
                continue;

            thread.ClearBlockedOn();
            while (thread.BlockedOn is null && thread.PendingQueueCount > 0)
            {
                var pendingEvent = thread.PeekPendingEvent();
                var state = ProcessOne(thread, pendingEvent);
                if (state == RecordedEventState.Failed)
                    return RecordedEventState.Failed;

                if (state == RecordedEventState.Deferred)
                    break;
                
                thread.DequeuePendingEvent();
            }
        }
        return RecordedEventState.Executed;
    }

    private static RecordedEventState Defer(ShadowThread thread, ProcessTrackedObjectId target)
    {
        thread.SetBlockedOn(target);
        return RecordedEventState.Deferred;
    }

    private void EnqueueDrain(ProcessThreadId? waiter)
    {
        if (waiter is not null)
            _drainQueue.Enqueue(waiter.Value);
    }

    private ShadowThread GetOrCreateThread(ProcessThreadId tid)
    {
        if (!_threads.TryGetValue(tid, out var thread))
            _threads[tid] = thread = new ShadowThread(tid, logger);
        return thread;
    }

    private ShadowLock GetOrCreateLock(ProcessTrackedObjectId lockId)
    {
        if (!_locks.TryGetValue(lockId, out var shadow))
            _locks[lockId] = shadow = new ShadowLock();
        return shadow;
    }

    private ShadowSemaphore CreateSemaphore(ProcessTrackedObjectId semaphoreId, int initialCount, int capacity)
    {
        return _semaphores[semaphoreId] = new ShadowSemaphore(initialCount, capacity);
    }

    private ShadowTask GetOrCreateTask(ProcessTrackedObjectId taskId)
    {
        if (!_tasks.TryGetValue(taskId, out var shadow))
            _tasks[taskId] = shadow = new ShadowTask();
        return shadow;
    }

    private static ProcessTrackedObjectId ReadTargetId(byte[] argumentValues, uint pid)
    {
        var raw = MemoryMarshal.Read<nuint>(argumentValues);
        return new ProcessTrackedObjectId(pid, new TrackedObjectId(raw));
    }

    private static bool ReadBoolOrDefault(byte[]? buffer, bool defaultValue)
    {
        if (buffer is null || buffer.Length == 0)
            return defaultValue;
        return MemoryMarshal.Read<bool>(buffer);
    }
    
    private static bool TryPeekTarget(ShadowThread thread, out ProcessTrackedObjectId target)
    {
        if (thread.SyncTargetStack.Count == 0)
        {
            target = default;
            return false;
        }
        
        target = thread.SyncTargetStack.Peek();
        return true;
    }

    private static bool TryPopTarget(ShadowThread thread, out ProcessTrackedObjectId target)
    {
        if (thread.SyncTargetStack.Count == 0)
        {
            target = default;
            return false;
        }
        
        target = thread.SyncTargetStack.Pop();
        return true;
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;
        _isDisposed = true;
        if (inner is IDisposable disposable)
            disposable.Dispose();
    }
}
