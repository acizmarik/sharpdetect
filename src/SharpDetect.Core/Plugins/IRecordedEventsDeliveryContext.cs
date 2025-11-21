// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Core.Plugins
{
    public interface IRecordedEventsDeliveryContext
    {
        void BlockEventsDeliveryForThreadWaitingForObject(ThreadId threadId, Lock lockObj);
        void BlockEventsDeliveryForThreadWaitingForThreadStart(ThreadId threadId, TrackedObjectId threadObjectId);
        void UnblockEventsDeliveryForThreadWaitingForObject(Lock lockObj);
        void UnblockEventsDeliveryForThreadWaitingForThreadStart(TrackedObjectId threadObjectId);
        bool IsBlockedEventsDeliveryForThread(ThreadId threadId);
        void EnqueueBlockedEventForThread(ThreadId threadId, RecordedEvent recordedEvent);

        bool HasUnblockedThreads();
        bool HasBlockedThreads();
        IEnumerable<RecordedEvent> ConsumeUndeliveredEvents(ThreadId threadId);
        IEnumerable<ThreadId> ConsumeUnblockedThreads();
    }
}
