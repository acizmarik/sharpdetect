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
    private bool _isDisposed;

    public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        var topResult = ProcessOne(recordedEvent);
        if (topResult == RecordedEventState.Failed)
            return RecordedEventState.Failed;

        var drainResult = DrainPendingThreads();
        return drainResult == RecordedEventState.Failed
            ? RecordedEventState.Failed
            : topResult;
    }

    private RecordedEventState ProcessOne(RecordedEvent recordedEvent)
    {
        var tid = new ProcessThreadId(recordedEvent.Metadata.Pid, recordedEvent.Metadata.Tid);
        var thread = GetOrCreateThread(tid);

        if (thread.BlockedOn is not null)
        {
            thread.PendingQueue.AddLast(recordedEvent);
            return RecordedEventState.Deferred;
        }

        return recordedEvent.EventArgs switch
        {
            MethodEnterWithArgumentsRecordedEvent enter => HandleEnter(recordedEvent, enter, tid, thread),
            MethodExitRecordedEvent exit => HandleExit(recordedEvent, exit.Interpretation, tid, thread, returnValue: null),
            MethodExitWithArgumentsRecordedEvent exit => HandleExit(recordedEvent, exit.Interpretation, tid, thread, exit.ReturnValue),
            ThreadDestroyRecordedEvent destroy => HandleThreadDestroy(recordedEvent, destroy),
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
                GetOrCreateSemaphore(semaphoreId).Initialize(initialCount);
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
            case RecordedEventType.SemaphoreRelease:
            case RecordedEventType.TaskJoinStart:
                thread.SyncTargetStack.Push(ReadTargetId(enter.ArgumentValues, tid.ProcessId));
                return inner.ProcessEvent(recordedEvent);

            case RecordedEventType.TaskStart:
            {
                var taskId = ReadTargetId(enter.ArgumentValues, tid.ProcessId);
                thread.SyncTargetStack.Push(taskId);
                GetOrCreateTask(taskId);
                return inner.ProcessEvent(recordedEvent);
            }

            case RecordedEventType.MonitorWaitAttempt:
            {
                var target = ReadTargetId(enter.ArgumentValues, tid.ProcessId);
                thread.SyncTargetStack.Push(target);
                var shadowLock = GetOrCreateLock(target);
                if (!shadowLock.TryReleaseForWait(tid, out var nextWaiter, out var suspendedCount))
                    return RecordedEventState.Discarded;
                
                thread.SuspendedWaitCount = suspendedCount.Value;
                if (nextWaiter is not null)
                    _drainQueue.Enqueue(nextWaiter.Value);
                return inner.ProcessEvent(recordedEvent);
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
                return HandleWaitResult(recordedEvent, tid, thread, ReadBoolOrDefault(returnValue, true));

            case RecordedEventType.SemaphoreAcquireResult:
                return HandleSemaphoreAcquireResult(recordedEvent, tid, thread, ReadBoolOrDefault(returnValue, true));

            case RecordedEventType.SemaphoreReleaseResult:
                return HandleSemaphoreReleaseResult(recordedEvent, thread);

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
            return Defer(thread, lockId, recordedEvent);
        
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

    private RecordedEventState HandleWaitResult(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread, bool success)
    {
        if (!TryPeekTarget(thread, out var lockId))
            return inner.ProcessEvent(recordedEvent);

        if (!success)
        {
            thread.SyncTargetStack.Pop();
            thread.SuspendedWaitCount = null;
            return inner.ProcessEvent(recordedEvent);
        }

        var suspended = thread.SuspendedWaitCount ?? 1;
        if (!GetOrCreateLock(lockId).TryReacquireAfterWait(tid, suspended))
            return Defer(thread, lockId, recordedEvent);
        
        thread.SyncTargetStack.Pop();
        thread.SuspendedWaitCount = null;
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleSemaphoreAcquireResult(RecordedEvent recordedEvent, ProcessThreadId tid, ShadowThread thread, bool success)
    {
        if (!TryPeekTarget(thread, out var semaphoreId))
            return inner.ProcessEvent(recordedEvent);

        if (success && !GetOrCreateSemaphore(semaphoreId).TryAcquire(tid))
            return Defer(thread, semaphoreId, recordedEvent);
        
        thread.SyncTargetStack.Pop();
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleSemaphoreReleaseResult(RecordedEvent recordedEvent, ShadowThread thread)
    {
        if (!TryPopTarget(thread, out var semaphoreId))
            return inner.ProcessEvent(recordedEvent);

        var result = inner.ProcessEvent(recordedEvent);
        EnqueueDrain(GetOrCreateSemaphore(semaphoreId).Release());
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
            return Defer(thread, taskId, recordedEvent);

        thread.SyncTargetStack.Pop();
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState HandleThreadDestroy(RecordedEvent recordedEvent, ThreadDestroyRecordedEvent destroy)
    {
        var pid = recordedEvent.Metadata.Pid;
        var destroyedTid = new ProcessThreadId(pid, destroy.ThreadId);

        foreach (var (lockId, shadow) in _locks)
        {
            var next = shadow.AbandonByDestroy(destroyedTid);
            if (next is null)
                continue;
            
            logger.LogWarning("Thread {Thread} destroyed while still owning lock {Lock}.", destroyedTid, lockId);
            _drainQueue.Enqueue(next.Value);
        }

        _threads.Remove(destroyedTid);
        return inner.ProcessEvent(recordedEvent);
    }

    private RecordedEventState DrainPendingThreads()
    {
        while (_drainQueue.Count > 0)
        {
            var tid = _drainQueue.Dequeue();
            if (!_threads.TryGetValue(tid, out var thread))
                continue;

            thread.BlockedOn = null;
            while (thread.BlockedOn is null && thread.PendingQueue.Count > 0)
            {
                var pendingEvent = thread.PendingQueue.First!.Value;
                thread.PendingQueue.RemoveFirst();
                var state = ProcessOne(pendingEvent);

                if (state == RecordedEventState.Failed)
                    return RecordedEventState.Failed;

                if (state != RecordedEventState.Deferred)
                    continue;
                
                var node = thread.PendingQueue.Last!;
                thread.PendingQueue.RemoveLast();
                thread.PendingQueue.AddFirst(node);
                break;
            }
        }
        return RecordedEventState.Executed;
    }

    private static RecordedEventState Defer(ShadowThread thread, ProcessTrackedObjectId target, RecordedEvent recordedEvent)
    {
        thread.BlockedOn = target;
        thread.PendingQueue.AddLast(recordedEvent);
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
            _threads[tid] = thread = new ShadowThread();
        return thread;
    }

    private ShadowLock GetOrCreateLock(ProcessTrackedObjectId lockId)
    {
        if (!_locks.TryGetValue(lockId, out var shadow))
            _locks[lockId] = shadow = new ShadowLock();
        return shadow;
    }

    private ShadowSemaphore GetOrCreateSemaphore(ProcessTrackedObjectId semaphoreId)
    {
        if (!_semaphores.TryGetValue(semaphoreId, out var shadow))
            _semaphores[semaphoreId] = shadow = new ShadowSemaphore();
        return shadow;
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
