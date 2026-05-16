// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies.Shadows;

internal sealed class ShadowThread(ProcessThreadId id, ILogger logger)
{
    public Stack<ProcessTrackedObjectId> SyncTargetStack { get; } = [];
    public int? SuspendedWaitCount { get; set; }

    public ProcessThreadId Id { get; } = id;
    public ProcessTrackedObjectId? BlockedOn { get; private set; }
    public int PendingQueueCount => _pendingQueue.Count;
    private readonly Queue<RecordedEvent> _pendingQueue = [];

    public void EnqueuePendingEvent(RecordedEvent recordedEvent)
    {
        _pendingQueue.Enqueue(recordedEvent);
        if (_pendingQueue.Count % 64 == 0)
            logger.LogWarning("Thread {tid}'s queue {size} is getting too large", Id, _pendingQueue.Count);
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
    }

    public void ClearBlockedOn()
    {
        BlockedOn = null;
    }
}
