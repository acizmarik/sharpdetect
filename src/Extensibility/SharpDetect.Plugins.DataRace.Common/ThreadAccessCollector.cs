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
    private readonly struct AccessKey(int methodToken, uint methodOffset, AccessType accessType, int callPathHash) : IEquatable<AccessKey>
    {
        private readonly int _methodToken = methodToken;
        private readonly uint _methodOffset = methodOffset;
        private readonly AccessType _accessType = accessType;
        private readonly int _callPathHash = callPathHash;

        public bool Equals(AccessKey other) =>
            _methodToken == other._methodToken &&
            _methodOffset == other._methodOffset &&
            _accessType == other._accessType &&
            _callPathHash == other._callPathHash;

        public override bool Equals(object? obj) => obj is AccessKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(_methodToken, _methodOffset, _accessType, _callPathHash);
    }

    private static int ComputeCallPathHash(CapturedStackTrace stack)
    {
        var deeperFrames = stack.GetDeeperFrames();
        if (deeperFrames.Count == 0)
            return 0;

        var hash = new HashCode();
        foreach (var frame in deeperFrames)
        {
            hash.Add(frame.ModuleId.Value);
            hash.Add(frame.MethodToken.Value);
        }

        return hash.ToHashCode();
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

        var key = new AccessKey(
            access.Stack.Top.MethodToken.Value,
            access.MethodOffset,
            access.AccessType,
            ComputeCallPathHash(access.Stack));
        if (bucket.Seen.Add(key))
            bucket.Entries.Add(new ThreadAccessEntry(race, access, role));
    }
}

