// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class VectorClockTests
{
    private readonly ThreadIndexTable _threads = new();

    private static ProcessThreadId Thread(uint id)
        => new(ProcessId: 1, new ThreadId(id));

    [Fact]
    public void GetClock_UnknownThread_ReturnsZero()
    {
        var vc = new VectorClock(_threads);

        Assert.Equal(0, vc.GetClock(Thread(1)));
    }

    [Fact]
    public void GetClock_ThreadKnownToTableButNotToClock_ReturnsZero()
    {
        var other = new VectorClock(_threads);
        other.SetClock(Thread(1), 5);
        var vc = new VectorClock(_threads);

        Assert.Equal(0, vc.GetClock(Thread(1)));
    }

    [Fact]
    public void SetClock_RoundTrips()
    {
        var vc = new VectorClock(_threads);

        vc.SetClock(Thread(1), 42);

        Assert.Equal(42, vc.GetClock(Thread(1)));
    }

    [Fact]
    public void Increment_StartsFromZero()
    {
        var vc = new VectorClock(_threads);

        vc.Increment(Thread(1));
        vc.Increment(Thread(1));

        Assert.Equal(2, vc.GetClock(Thread(1)));
    }

    [Fact]
    public void SetClock_GrowsBeyondInitialCapacity()
    {
        var vc = new VectorClock(_threads);

        for (var i = 0u; i < 64; i++)
            vc.SetClock(Thread(i), (int)i + 1);

        for (var i = 0u; i < 64; i++)
            Assert.Equal((int)i + 1, vc.GetClock(Thread(i)));
    }

    [Fact]
    public void Join_TakesElementWiseMaximum()
    {
        var left = new VectorClock(_threads);
        left.SetClock(Thread(1), 5);
        left.SetClock(Thread(2), 1);
        var right = new VectorClock(_threads);
        right.SetClock(Thread(1), 3);
        right.SetClock(Thread(2), 7);

        left.Join(right);

        Assert.Equal(5, left.GetClock(Thread(1)));
        Assert.Equal(7, left.GetClock(Thread(2)));
    }

    [Fact]
    public void Join_WithLongerClock_AdoptsItsEntries()
    {
        var shorter = new VectorClock(_threads);
        shorter.SetClock(Thread(1), 1);
        var longer = new VectorClock(_threads);
        longer.SetClock(Thread(2), 2);
        longer.SetClock(Thread(3), 3);

        shorter.Join(longer);

        Assert.Equal(1, shorter.GetClock(Thread(1)));
        Assert.Equal(2, shorter.GetClock(Thread(2)));
        Assert.Equal(3, shorter.GetClock(Thread(3)));
    }

    [Fact]
    public void Join_WithShorterClock_KeepsOwnEntries()
    {
        var longer = new VectorClock(_threads);
        longer.SetClock(Thread(1), 1);
        longer.SetClock(Thread(2), 2);
        var shorter = new VectorClock(_threads);
        shorter.SetClock(Thread(1), 9);

        longer.Join(shorter);

        Assert.Equal(9, longer.GetClock(Thread(1)));
        Assert.Equal(2, longer.GetClock(Thread(2)));
    }

    [Fact]
    public void Join_DoesNotAliasTheOtherClock()
    {
        var left = new VectorClock(_threads);
        var right = new VectorClock(_threads);
        right.SetClock(Thread(1), 1);

        left.Join(right);
        left.SetClock(Thread(1), 100);

        Assert.Equal(1, right.GetClock(Thread(1)));
    }

    [Fact]
    public void CopyFrom_ReplacesContents()
    {
        var target = new VectorClock(_threads);
        target.SetClock(Thread(1), 1);
        var source = new VectorClock(_threads);
        source.SetClock(Thread(2), 2);

        target.CopyFrom(source);

        Assert.Equal(2, target.GetClock(Thread(2)));
    }

    [Fact]
    public void CopyFrom_ShorterSource_ClearsTrailingEntries()
    {
        var target = new VectorClock(_threads);
        target.SetClock(Thread(1), 1);
        target.SetClock(Thread(2), 2);
        target.SetClock(Thread(3), 3);
        var source = new VectorClock(_threads);
        source.SetClock(Thread(1), 9);

        target.CopyFrom(source);

        Assert.Equal(9, target.GetClock(Thread(1)));
        Assert.Equal(0, target.GetClock(Thread(2)));
        Assert.Equal(0, target.GetClock(Thread(3)));
    }

    [Fact]
    public void CopyFrom_ShorterSource_ThenRegrowing_DoesNotResurrectClearedEntries()
    {
        var target = new VectorClock(_threads);
        target.SetClock(Thread(1), 1);
        target.SetClock(Thread(2), 42);
        var source = new VectorClock(_threads);
        source.SetClock(Thread(1), 1);

        target.CopyFrom(source);
        target.Increment(Thread(2));

        Assert.Equal(1, target.GetClock(Thread(2)));
    }

    [Fact]
    public void CopyFrom_DoesNotAliasTheSource()
    {
        var target = new VectorClock(_threads);
        var source = new VectorClock(_threads);
        source.SetClock(Thread(1), 1);

        target.CopyFrom(source);
        target.SetClock(Thread(1), 100);

        Assert.Equal(1, source.GetClock(Thread(1)));
    }

    [Fact]
    public void Clone_CopiesEntries()
    {
        var vc = new VectorClock(_threads);
        vc.SetClock(Thread(1), 1);
        vc.SetClock(Thread(2), 2);

        var clone = vc.Clone();

        Assert.Equal(1, clone.GetClock(Thread(1)));
        Assert.Equal(2, clone.GetClock(Thread(2)));
    }

    [Fact]
    public void Clone_DoesNotAliasTheOriginal()
    {
        var vc = new VectorClock(_threads);
        vc.SetClock(Thread(1), 1);

        var clone = vc.Clone();
        clone.SetClock(Thread(1), 100);

        Assert.Equal(1, vc.GetClock(Thread(1)));
    }

    [Fact]
    public void Clone_CanGrowIndependently()
    {
        var vc = new VectorClock(_threads);
        vc.SetClock(Thread(1), 1);

        var clone = vc.Clone();
        clone.SetClock(Thread(2), 2);

        Assert.Equal(2, clone.GetClock(Thread(2)));
        Assert.Equal(0, vc.GetClock(Thread(2)));
    }

    [Fact]
    public void GetEpoch_ZeroClock_IsNone()
    {
        var vc = new VectorClock(_threads);

        Assert.Equal(Epoch.None, vc.GetEpoch(Thread(1)));
    }

    [Fact]
    public void GetEpoch_NonZeroClock_CarriesThreadAndClock()
    {
        var vc = new VectorClock(_threads);
        vc.SetClock(Thread(1), 7);

        Assert.Equal(new Epoch(Thread(1), 7), vc.GetEpoch(Thread(1)));
    }

    [Fact]
    public void FindRacingReader_AllReadsSeenByWriter_ReturnsNull()
    {
        var readers = new VectorClock(_threads);
        readers.SetClock(Thread(1), 2);
        var writer = new VectorClock(_threads);
        writer.SetClock(Thread(1), 2);

        Assert.Null(readers.FindRacingReader(writer));
    }

    [Fact]
    public void FindRacingReader_ReadAheadOfWriter_ReturnsThatReader()
    {
        var readers = new VectorClock(_threads);
        readers.SetClock(Thread(1), 1);
        readers.SetClock(Thread(2), 5);
        var writer = new VectorClock(_threads);
        writer.SetClock(Thread(1), 1);
        writer.SetClock(Thread(2), 4);

        Assert.Equal(Thread(2), readers.FindRacingReader(writer));
    }

    [Fact]
    public void FindRacingReader_ReaderBeyondWriterLength_ReturnsThatReader()
    {
        var readers = new VectorClock(_threads);
        readers.SetClock(Thread(1), 1);
        readers.SetClock(Thread(2), 1);
        var writer = new VectorClock(_threads);
        writer.SetClock(Thread(1), 1);

        Assert.Equal(Thread(2), readers.FindRacingReader(writer));
    }

    [Fact]
    public void FindRacingReader_EmptyReaders_ReturnsNull()
    {
        var readers = new VectorClock(_threads);
        var writer = new VectorClock(_threads);
        writer.SetClock(Thread(1), 1);

        Assert.Null(readers.FindRacingReader(writer));
    }
}
