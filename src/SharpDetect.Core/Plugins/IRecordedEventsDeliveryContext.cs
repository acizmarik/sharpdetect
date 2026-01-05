// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Core.Plugins
{
    public interface IRecordedEventsDeliveryContext
    {
        void RegisterThreadWaitingForObjectPulse(ProcessThreadId processThreadId, ShadowLock lockObj);
        void UnregisterThreadWaitingForObjectPulse(ProcessThreadId processThreadId, ShadowLock lockObj);
        void BlockEventsDeliveryForThreadWaitingForObject(ProcessThreadId processThreadId, ShadowLock lockObj);
        void BlockEventsDeliveryForThreadWaitingForThreadStart(ProcessThreadId processThreadId, ProcessTrackedObjectId threadObjectId);
        void UnblockEventsDeliveryForThreadWaitingForObject(ShadowLock lockObj);
        void UnblockEventsDeliveryForThreadWaitingForThreadStart(ProcessTrackedObjectId threadObjectId);
        bool IsBlockedEventsDeliveryForThread(ProcessThreadId threadId);
        void EnqueueBlockedEventForThread(ProcessThreadId threadId, RecordedEvent recordedEvent);
        bool SignalOneThreadWaitingForObjectPulse(ShadowLock lockObj);
        bool SignalAllThreadsWaitingForObjectPulse(ShadowLock lockObj);

        bool HasUnblockedThreads();
        bool HasBlockedThreads();
        bool HasAnyUndeliveredEvents();
        bool HasUndeliveredEvents(ProcessThreadId threadId);
        bool IsWaitingForObjectPulse(ProcessThreadId threadId, ShadowLock lockObj);
        IEnumerable<RecordedEvent> ConsumeUndeliveredEvents(ProcessThreadId threadId);
        IEnumerable<ProcessThreadId> ConsumeUnblockedThreads();
    }
}
