// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.PluginHost.Services;

internal class RecordedEventsDeliveryContext : IRecordedEventsDeliveryContext
{
    private readonly Dictionary<ProcessThreadId, Queue<RecordedEvent>> _undelivered;
    private readonly Dictionary<ShadowLock, Queue<ProcessThreadId>> _waitForLockQueue;
    private readonly Dictionary<ShadowLock, List<ProcessThreadId>> _waitForSignalQueue;
    private readonly Dictionary<ProcessTrackedObjectId, Queue<ProcessThreadId>> _waitForThreadStartQueue;
    private readonly HashSet<ProcessThreadId> _blockedThreads;
    private readonly HashSet<ProcessThreadId> _unblockedThreads;

    public RecordedEventsDeliveryContext()
    {
        _undelivered = [];
        _waitForSignalQueue = [];
        _waitForLockQueue = [];
        _waitForThreadStartQueue = [];
        _blockedThreads = [];
        _unblockedThreads = [];
    }

    public void RegisterThreadWaitingForObjectPulse(ProcessThreadId processThreadId, ShadowLock lockObj)
    {
        if (!_waitForSignalQueue.ContainsKey(lockObj))
            _waitForSignalQueue[lockObj] = new List<ProcessThreadId>();
        _waitForSignalQueue[lockObj].Add(processThreadId);
    }

    public void UnregisterThreadWaitingForObjectPulse(ProcessThreadId processThreadId, ShadowLock lockObj)
    {
        if (!_waitForSignalQueue.TryGetValue(lockObj, out var queue))
            return;

        queue.Remove(processThreadId);
    }

    public void BlockEventsDeliveryForThreadWaitingForObject(ProcessThreadId processThreadId, ShadowLock lockObj)
    {
        if (!_undelivered.ContainsKey(processThreadId))
            _undelivered[processThreadId] = new Queue<RecordedEvent>();
        _blockedThreads.Add(processThreadId);

        if (!_waitForLockQueue.ContainsKey(lockObj))
            _waitForLockQueue[lockObj] = new Queue<ProcessThreadId>();
        _waitForLockQueue[lockObj].Enqueue(processThreadId);
    }

    public void BlockEventsDeliveryForThreadWaitingForThreadStart(ProcessThreadId processThreadId, ProcessTrackedObjectId threadObjectId)
    {
        if (!_undelivered.ContainsKey(processThreadId))
            _undelivered[processThreadId] = new Queue<RecordedEvent>();
        _blockedThreads.Add(processThreadId);
        
        if (!_waitForThreadStartQueue.ContainsKey(threadObjectId))
            _waitForThreadStartQueue[threadObjectId] = new Queue<ProcessThreadId>();
        _waitForThreadStartQueue[threadObjectId].Enqueue(processThreadId);
    }

    public bool SignalOneThreadWaitingForObjectPulse(ShadowLock lockObj)
    {
        if (_waitForSignalQueue.TryGetValue(lockObj, out var waitQueue) && waitQueue.Count > 0)
        {
            waitQueue.RemoveAt(0);
            return true;
        }
        
        return false;
    }

    public bool SignalAllThreadsWaitingForObjectPulse(ShadowLock lockObj)
    {
        if (_waitForSignalQueue.TryGetValue(lockObj, out var waitQueue))
        {
            waitQueue.Clear();
            return true;
        }

        return false;
    }

    public void UnblockEventsDeliveryForThreadWaitingForObject(ShadowLock lockObj)
    {
        if (_waitForLockQueue.TryGetValue(lockObj, out var waitQueue) && waitQueue.Count > 0)
        {
            var firstThreadId = waitQueue.Dequeue();
            _blockedThreads.Remove(firstThreadId);
            _unblockedThreads.Add(firstThreadId);
        }
    }

    public void UnblockEventsDeliveryForThreadWaitingForThreadStart(ProcessTrackedObjectId threadObjectId)
    {
        if (_waitForThreadStartQueue.TryGetValue(threadObjectId, out var waitQueue))
        {
            while (waitQueue.Count > 0)
            {
                var currentThreadId = waitQueue.Dequeue();
                _blockedThreads.Remove(currentThreadId);
                _unblockedThreads.Add(currentThreadId);
            }
        }
    }

    public bool HasAnyUndeliveredEvents()
    {
        return _undelivered.Any(kv => kv.Value.Count > 0);
    }

    public bool HasUndeliveredEvents(ProcessThreadId threadId)
    {
        return _undelivered.ContainsKey(threadId) && _undelivered[threadId].Count > 0;
    }

    public bool IsWaitingForObjectPulse(ProcessThreadId threadId, ShadowLock lockObj)
    {
        return _waitForSignalQueue.TryGetValue(lockObj, out var queue) && queue.Contains(threadId);
    }

    public IEnumerable<RecordedEvent> ConsumeUndeliveredEvents(ProcessThreadId processThreadId)
    {
        if (!_undelivered.TryGetValue(processThreadId, out Queue<RecordedEvent>? undelivereds))
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

    public IEnumerable<ProcessThreadId> ConsumeUnblockedThreads()
    {
        while (_unblockedThreads.Count > 0)
        {
            var current = _unblockedThreads.First();
            _unblockedThreads.Remove(current);
            yield return current;
        }
    }

    public void EnqueueBlockedEventForThread(ProcessThreadId processThreadId, RecordedEvent recordedEvent)
    {
        _undelivered[processThreadId].Enqueue(recordedEvent);
    }

    public bool IsBlockedEventsDeliveryForThread(ProcessThreadId processThreadId)
    {
        return _blockedThreads.Contains(processThreadId) || _unblockedThreads.Contains(processThreadId);
    }
}
