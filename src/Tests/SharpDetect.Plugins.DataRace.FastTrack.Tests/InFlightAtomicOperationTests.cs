// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class InFlightAtomicOperationTests
{
    private readonly DetectorHarness _harness = new();
    private readonly DetectorHarness _reportingPlainReads = new(new FastTrackPluginConfiguration
    {
        ReportPlainReadsRacingWithAtomicWrites = true
    });

    [Fact]
    public void PlainWriteThatDidNotObserveTheOperation_IsReportedWhenItCompletes()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act
        _harness.AtomicRelease(incrementer, counter, instance: null);
        var writeRace = _harness.Write(plainWriter, counter, instance: null);
        var completionRace = _harness.AtomicAcquire(incrementer, counter, instance: null);

        // Assert - only the completion can tell whether the write happened before the operation
        Assert.Null(writeRace);
        Assert.NotNull(completionRace);
        Assert.Equal(AccessType.AtomicWrite, completionRace.CurrentAccess.AccessType);
        Assert.Equal(plainWriter, completionRace.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.Write, completionRace.LastAccess.AccessType);
    }

    [Fact]
    public void PlainWriteThatTheOperationObserves_IsNotRace()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var publisher = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the operation reads the volatile write that follows the plain write
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.Write(publisher, counter, instance: null);
        _harness.VolatileWrite(publisher, counter, instance: null);
        var race = _harness.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void PlainWriteBeforeTheRelease_IsCheckedEvenWhenAnOrderedWriteReplacedIt()
    {
        // Arrange
        var plainWriter = _harness.NewThread();
        var incrementer = _harness.NewThread();
        var resetter = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the resetter observes the operation, so its own write is ordered after it
        _harness.Write(plainWriter, counter, instance: null);
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.ReadModifyWrite(resetter, counter, instance: null);
        _harness.Write(resetter, counter, instance: null);
        var race = _harness.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void PlainWriteDuringFailedCompareExchange_IsReported()
    {
        // Arrange
        var exchanger = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act - a failed exchange still loads the value
        _harness.CompareExchangeRelease(exchanger, flag, instance: null);
        _harness.Write(plainWriter, flag, instance: null);
        var race = _harness.CompareExchangeCompleted(exchanger, flag, instance: null, succeeded: false);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(AccessType.AtomicRead, race.CurrentAccess.AccessType);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void OperationThatNeverCompleted_DoesNotHideTheNextOne()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the first call threw, the second one runs while the plain write happens
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.Write(plainWriter, counter, instance: null);
        var race = _harness.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void PlainWriteAfterObservingAnInFlightOperation_OnInstanceField_IsNotRace()
    {
        // Arrange
        var first = _harness.NewThread();
        var second = _harness.NewThread();
        var counter = _harness.NewInstanceField("Counter");
        var instance = _harness.NewObject();

        // Act
        _harness.AtomicRelease(first, counter, instance);
        _harness.ReadModifyWrite(second, counter, instance);
        _harness.Write(second, counter, instance);
        var race = _harness.AtomicAcquire(first, counter, instance);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void PlainWriteAfterObservingACompareExchangeThatFails_IsRace()
    {
        // Arrange
        var exchanger = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act - the pending release was visible, but the failed exchange stored nothing the writer could have read
        _harness.CompareExchangeRelease(exchanger, flag, instance: null);
        _harness.VolatileRead(plainWriter, flag, instance: null);
        _harness.Write(plainWriter, flag, instance: null);
        var race = _harness.CompareExchangeCompleted(exchanger, flag, instance: null, succeeded: false);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void LatestPlainWriteOfAThread_DecidesWhetherItRaces()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var publisher = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the operation reads the volatile write, so the first plain write is ordered before it, the second is not
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.Write(publisher, counter, instance: null);
        _harness.VolatileWrite(publisher, counter, instance: null);
        _harness.Write(publisher, counter, instance: null);
        var race = _harness.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(publisher, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void PlainWriteDuringTwoOperations_IsJudgedSeparatelyForEach()
    {
        // Arrange
        var observed = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var unobserved = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the writer observed the first operation only
        _harness.AtomicRelease(observed, counter, instance: null);
        _harness.VolatileRead(plainWriter, counter, instance: null);
        _harness.AtomicRelease(unobserved, counter, instance: null);
        _harness.Write(plainWriter, counter, instance: null);
        var observedRace = _harness.AtomicAcquire(observed, counter, instance: null);
        var unobservedRace = _harness.AtomicAcquire(unobserved, counter, instance: null);

        // Assert
        Assert.Null(observedRace);
        Assert.NotNull(unobservedRace);
        Assert.Equal(plainWriter, unobservedRace.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void OperationThatNeverCompleted_IsReplacedWithTheReleaseOfTheNextOne()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the writer observed the first operation, whose call threw, but not the second one
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.VolatileRead(plainWriter, counter, instance: null);
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.Write(plainWriter, counter, instance: null);
        var race = _harness.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void PlainReadAfterObservingTheOperation_IsNotReportedWhenConfigured()
    {
        // Arrange
        var incrementer = _reportingPlainReads.NewThread();
        var reader = _reportingPlainReads.NewThread();
        var counter = _reportingPlainReads.NewStaticField("Counter");

        // Act
        _reportingPlainReads.AtomicRelease(incrementer, counter, instance: null);
        _reportingPlainReads.VolatileRead(reader, counter, instance: null);
        _reportingPlainReads.Read(reader, counter, instance: null);
        var race = _reportingPlainReads.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void PlainReadThatDidNotObserveTheOperation_IsReportedWhenConfigured()
    {
        // Arrange
        var incrementer = _reportingPlainReads.NewThread();
        var reader = _reportingPlainReads.NewThread();
        var counter = _reportingPlainReads.NewStaticField("Counter");

        // Act
        _reportingPlainReads.AtomicRelease(incrementer, counter, instance: null);
        var readRace = _reportingPlainReads.Read(reader, counter, instance: null);
        var completionRace = _reportingPlainReads.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.Null(readRace);
        Assert.NotNull(completionRace);
        Assert.Equal(reader, completionRace.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.Read, completionRace.LastAccess.AccessType);
    }

    [Fact]
    public void PlainReadBeforeTheRelease_IsReportedWhenConfigured()
    {
        // Arrange
        var reader = _reportingPlainReads.NewThread();
        var incrementer = _reportingPlainReads.NewThread();
        var counter = _reportingPlainReads.NewStaticField("Counter");

        // Act
        _reportingPlainReads.Read(reader, counter, instance: null);
        _reportingPlainReads.AtomicRelease(incrementer, counter, instance: null);
        var race = _reportingPlainReads.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(reader, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void PlainReadDuringFailedCompareExchange_IsNotReportedWhenConfigured()
    {
        // Arrange
        var exchanger = _reportingPlainReads.NewThread();
        var reader = _reportingPlainReads.NewThread();
        var flag = _reportingPlainReads.NewStaticField("Flag");

        // Act - a failed exchange stores nothing
        _reportingPlainReads.CompareExchangeRelease(exchanger, flag, instance: null);
        _reportingPlainReads.Read(reader, flag, instance: null);
        var race = _reportingPlainReads.CompareExchangeCompleted(exchanger, flag, instance: null, succeeded: false);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void PlainReadBeforeTheRelease_IsReportedAfterAnOrderedReadReplacedIt()
    {
        // Arrange
        var reader = _reportingPlainReads.NewThread();
        var incrementer = _reportingPlainReads.NewThread();
        var laterReader = _reportingPlainReads.NewThread();
        var counter = _reportingPlainReads.NewStaticField("Counter");
        var flag = _reportingPlainReads.NewStaticField("Flag");
        var firstRead = _reportingPlainReads.NewStack();

        // Act - the later reader is ordered after the first read and observes the operation
        _reportingPlainReads.Detector.RecordRead(reader, methodOffset: 0, counter, objectId: null, firstRead);
        _reportingPlainReads.VolatileWrite(reader, flag, instance: null);
        _reportingPlainReads.VolatileRead(laterReader, flag, instance: null);
        _reportingPlainReads.AtomicRelease(incrementer, counter, instance: null);
        _reportingPlainReads.VolatileRead(laterReader, counter, instance: null);
        _reportingPlainReads.Read(laterReader, counter, instance: null);
        var race = _reportingPlainReads.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(reader, race.LastAccess.ProcessThreadId);
        Assert.Equal(firstRead, race.LastAccess.Stack);
    }

    [Fact]
    public void PlainReadBeforeTheRelease_IsAttributedToThatReadNotAnOrderedLaterOne()
    {
        // Arrange
        var reader = _reportingPlainReads.NewThread();
        var incrementer = _reportingPlainReads.NewThread();
        var counter = _reportingPlainReads.NewStaticField("Counter");
        var firstRead = _reportingPlainReads.NewStack();

        // Act - the reader's second read observed the operation
        _reportingPlainReads.Detector.RecordRead(reader, methodOffset: 0, counter, objectId: null, firstRead);
        _reportingPlainReads.AtomicRelease(incrementer, counter, instance: null);
        _reportingPlainReads.VolatileRead(reader, counter, instance: null);
        _reportingPlainReads.Read(reader, counter, instance: null);
        var race = _reportingPlainReads.AtomicAcquire(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(firstRead, race.LastAccess.Stack);
    }
}
