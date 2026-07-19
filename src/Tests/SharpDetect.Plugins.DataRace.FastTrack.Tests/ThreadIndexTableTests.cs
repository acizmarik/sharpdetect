// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class ThreadIndexTableTests
{
    private readonly ThreadIndexTable _table = new();

    private static ProcessThreadId Thread(uint id)
        => new(ProcessId: 1, new ThreadId(id));

    [Fact]
    public void GetOrAdd_AssignsConsecutiveIndicesFromZero()
    {
        Assert.Equal(0, _table.GetOrAdd(Thread(10)));
        Assert.Equal(1, _table.GetOrAdd(Thread(20)));
        Assert.Equal(2, _table.GetOrAdd(Thread(30)));
    }

    [Fact]
    public void GetOrAdd_SameThread_ReturnsSameIndex()
    {
        var first = _table.GetOrAdd(Thread(10));
        _table.GetOrAdd(Thread(20));

        Assert.Equal(first, _table.GetOrAdd(Thread(10)));
    }

    [Fact]
    public void GetOrAdd_DistinguishesProcesses()
    {
        var first = _table.GetOrAdd(new ProcessThreadId(1, new ThreadId(10)));
        var second = _table.GetOrAdd(new ProcessThreadId(2, new ThreadId(10)));

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Count_TracksDistinctThreads()
    {
        _table.GetOrAdd(Thread(10));
        _table.GetOrAdd(Thread(20));
        _table.GetOrAdd(Thread(10));

        Assert.Equal(2, _table.Count);
    }

    [Fact]
    public void TryGetIndex_UnknownThread_ReturnsFalse()
    {
        Assert.False(_table.TryGetIndex(Thread(10), out _));
    }

    [Fact]
    public void TryGetIndex_KnownThread_ReturnsItsIndex()
    {
        var index = _table.GetOrAdd(Thread(10));

        Assert.True(_table.TryGetIndex(Thread(10), out var found));
        Assert.Equal(index, found);
    }

    [Fact]
    public void GetThread_ReturnsTheThreadAtAnIndex()
    {
        var index = _table.GetOrAdd(Thread(10));

        Assert.Equal(Thread(10), _table.GetThread(index));
    }

    [Fact]
    public void GetOrAdd_PastInitialCapacity_KeepsIndicesStable()
    {
        var indices = new List<int>();
        for (var i = 0u; i < 100; i++)
            indices.Add(_table.GetOrAdd(Thread(i)));

        for (var i = 0u; i < 100; i++)
        {
            Assert.Equal(indices[(int)i], _table.GetOrAdd(Thread(i)));
            Assert.Equal(Thread(i), _table.GetThread(indices[(int)i]));
        }
    }
}
