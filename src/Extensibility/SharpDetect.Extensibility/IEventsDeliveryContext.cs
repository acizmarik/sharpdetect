// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Extensibility
{
    public interface IEventsDeliveryContext
    {
        void BlockEventsDeliveryForThread(ThreadId threadId);
        void UnblockEventsDeliveryForThread(ThreadId threadId);
        bool IsBlockedEventsDeliveryForThread(ThreadId threadId);
        void EnqueueBlockedEventForThread(ThreadId threadId, RecordedEvent recordedEvent);

        bool HasUnblockedThreads();
        IEnumerable<RecordedEvent> ConsumeUndeliveredEvents(ThreadId threadId);
        IEnumerable<ThreadId> ConsumeUnblockedThreads();
        IEnumerable<ThreadId> GetBlockedThreads();
    }
}
