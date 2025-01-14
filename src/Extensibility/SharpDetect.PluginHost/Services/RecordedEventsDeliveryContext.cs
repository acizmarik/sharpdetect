// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.PluginHost.Services;

internal class RecordedEventsDeliveryContext : IRecordedEventsDeliveryContext
{
    private readonly Dictionary<ThreadId, Queue<RecordedEvent>> _undelivered;
    private readonly Dictionary<Lock, Queue<ThreadId>> _waitForLockQueue;
    private readonly HashSet<ThreadId> _blockedThreads;
    private readonly HashSet<ThreadId> _unblockedThreads;

    public RecordedEventsDeliveryContext()
    {
        _undelivered = [];
        _waitForLockQueue = [];
        _blockedThreads = [];
        _unblockedThreads = [];
    }

    public void BlockEventsDeliveryForThreadWaitingForObject(ThreadId threadId, Lock lockObj)
    {
        if (!_undelivered.ContainsKey(threadId))
            _undelivered[threadId] = new Queue<RecordedEvent>();
        _blockedThreads.Add(threadId);

        if (!_waitForLockQueue.ContainsKey(lockObj))
            _waitForLockQueue[lockObj] = new Queue<ThreadId>();
        _waitForLockQueue[lockObj].Enqueue(threadId);
    }

    public void UnblockEventsDeliveryForThreadWaitingForObject(Lock lockObj)
    {
        if (_waitForLockQueue.TryGetValue(lockObj, out var waitQueue) && waitQueue.Count > 0)
        {
            var firstThreadId = waitQueue.Dequeue();
            _blockedThreads.Remove(firstThreadId);
            _unblockedThreads.Add(firstThreadId);
        }
    }

    public IEnumerable<RecordedEvent> ConsumeUndeliveredEvents(ThreadId threadId)
    {
        if (!_undelivered.TryGetValue(threadId, out Queue<RecordedEvent>? undelivereds))
            yield break;

        while (undelivereds.Count > 0)
        {
            var item = undelivereds.Dequeue();
            yield return item;
        }
    }

    public bool HasUnblockedThreads()
    {
        return _unblockedThreads.Count > 0;
    }

    public bool HasBlockedThreads()
    {
        return _blockedThreads.Count > 0;
    }

    public IEnumerable<ThreadId> ConsumeUnblockedThreads()
    {
        while (_unblockedThreads.Count > 0)
        {
            var current = _unblockedThreads.First();
            _unblockedThreads.Remove(current);
            yield return current;
        }
    }

    public void EnqueueBlockedEventForThread(ThreadId threadId, RecordedEvent recordedEvent)
    {
        _undelivered[threadId].Enqueue(recordedEvent);
    }

    public bool IsBlockedEventsDeliveryForThread(ThreadId threadId)
    {
        return _blockedThreads.Contains(threadId);
    }
}
