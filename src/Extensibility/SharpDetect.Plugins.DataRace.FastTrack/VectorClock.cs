// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class VectorClock
{
    private readonly Dictionary<ProcessThreadId, int> _clocks = [];

    public VectorClock()
    {
    }

    private VectorClock(Dictionary<ProcessThreadId, int> clocks)
    {
        _clocks = new Dictionary<ProcessThreadId, int>(clocks);
    }
    
    public int GetClock(ProcessThreadId threadId)
    {
        return _clocks.GetValueOrDefault(threadId, 0);
    }
    
    public void SetClock(ProcessThreadId threadId, int value)
    {
        _clocks[threadId] = value;
    }
    
    public void Increment(ProcessThreadId threadId)
    {
        _clocks[threadId] = GetClock(threadId) + 1;
    }
    
    public void Join(VectorClock other)
    {
        foreach (var (threadId, otherClock) in other._clocks)
        {
            var myClock = GetClock(threadId);
            if (otherClock > myClock)
                _clocks[threadId] = otherClock;
        }
    }
    
    public ProcessThreadId? FindRacingReader(VectorClock writerVc)
    {
        foreach (var (threadId, clock) in _clocks)
        {
            if (clock > writerVc.GetClock(threadId))
                return threadId;
        }

        return null;
    }

    public Epoch GetEpoch(ProcessThreadId threadId)
    {
        var clock = GetClock(threadId);
        return clock == 0 ? Epoch.None : new Epoch(threadId, clock);
    }
    
    public VectorClock Clone()
    {
        return new VectorClock(_clocks);
    }
}

