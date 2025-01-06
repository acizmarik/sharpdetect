// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;
using SharpDetect.Extensibility.Models;

namespace SharpDetect.Extensibility
{
    public interface IEventsDeliveryContext
    {
        void BlockEventsDeliveryForThreadWaitingForObject(ThreadId threadId, Lock lockObj);
        void UnblockEventsDeliveryForThreadWaitingForObject(Lock lockObj);
        bool IsBlockedEventsDeliveryForThread(ThreadId threadId);
        void EnqueueBlockedEventForThread(ThreadId threadId, RecordedEvent recordedEvent);

        bool HasUnblockedThreads();
        bool HasBlockedThreads();
        IEnumerable<RecordedEvent> ConsumeUndeliveredEvents(ThreadId threadId);
        IEnumerable<ThreadId> ConsumeUnblockedThreads();
    }
}
