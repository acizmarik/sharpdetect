// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.Common;

public enum RaceRole
{
    Triggering,
    Conflicting
}

public readonly record struct ThreadAccessEntry(DataRaceInfo Race, AccessInfo Access, RaceRole Role);

public sealed class ThreadAccessCollector
{
    private readonly struct AccessKey(int methodToken, uint methodOffset, AccessType accessType) : IEquatable<AccessKey>
    {
        private readonly int _methodToken = methodToken;
        private readonly uint _methodOffset = methodOffset;
        private readonly AccessType _accessType = accessType;

        public bool Equals(AccessKey other) =>
            _methodToken == other._methodToken &&
            _methodOffset == other._methodOffset &&
            _accessType == other._accessType;

        public override bool Equals(object? obj) => obj is AccessKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_methodToken, _methodOffset, _accessType);
    }

    private readonly Dictionary<ProcessThreadId, (List<ThreadAccessEntry> Entries, HashSet<AccessKey> Seen)> _byThread = [];

    public void AddRace(DataRaceInfo race)
    {
        AddAccess(race.CurrentAccess.ProcessThreadId, race, race.CurrentAccess, RaceRole.Triggering);
        AddAccess(race.LastAccess.ProcessThreadId, race, race.LastAccess, RaceRole.Conflicting);
    }

    public IEnumerable<ProcessThreadId> GetThreads() => _byThread.Keys;

    public IEnumerable<ThreadAccessEntry> GetDistinctAccesses(ProcessThreadId threadId)
        => _byThread[threadId].Entries;

    private void AddAccess(ProcessThreadId threadId, DataRaceInfo race, AccessInfo access, RaceRole role)
    {
        if (!_byThread.TryGetValue(threadId, out var bucket))
        {
            bucket = ([], []);
            _byThread[threadId] = bucket;
        }

        var key = new AccessKey(access.MethodToken.Value, access.MethodOffset, access.AccessType);
        if (bucket.Seen.Add(key))
            bucket.Entries.Add(new ThreadAccessEntry(race, access, role));
    }
}

