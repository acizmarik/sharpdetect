// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class ThreadClockTable
{
    private readonly ThreadIndexTable _threadIndices = new();
    private readonly Dictionary<ProcessThreadId, ThreadClock> _clocks = [];
    private long _stamp;

    private sealed class ThreadClock(VectorClock clock)
    {
        public readonly VectorClock Clock = clock;
        public long SeenAt;
    }

    public int Count => _clocks.Count;

    public long CurrentStamp => _stamp;

    public VectorClock CreateClock() => new(_threadIndices);

    public VectorClock GetOrCreate(ProcessThreadId threadId) => GetOrCreateEntry(threadId).Clock;

    public VectorClock GetOrCreateSeen(ProcessThreadId threadId) => GetOrCreateSeen(threadId, out _);

    public VectorClock GetOrCreateSeen(ProcessThreadId threadId, out long previouslySeenAt)
    {
        var entry = GetOrCreateEntry(threadId);
        previouslySeenAt = entry.SeenAt;
        entry.SeenAt = _stamp;
        return entry.Clock;
    }

    // Stamps order the stores that change what a later load can observe
    public long NextStamp() => ++_stamp;

    public void EnsureExists(ProcessThreadId threadId) => GetOrCreate(threadId);

    // Starts a new epoch, so that work done from now on is not covered by an edge released so far
    public void Advance(ProcessThreadId threadId) => GetOrCreateSeen(threadId).Increment(threadId);

    // Acquire edge between two threads: the joiner absorbs everything the joined thread has done
    public void AcquireFromThread(ProcessThreadId joinerThreadId, ProcessThreadId joinedThreadId)
    {
        var joinerVc = GetOrCreateSeen(joinerThreadId);
        joinerVc.Join(GetOrCreate(joinedThreadId));
        joinerVc.Increment(joinerThreadId);
    }

    private ThreadClock GetOrCreateEntry(ProcessThreadId threadId)
    {
        if (!_clocks.TryGetValue(threadId, out var entry))
        {
            var vc = new VectorClock(_threadIndices);
            vc.SetClock(threadId, 1);
            entry = new ThreadClock(vc);
            _clocks[threadId] = entry;
        }

        return entry;
    }
}
