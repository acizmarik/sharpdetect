// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Core.Plugins
{
    public interface IRecordedEventsDeliveryContext
    {
        void BlockEventsDeliveryForThreadWaitingForObject(ProcessThreadId processThreadId, Lock lockObj);
        void BlockEventsDeliveryForThreadWaitingForThreadStart(ProcessThreadId processThreadId, ProcessTrackedObjectId threadObjectId);
        void UnblockEventsDeliveryForThreadWaitingForObject(Lock lockObj);
        void UnblockEventsDeliveryForThreadWaitingForThreadStart(ProcessTrackedObjectId threadObjectId);
        bool IsBlockedEventsDeliveryForThread(ProcessThreadId threadId);
        void EnqueueBlockedEventForThread(ProcessThreadId threadId, RecordedEvent recordedEvent);

        bool HasUnblockedThreads();
        bool HasBlockedThreads();
        bool HasAnyUndeliveredEvents();
        bool HasUndeliveredEvents(ProcessThreadId threadId);
        IEnumerable<RecordedEvent> ConsumeUndeliveredEvents(ProcessThreadId threadId);
        IEnumerable<ProcessThreadId> ConsumeUnblockedThreads();
    }
}
