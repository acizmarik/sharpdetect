// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class VolatileClockTable(ThreadClockTable threadClocks) : ITrackedObjectState
{
    private readonly Dictionary<FieldId, LocationClocks> _staticClocks = [];
    private readonly Dictionary<ProcessTrackedObjectId, Dictionary<FieldId, LocationClocks>> _instanceClocks = [];

    private sealed class LocationClocks
    {
        public VectorClock Committed;

        public LocationClocks(VectorClock committed)
        {
            Committed = committed;
        }

        public Dictionary<ProcessThreadId, VectorClock>? Pending;
    }

    public void Acquire(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var location = GetOrCreateLocation(fieldId, objectId);
        var threadVc = threadClocks.GetOrCreate(threadId);
        threadVc.Join(location.Committed);

        if (location.Pending is { } pending)
        {
            foreach (var pendingVc in pending.Values)
                threadVc.Join(pendingVc);
        }
    }

    // A volatile write publishes exactly what the writing thread has done, replacing any earlier writer's clock
    public void Release(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var threadVc = threadClocks.GetOrCreate(threadId);
        GetOrCreateLocation(fieldId, objectId).Committed = threadVc.Clone();
        threadVc.Increment(threadId);
    }

    // An atomic operation is released before it executes, but only takes effect once it reports its completion
    public void ReleaseProvisionally(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var threadVc = threadClocks.GetOrCreate(threadId);
        var location = GetOrCreateLocation(fieldId, objectId);
        location.Pending ??= [];
        location.Pending[threadId] = threadVc.Clone();
        threadVc.Increment(threadId);
    }

    // A stored value continues the release sequence, so it adds to the clocks of earlier writers
    public VectorClock? CompleteProvisionalRelease(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        bool stored)
    {
        var location = GetOrCreateLocation(fieldId, objectId);
        if (location.Pending is not { } pending || !pending.Remove(threadId, out var pendingVc))
            return null;

        if (stored)
            location.Committed.Join(pendingVc);

        return pendingVc;
    }

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId) => _instanceClocks.Remove(objectId);

    private LocationClocks GetOrCreateLocation(FieldId fieldId, ProcessTrackedObjectId? objectId)
    {
        var clocks = GetClockMap(objectId);
        if (!clocks.TryGetValue(fieldId, out var location))
        {
            location = new LocationClocks(threadClocks.CreateClock());
            clocks[fieldId] = location;
        }

        return location;
    }

    private Dictionary<FieldId, LocationClocks> GetClockMap(ProcessTrackedObjectId? objectId)
    {
        if (objectId is not { } objId)
            return _staticClocks;

        if (!_instanceClocks.TryGetValue(objId, out var clocks))
        {
            clocks = [];
            _instanceClocks[objId] = clocks;
        }

        return clocks;
    }
}
