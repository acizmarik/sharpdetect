// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowThread(ProcessThreadId id)
{
    public const int InitialPendingQueueWarningThreshold = 64;

    public Stack<ProcessTrackedObjectId> SyncTargetStack { get; } = [];
    public int? SuspendedWaitCount { get; set; }
    public int NextPendingQueueWarningCount { get; set; } = InitialPendingQueueWarningThreshold;

    public ProcessThreadId Id { get; } = id;
    public ProcessTrackedObjectId? BlockedOn { get; private set; }
    public long BlockedSinceTimestamp { get; private set; }
    public int PendingQueueCount => _pendingQueue.Count;
    public IEnumerable<RecordedEvent> PendingEvents => _pendingQueue;
    private readonly Queue<RecordedEvent> _pendingQueue = [];

    public void EnqueuePendingEvent(RecordedEvent recordedEvent)
    {
        _pendingQueue.Enqueue(recordedEvent);
    }

    public RecordedEvent PeekPendingEvent()
    {
        return _pendingQueue.Peek();
    }

    public RecordedEvent DequeuePendingEvent()
    {
        return _pendingQueue.Dequeue();
    }

    public void SetBlockedOn(ProcessTrackedObjectId blockedOn)
    {
        BlockedOn = blockedOn;
        BlockedSinceTimestamp = Stopwatch.GetTimestamp();
    }

    public void ClearBlockedOn()
    {
        BlockedOn = null;
    }
}
