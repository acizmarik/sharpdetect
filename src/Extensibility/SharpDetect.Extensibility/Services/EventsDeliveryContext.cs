// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Extensibility
{
    internal class EventsDeliveryContext : IEventsDeliveryContext
    {
        private readonly Dictionary<ThreadId, Queue<RecordedEvent>> _undelivered;
        private readonly HashSet<ThreadId> _blockedThreads;
        private readonly HashSet<ThreadId> _newlyUnblockedThreads;

        public EventsDeliveryContext()
        {
            _undelivered = [];
            _blockedThreads = [];
            _newlyUnblockedThreads = [];
        }
        public void BlockEventsDeliveryForThread(ThreadId threadId)
        {
            if (!_undelivered.ContainsKey(threadId))
                _undelivered[threadId] = new Queue<RecordedEvent>();
            _blockedThreads.Add(threadId);
        }

        public void UnblockEventsDeliveryForThread(ThreadId threadId)
        {
            _blockedThreads.Remove(threadId);
            _newlyUnblockedThreads.Add(threadId);
        }

        public IEnumerable<ThreadId> GetBlockedThreads()
        {
            return _blockedThreads;
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
            return _newlyUnblockedThreads.Count > 0;
        }

        public IEnumerable<ThreadId> ConsumeUnblockedThreads()
        {
            while (_newlyUnblockedThreads.Count > 0)
            {
                var current = _newlyUnblockedThreads.First();
                _newlyUnblockedThreads.Remove(current);
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
}
