// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.DataRace.FastTrack;

internal sealed class VectorClock
{
    private readonly ThreadIndexTable _threads;
    private int[] _clocks;
    private int _length;

    public VectorClock(ThreadIndexTable threads)
    {
        _threads = threads;
        _clocks = [];
    }

    private VectorClock(ThreadIndexTable threads, int[] clocks, int length)
    {
        _threads = threads;
        _clocks = clocks;
        _length = length;
    }
    
    public int GetClock(ProcessThreadId threadId)
    {
        if (!_threads.TryGetIndex(threadId, out var index) || index >= _length)
            return 0;

        return _clocks[index];
    }
    
    public void SetClock(ProcessThreadId threadId, int value)
    {
        var index = _threads.GetOrAdd(threadId);
        EnsureLength(index + 1);
        _clocks[index] = value;
    }
    
    public void Increment(ProcessThreadId threadId)
    {
        var index = _threads.GetOrAdd(threadId);
        EnsureLength(index + 1);
        _clocks[index]++;
    }
    
    public void Join(VectorClock other)
    {
        var otherLength = other._length;
        if (otherLength > _length)
            EnsureLength(otherLength);

        var otherClocks = other._clocks;
        for (var index = 0; index < otherLength; index++)
        {
            if (otherClocks[index] > _clocks[index])
                _clocks[index] = otherClocks[index];
        }
    }
    
    public ProcessThreadId? FindRacingReader(VectorClock writerVc)
    {
        var writerClocks = writerVc._clocks;
        var writerLength = writerVc._length;
        for (var index = 0; index < _length; index++)
        {
            var writerClock = index < writerLength ? writerClocks[index] : 0;
            if (_clocks[index] > writerClock)
                return _threads.GetThread(index);
        }

        return null;
    }

    public Epoch GetEpoch(ProcessThreadId threadId)
    {
        var clock = GetClock(threadId);
        return clock == 0 ? Epoch.None : new Epoch(threadId, clock);
    }
    
    public void CopyFrom(VectorClock other)
    {
        var otherLength = other._length;
        if (_clocks.Length < otherLength)
            _clocks = new int[otherLength];

        Array.Copy(other._clocks, _clocks, otherLength);
        if (_length > otherLength)
            Array.Clear(_clocks, otherLength, _length - otherLength);

        _length = otherLength;
    }

    public VectorClock Clone()
    {
        var copy = new int[_length];
        Array.Copy(_clocks, copy, _length);
        return new VectorClock(_threads, copy, _length);
    }

    private void EnsureLength(int length)
    {
        if (length <= _length)
            return;

        if (_clocks.Length < length)
            Array.Resize(ref _clocks, Math.Max(length, _clocks.Length * 2));

        _length = length;
    }
}
