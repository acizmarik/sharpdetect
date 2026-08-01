// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public sealed record RaceGroup(
    RaceKey Key,
    DataRaceInfo Representative,
    int OccurrenceCount,
    ImmutableArray<ProcessTrackedObjectId> ObjectIds,
    bool BothOrderingsObserved)
{
    public AccessInfo Earlier => Representative.LastAccess;
    public AccessInfo Later => Representative.CurrentAccess;
    public FieldId FieldId => Key.FieldId;
}

public static class RaceAggregator
{
    public static IReadOnlyList<RaceGroup> Aggregate(IEnumerable<DataRaceInfo> races)
    {
        var accumulators = new Dictionary<RaceKey, Accumulator>();
        var order = new List<RaceKey>();

        foreach (var race in races)
        {
            var earlier = RaceSite.From(race.LastAccess);
            var later = RaceSite.From(race.CurrentAccess);
            var key = RaceKey.Create(race.FieldId, earlier, later);

            if (!accumulators.TryGetValue(key, out var accumulator))
            {
                accumulator = new Accumulator(race);
                accumulators.Add(key, accumulator);
                order.Add(key);
            }

            accumulator.Add(race, isReversed: earlier.CompareTo(later) > 0);
        }

        return [.. order.Select(key => accumulators[key].ToGroup(key))];
    }

    private sealed class Accumulator
    {
        private readonly DataRaceInfo _representative;
        private readonly HashSet<ProcessTrackedObjectId> _objectIds = [];
        private readonly List<ProcessTrackedObjectId> _orderedObjectIds = [];
        private int _occurrences;
        private bool _sawForward;
        private bool _sawReversed;

        public Accumulator(DataRaceInfo representative)
        {
            _representative = representative;
        }

        public void Add(DataRaceInfo race, bool isReversed)
        {
            _occurrences++;

            if (isReversed)
                _sawReversed = true;
            else
                _sawForward = true;

            if (race.ObjectId is { } objectId && _objectIds.Add(objectId))
                _orderedObjectIds.Add(objectId);
        }

        public RaceGroup ToGroup(RaceKey key) => new(
            Key: key,
            Representative: _representative,
            OccurrenceCount: _occurrences,
            ObjectIds: [.. _orderedObjectIds],
            BothOrderingsObserved: _sawForward && _sawReversed);
    }
}
