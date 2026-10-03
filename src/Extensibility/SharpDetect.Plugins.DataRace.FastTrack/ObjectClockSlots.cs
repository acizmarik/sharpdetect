// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class ObjectClockSlots(ThreadClockTable threadClocks) : ITrackedObjectState
{
    private readonly Dictionary<ProcessTrackedObjectId, VectorClock> _slots = [];

    // Release edge: what the thread has done so far becomes visible to whoever acquires the slot next
    public void Release(ProcessTrackedObjectId slotId, ProcessThreadId threadId)
    {
        var threadVc = threadClocks.GetOrCreate(threadId);
        if (_slots.TryGetValue(slotId, out var slotVc))
            slotVc.Join(threadVc);
        else
            _slots[slotId] = threadVc.Clone();

        threadVc.Increment(threadId);
    }

    // Release edge that replaces the slot, so the handover carries only the releasing thread's work
    public void ReleaseSnapshot(ProcessTrackedObjectId slotId, ProcessThreadId threadId)
    {
        var threadVc = threadClocks.GetOrCreate(threadId);
        _slots[slotId] = threadVc.Clone();
        threadVc.Increment(threadId);
    }

    // Acquire edge against a slot that is simply empty until someone releases into it
    public void Acquire(ProcessTrackedObjectId slotId, ProcessThreadId threadId)
    {
        if (!_slots.TryGetValue(slotId, out var slotVc))
        {
            slotVc = threadClocks.CreateClock();
            _slots[slotId] = slotVc;
        }

        threadClocks.GetOrCreate(threadId).Join(slotVc);
    }

    // Acquire edge that is only established if something was released into the slot
    public bool TryAcquire(ProcessTrackedObjectId slotId, ProcessThreadId threadId)
    {
        if (!_slots.TryGetValue(slotId, out var slotVc))
            return false;

        threadClocks.GetOrCreate(threadId).Join(slotVc);
        return true;
    }

    // Acquire edge that consumes the slot, so a single release cannot be acquired twice
    public bool TryAcquireOnce(ProcessTrackedObjectId slotId, ProcessThreadId threadId)
    {
        if (!_slots.Remove(slotId, out var slotVc))
            return false;

        threadClocks.GetOrCreate(threadId).Join(slotVc);
        return true;
    }

    public void SetEmpty(ProcessTrackedObjectId slotId) => _slots[slotId] = threadClocks.CreateClock();

    public bool Remove(ProcessTrackedObjectId slotId) => _slots.Remove(slotId);

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId) => _slots.Remove(objectId);
}
