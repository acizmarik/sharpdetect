// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class PublicationTracker(ThreadClockTable threadClocks) : ITrackedObjectState
{
    internal const int MaxPendingObservations = 1024;

    private readonly Dictionary<PublicationSlot, VectorClock> _publicationClocks = [];
    private readonly Dictionary<PublicationSlot, PendingObservation> _pendingObservations = [];
    private readonly LinkedList<PublicationSlot> _pendingObservationOrder = new();
    private readonly Dictionary<ProcessTrackedObjectId, HashSet<PublicationSlot>> _slotsByParticipant = [];

    private readonly record struct PublicationSlot(ProcessTrackedObjectId Container, ProcessTrackedObjectId Value);

    private sealed class PendingObservation
    {
        public readonly HashSet<ProcessThreadId> Observers = [];
        public LinkedListNode<PublicationSlot>? OrderNode;
    }

    public int PublicationCount => _publicationClocks.Count;
    public int PendingObservationCount => _pendingObservations.Count;
    public int IndexedParticipantCount => _slotsByParticipant.Count;

    public void RecordPublished(
        ProcessThreadId threadId,
        ProcessTrackedObjectId containerId,
        ProcessTrackedObjectId valueId,
        bool onlyIfAbsent)
    {
        var slot = new PublicationSlot(containerId, valueId);
        var threadVc = threadClocks.GetOrCreate(threadId);
        if (_publicationClocks.TryGetValue(slot, out var publicationVc))
        {
            if (onlyIfAbsent)
                return;

            publicationVc.CopyFrom(threadVc);
        }
        else
        {
            publicationVc = threadVc.Clone();
            _publicationClocks[slot] = publicationVc;
            IndexSlot(slot);
        }

        ReleasePendingObservers(slot, publicationVc, threadId);
        threadVc.Increment(threadId);
    }

    public void RecordObserved(
        ProcessThreadId threadId,
        ProcessTrackedObjectId containerId,
        ProcessTrackedObjectId valueId)
    {
        var slot = new PublicationSlot(containerId, valueId);
        if (_publicationClocks.TryGetValue(slot, out var publicationVc))
        {
            threadClocks.GetOrCreate(threadId).Join(publicationVc);
            return;
        }

        RecordPendingObserver(slot, threadId);
    }

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId)
    {
        if (_publicationClocks.Count == 0 && _pendingObservations.Count == 0)
            return;

        if (!_slotsByParticipant.Remove(objectId, out var slots))
            return;

        foreach (var slot in slots)
        {
            _publicationClocks.Remove(slot);
            RemovePendingObservation(slot);

            var otherParticipant = slot.Container == objectId ? slot.Value : slot.Container;
            if (otherParticipant != objectId)
                UnindexSlot(otherParticipant, slot);
        }
    }

    private void RecordPendingObserver(PublicationSlot slot, ProcessThreadId threadId)
    {
        if (!_pendingObservations.TryGetValue(slot, out var pending))
        {
            pending = new PendingObservation { OrderNode = _pendingObservationOrder.AddLast(slot) };
            _pendingObservations[slot] = pending;
            IndexSlot(slot);
            EvictOldestPendingObservations();
        }

        pending.Observers.Add(threadId);
    }

    private void ReleasePendingObservers(
        PublicationSlot slot,
        VectorClock publicationVc,
        ProcessThreadId publisherThreadId)
    {
        if (RemovePendingObservation(slot) is not { } pending)
            return;

        foreach (var observerThreadId in pending.Observers)
        {
            if (observerThreadId != publisherThreadId)
                threadClocks.GetOrCreate(observerThreadId).Join(publicationVc);
        }
    }

    private PendingObservation? RemovePendingObservation(PublicationSlot slot)
    {
        if (!_pendingObservations.Remove(slot, out var pending))
            return null;

        if (pending.OrderNode is { } node)
        {
            _pendingObservationOrder.Remove(node);
            pending.OrderNode = null;
        }

        return pending;
    }

    private void EvictOldestPendingObservations()
    {
        while (_pendingObservations.Count > MaxPendingObservations &&
               _pendingObservationOrder.First is { } oldest)
        {
            var slot = oldest.Value;
            if (RemovePendingObservation(slot) is null)
            {
                _pendingObservationOrder.Remove(oldest);
                continue;
            }

            UnindexSlot(slot.Container, slot);
            if (slot.Container != slot.Value)
                UnindexSlot(slot.Value, slot);
        }
    }

    private void IndexSlot(PublicationSlot slot)
    {
        IndexSlot(slot.Container, slot);
        if (slot.Container != slot.Value)
            IndexSlot(slot.Value, slot);
    }

    private void IndexSlot(ProcessTrackedObjectId participant, PublicationSlot slot)
    {
        if (!_slotsByParticipant.TryGetValue(participant, out var slots))
        {
            slots = [];
            _slotsByParticipant[participant] = slots;
        }

        slots.Add(slot);
    }

    private void UnindexSlot(ProcessTrackedObjectId participant, PublicationSlot slot)
    {
        if (!_slotsByParticipant.TryGetValue(participant, out var slots))
            return;

        slots.Remove(slot);
        if (slots.Count == 0)
            _slotsByParticipant.Remove(participant);
    }
}
