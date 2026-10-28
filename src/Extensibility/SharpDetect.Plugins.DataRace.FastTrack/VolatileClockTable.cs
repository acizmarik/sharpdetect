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

        public long CommittedAt;
        public VectorClock? Replaced;
        public long ReplacedAt;
        public Dictionary<ProcessThreadId, PendingRelease>? Pending;
    }

    private readonly record struct PendingRelease(VectorClock Clock, long ReleasedAt);

    public void Acquire(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var location = GetOrCreateLocation(fieldId, objectId);
        var threadVc = threadClocks.GetOrCreateSeen(threadId, out var previouslySeenAt);
        threadVc.Join(location.Committed);

        if (location.Replaced is { } replaced && location.ReplacedAt > previouslySeenAt)
            threadVc.Join(replaced);

        if (location.Pending is { } pending)
        {
            foreach (var release in pending.Values)
                threadVc.Join(release.Clock);
        }
    }

    // An access is released before it executes, but only takes effect once it reports its completion
    public void ReleaseProvisionally(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var threadVc = threadClocks.GetOrCreateSeen(threadId);
        var location = GetOrCreateLocation(fieldId, objectId);
        location.Pending ??= [];
        location.Pending[threadId] = new PendingRelease(threadVc.Clone(), threadClocks.CurrentStamp);
        threadVc.Increment(threadId);
    }

    // A volatile store publishes exactly what the writing thread had done, replacing any earlier writer's clock
    public void CompleteRelease(FieldId fieldId, ProcessTrackedObjectId? objectId, ProcessThreadId threadId)
    {
        var location = GetOrCreateLocation(fieldId, objectId);
        if (location.Pending is { } pending && pending.Remove(threadId, out var release))
            Commit(location, release);

        threadClocks.GetOrCreateSeen(threadId);
    }

    public VectorClock? CompleteProvisionalRelease(
        FieldId fieldId,
        ProcessTrackedObjectId? objectId,
        ProcessThreadId threadId,
        bool stored)
    {
        var location = GetOrCreateLocation(fieldId, objectId);
        if (location.Pending is not { } pending || !pending.Remove(threadId, out var release))
            return null;

        if (stored)
        {
            location.Committed.Join(release.Clock);
            location.CommittedAt = threadClocks.NextStamp();
        }

        return release.Clock;
    }

    public void RemoveTrackedObject(ProcessTrackedObjectId objectId) => _instanceClocks.Remove(objectId);

    private void Commit(LocationClocks location, PendingRelease release)
    {
        var stamp = threadClocks.NextStamp();
        if (location.CommittedAt > release.ReleasedAt)
        {
            // Another store landed while this one was pending, so either of them may have been the last
            location.Committed.Join(release.Clock);
        }
        else
        {
            location.Replaced = location.Committed;
            location.ReplacedAt = stamp;
            location.Committed = release.Clock;
        }

        location.CommittedAt = stamp;
    }

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
