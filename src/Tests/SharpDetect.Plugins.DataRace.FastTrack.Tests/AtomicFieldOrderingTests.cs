// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class AtomicFieldOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void ReadModifyWriteReleaseThenAcquire_OrdersTheGuardedWrite()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.AtomicRelease(publisher, flag, instance: null);
        _harness.AtomicAcquire(publisher, flag, instance: null);
        _harness.AtomicAcquire(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void ReadModifyWriteRelease_DoesNotCoverWritesAfterIt()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.AtomicRelease(publisher, flag, instance: null);
        _harness.Write(publisher, guarded, instance: null);
        _harness.AtomicAcquire(publisher, flag, instance: null);
        _harness.AtomicAcquire(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void ReadModifyWriteChain_KeepsEveryWriterInTheReleaseSequence()
    {
        // Arrange
        var first = _harness.NewThread();
        var second = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByFirst = _harness.NewStaticField("GuardedByFirst");
        var guardedBySecond = _harness.NewStaticField("GuardedBySecond");
        var counter = _harness.NewStaticField("Counter");

        // Act - both releases are recorded before either operation reports its acquire
        _harness.Write(first, guardedByFirst, instance: null);
        _harness.Write(second, guardedBySecond, instance: null);
        _harness.AtomicRelease(first, counter, instance: null);
        _harness.AtomicRelease(second, counter, instance: null);
        _harness.AtomicAcquire(first, counter, instance: null);
        _harness.AtomicAcquire(second, counter, instance: null);
        _harness.AtomicAcquire(consumer, counter, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guardedByFirst, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedBySecond, instance: null));
    }

    [Fact]
    public void VolatileWriteAfterReadModifyWriteChain_EndsTheReleaseSequence()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByIncrementer = _harness.NewStaticField("GuardedByIncrementer");
        var guardedByOverwriter = _harness.NewStaticField("GuardedByOverwriter");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(incrementer, guardedByIncrementer, instance: null);
        _harness.AtomicRelease(incrementer, flag, instance: null);
        _harness.AtomicAcquire(incrementer, flag, instance: null);
        _harness.Write(overwriter, guardedByOverwriter, instance: null);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guardedByIncrementer, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedByOverwriter, instance: null));
    }

    [Fact]
    public void VolatileWriteRecordedWhileReadModifyWriteIsInFlight_DoesNotDiscardIt()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByIncrementer = _harness.NewStaticField("GuardedByIncrementer");
        var flag = _harness.NewStaticField("Flag");

        // Act - the overwriter's store lands before the read-modify-write executes, so the consumer reads the latter
        _harness.Write(incrementer, guardedByIncrementer, instance: null);
        _harness.AtomicRelease(incrementer, flag, instance: null);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.AtomicAcquire(incrementer, flag, instance: null);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guardedByIncrementer, instance: null));
    }

    [Fact]
    public void SucceededCompareExchange_Releases()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.CompareExchangeRelease(publisher, flag, instance: null);
        _harness.CompareExchangeCompleted(publisher, flag, instance: null, succeeded: true);
        _harness.AtomicAcquire(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void FailedCompareExchange_DoesNotRelease()
    {
        // Arrange
        var writer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(writer, guarded, instance: null);
        _harness.CompareExchangeRelease(writer, flag, instance: null);
        _harness.CompareExchangeCompleted(writer, flag, instance: null, succeeded: false);
        _harness.AtomicAcquire(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void FailedCompareExchange_StillAcquires()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.AtomicRelease(publisher, flag, instance: null);
        _harness.AtomicAcquire(publisher, flag, instance: null);
        _harness.CompareExchangeRelease(consumer, flag, instance: null);
        _harness.CompareExchangeCompleted(consumer, flag, instance: null, succeeded: false);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void PendingCompareExchange_IsVisibleUntilItFails()
    {
        // Arrange
        var writer = _harness.NewThread();
        var earlyConsumer = _harness.NewThread();
        var lateConsumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(writer, guarded, instance: null);
        _harness.CompareExchangeRelease(writer, flag, instance: null);
        _harness.AtomicAcquire(earlyConsumer, flag, instance: null);
        _harness.CompareExchangeCompleted(writer, flag, instance: null, succeeded: false);
        _harness.AtomicAcquire(lateConsumer, flag, instance: null);

        // Assert - the outcome is unknown while the exchange is pending, so the early acquire may have observed it
        Assert.False(_harness.ReadIsRace(earlyConsumer, guarded, instance: null));
        Assert.True(_harness.ReadIsRace(lateConsumer, guarded, instance: null));
    }

    [Fact]
    public void UncompletedCompareExchange_IsReplacedByTheThreadsNextOne()
    {
        // Arrange
        var writer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act - the first exchange never reports its outcome (for example because the call threw)
        _harness.Write(writer, guarded, instance: null);
        _harness.CompareExchangeRelease(writer, flag, instance: null);
        _harness.CompareExchangeRelease(writer, flag, instance: null);
        _harness.CompareExchangeCompleted(writer, flag, instance: null, succeeded: false);
        _harness.AtomicAcquire(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void CompareExchangeClocks_AreKeyedPerInstance()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewInstanceField("Flag");
        var publishedInstance = _harness.NewObject();
        var otherInstance = _harness.NewObject();

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.CompareExchangeRelease(publisher, flag, publishedInstance);
        _harness.AtomicAcquire(consumer, flag, otherInstance);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guarded, instance: null));
    }
}
