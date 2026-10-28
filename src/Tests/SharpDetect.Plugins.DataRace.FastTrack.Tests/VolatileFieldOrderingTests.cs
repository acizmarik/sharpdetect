// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class VolatileFieldOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void VolatileWriteThenVolatileRead_OrdersTheGuardedWrite()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void VolatileReadWithoutAPrecedingVolatileWrite_AddsNoOrdering_AndIsReported()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void VolatileClocksAreKeyedPerField()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var publishedFlag = _harness.NewStaticField("PublishedFlag");
        var unrelatedFlag = _harness.NewStaticField("UnrelatedFlag");

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.VolatileWrite(publisher, publishedFlag, instance: null);
        _harness.VolatileRead(consumer, unrelatedFlag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guarded, instance: null));
    }

    [Fact]
    public void VolatileClocksAreKeyedPerInstance()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var sameInstanceConsumer = _harness.NewThread();
        var otherInstanceConsumer = _harness.NewThread();
        var guarded = _harness.NewStaticField("Guarded");
        var flag = _harness.NewInstanceField("Flag");
        var publishedInstance = _harness.NewObject();
        var otherInstance = _harness.NewObject();

        // Act
        _harness.Write(publisher, guarded, instance: null);
        _harness.VolatileWrite(publisher, flag, publishedInstance);

        // Assert
        _harness.VolatileRead(sameInstanceConsumer, flag, publishedInstance);
        Assert.False(_harness.ReadIsRace(sameInstanceConsumer, guarded, instance: null));
        _harness.VolatileRead(otherInstanceConsumer, flag, otherInstance);
        Assert.True(_harness.ReadIsRace(otherInstanceConsumer, guarded, instance: null));
    }
    
    [Fact]
    public void VolatileRead_IsOrderedOnlyAgainstTheWriteItObserved()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var guardedByOverwriter = _harness.NewStaticField("GuardedByOverwriter");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.Write(overwriter, guardedByOverwriter, instance: null);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedByOverwriter, instance: null));
    }

    [Fact]
    public void VolatileRead_WhileTheOverwriteIsPending_IsOrderedAgainstBothWrites()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var guardedByOverwriter = _harness.NewStaticField("GuardedByOverwriter");
        var flag = _harness.NewStaticField("Flag");

        // Act - the overwrite is released, but its store has not landed yet
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.ReadModifyWrite(publisher, flag, instance: null);
        _harness.Write(overwriter, guardedByOverwriter, instance: null);
        _harness.VolatileWriteRelease(overwriter, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedByOverwriter, instance: null));
    }

    [Fact]
    public void VolatileRead_AfterTheOverwriteCompletes_IsOrderedOnlyAgainstTheOverwrite()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.VolatileWriteRelease(overwriter, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.VolatileWriteCompleted(overwriter, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
    }

    [Fact]
    public void VolatileRead_AfterTheOverwriteOfAGenericStaticCompletes_IsOrderedOnlyAgainstTheOverwrite()
    {
        // Arrange
        const nuint instantiation = 0x1000;
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var flag = _harness.NewStaticField(_harness.NewGenericType("Flags`1"), "Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null, instantiation);
        _harness.VolatileWriteRelease(overwriter, flag, instance: null, instantiation);
        _harness.ObserveThread(consumer);
        _harness.VolatileWriteCompleted(overwriter, flag, instance: null, instantiation);
        _harness.ObserveThread(consumer);
        _harness.VolatileRead(consumer, flag, instance: null, instantiation);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
    }

    [Fact]
    public void VolatileRead_OfAThreadNotSeenSinceTheOverwrite_MayHaveLoadedTheOverwrittenValue()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var guardedByOverwriter = _harness.NewStaticField("GuardedByOverwriter");
        var flag = _harness.NewStaticField("Flag");

        // Act - the consumer loads the publisher's value, then the overwrite lands before the load is reported
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.ReadModifyWrite(publisher, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.Write(overwriter, guardedByOverwriter, instance: null);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedByOverwriter, instance: null));
    }

    [Fact]
    public void VolatileRead_OfAThreadNotSeenSinceTwoOverwrites_KeepsOnlyTheLastReplacedValue()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var firstOverwriter = _harness.NewThread();
        var secondOverwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var guardedByFirstOverwriter = _harness.NewStaticField("GuardedByFirstOverwriter");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.Write(firstOverwriter, guardedByFirstOverwriter, instance: null);
        _harness.VolatileWrite(firstOverwriter, flag, instance: null);
        _harness.VolatileWrite(secondOverwriter, flag, instance: null);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert - a load that old would need two complete stores between it and its report
        Assert.True(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedByFirstOverwriter, instance: null));
    }

    [Fact]
    public void VolatileRead_ByTheOverwriter_IsNotOrderedAgainstTheValueItReplaced()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.VolatileRead(overwriter, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(overwriter, guardedByPublisher, instance: null));
    }

    [Fact]
    public void VolatileRead_AfterTheReaderAcquiresALock_IsOrderedOnlyAgainstTheOverwrite()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var flag = _harness.NewStaticField("Flag");

        // Act - the consumer wakes up after the overwrite, so its load cannot return the replaced value
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.Detector.RecordLockAcquired(consumer, lockObject);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
    }

    [Fact]
    public void VolatileRead_OfAThreadStartedAfterTheOverwrite_IsOrderedOnlyAgainstTheOverwrite()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var parent = _harness.NewThread();
        var child = _harness.NewThread();
        var childObject = _harness.NewObject();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.Detector.RecordThreadForkRequested(parent, childObject);
        _harness.Detector.RecordThreadFork(childObject, child);
        _harness.VolatileRead(child, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(child, guardedByPublisher, instance: null));
    }

    [Fact]
    public void VolatileRead_OfAThreadStartedBeforeTheOverwrite_MayHaveLoadedTheOverwrittenValue()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var parent = _harness.NewThread();
        var child = _harness.NewThread();
        var childObject = _harness.NewObject();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.Detector.RecordThreadForkRequested(parent, childObject);
        _harness.Detector.RecordThreadFork(childObject, child);
        _harness.VolatileWrite(overwriter, flag, instance: null);
        _harness.VolatileRead(child, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(child, guardedByPublisher, instance: null));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OverlappingVolatileWrites_AreBothVisible_WhicheverCompletesLast(bool firstCompletesFirst)
    {
        // Arrange
        var first = _harness.NewThread();
        var second = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByFirst = _harness.NewStaticField("GuardedByFirst");
        var guardedBySecond = _harness.NewStaticField("GuardedBySecond");
        var flag = _harness.NewStaticField("Flag");

        // Act - both stores are in flight together, so either of them may have landed last
        _harness.Write(first, guardedByFirst, instance: null);
        _harness.VolatileWriteRelease(first, flag, instance: null);
        _harness.Write(second, guardedBySecond, instance: null);
        _harness.VolatileWriteRelease(second, flag, instance: null);
        _harness.VolatileWriteCompleted(firstCompletesFirst ? first : second, flag, instance: null);
        _harness.VolatileWriteCompleted(firstCompletesFirst ? second : first, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guardedByFirst, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedBySecond, instance: null));
    }

    [Fact]
    public void ReadModifyWriteCompletedWhileVolatileWriteIsPending_IsNotDiscarded()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var overwriter = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByIncrementer = _harness.NewStaticField("GuardedByIncrementer");
        var flag = _harness.NewStaticField("Flag");

        // Act - the read-modify-write may have executed after the store, continuing its release sequence
        _harness.Write(incrementer, guardedByIncrementer, instance: null);
        _harness.VolatileWriteRelease(overwriter, flag, instance: null);
        _harness.ReadModifyWrite(incrementer, flag, instance: null);
        _harness.VolatileWriteCompleted(overwriter, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guardedByIncrementer, instance: null));
    }

    [Fact]
    public void VolatileWriteCompletion_WithoutARelease_IsIgnored()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var stray = _harness.NewThread();
        var consumer = _harness.NewThread();
        var guardedByPublisher = _harness.NewStaticField("GuardedByPublisher");
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(publisher, guardedByPublisher, instance: null);
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.VolatileWriteCompleted(stray, flag, instance: null);
        _harness.ObserveThread(consumer);
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
    }

    [Fact]
    public void VolatileWrite_IsCheckedAgainstPlainAccessesAtItsRelease()
    {
        // Arrange
        var writer = _harness.NewThread();
        var volatileWriter = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(writer, flag, instance: null);
        var raceInfo = _harness.VolatileWriteRelease(volatileWriter, flag, instance: null);
        _harness.VolatileWriteCompleted(volatileWriter, flag, instance: null);

        // Assert
        Assert.NotNull(raceInfo);
    }
}
