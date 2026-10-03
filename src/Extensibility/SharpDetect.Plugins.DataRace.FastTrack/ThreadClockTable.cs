// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class ThreadClockTable
{
    private readonly ThreadIndexTable _threadIndices = new();
    private readonly Dictionary<ProcessThreadId, VectorClock> _clocks = [];

    public int Count => _clocks.Count;

    public VectorClock CreateClock() => new(_threadIndices);

    public VectorClock GetOrCreate(ProcessThreadId threadId)
    {
        if (!_clocks.TryGetValue(threadId, out var vc))
        {
            vc = new VectorClock(_threadIndices);
            vc.SetClock(threadId, 1);
            _clocks[threadId] = vc;
        }

        return vc;
    }

    public void EnsureExists(ProcessThreadId threadId) => GetOrCreate(threadId);

    // Starts a new epoch, so that work done from now on is not covered by an edge released so far
    public void Advance(ProcessThreadId threadId) => GetOrCreate(threadId).Increment(threadId);

    // Acquire edge between two threads: the joiner absorbs everything the joined thread has done
    public void AcquireFromThread(ProcessThreadId joinerThreadId, ProcessThreadId joinedThreadId)
    {
        var joinerVc = GetOrCreate(joinerThreadId);
        joinerVc.Join(GetOrCreate(joinedThreadId));
        joinerVc.Increment(joinerThreadId);
    }
}
