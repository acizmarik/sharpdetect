// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class SemaphoreOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void ReleaseAfterWrite_OrdersTheNextAcquirer()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreAcquired(successor, semaphore);

        // Assert
        Assert.False(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void InitialPermitsCarryNoHappensBefore_AndAreReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 1);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreAcquired(successor, semaphore);

        // Assert
        Assert.True(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void ReleaseWithCountTwo_OrdersTwoAcquirers()
    {
        // Arrange
        var writer = _harness.NewThread();
        var firstAcquirer = _harness.NewThread();
        var secondAcquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 2);

        // Assert
        _harness.Detector.RecordSemaphoreAcquired(firstAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(firstAcquirer, field, instance: null));
        _harness.Detector.RecordSemaphoreAcquired(secondAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(secondAcquirer, field, instance: null));
    }

    [Fact]
    public void MorePermitsConsumedThanReleased_IsReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var firstAcquirer = _harness.NewThread();
        var secondAcquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);

        // Assert
        _harness.Detector.RecordSemaphoreAcquired(firstAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(firstAcquirer, field, instance: null));
        _harness.Detector.RecordSemaphoreAcquired(secondAcquirer, semaphore);
        Assert.True(_harness.ReadIsRace(secondAcquirer, field, instance: null));
    }

    [Fact]
    public void AcquireOnUnseenSemaphore_AddsNoOrdering_AndIsReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreAcquired(successor, semaphore);

        // Assert
        Assert.True(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void MaximalInitialCount_IsTracked_AndInitialPermitsAreReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: int.MaxValue);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreAcquired(successor, semaphore);

        // Assert
        Assert.True(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void InitialPermitsAreConsumedBeforeReleasedPermits()
    {
        // Arrange
        var writer = _harness.NewThread();
        var firstAcquirer = _harness.NewThread();
        var secondAcquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 1);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);

        // Assert
        _harness.Detector.RecordSemaphoreAcquired(firstAcquirer, semaphore);
        Assert.True(_harness.ReadIsRace(firstAcquirer, field, instance: null));
        _harness.Detector.RecordSemaphoreAcquired(secondAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(secondAcquirer, field, instance: null));
    }

    [Fact]
    public void RetractedRelease_DoesNotOrderTheNextAcquirer()
    {
        // Arrange
        var releaser = _harness.NewThread();
        var acquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Write(releaser, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreReleaseRetracted(releaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreAcquired(acquirer, semaphore);

        // Assert
        Assert.True(_harness.ReadIsRace(acquirer, field, instance: null));
    }

    [Fact]
    public void RetractedRelease_DoesNotShiftLaterPermitsToTheWrongAcquirer()
    {
        // Arrange
        var phantomReleaser = _harness.NewThread();
        var releaser = _harness.NewThread();
        var acquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Detector.RecordSemaphoreReleased(phantomReleaser, semaphore, releaseCount: 1);
        _harness.Write(releaser, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreReleaseRetracted(phantomReleaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreAcquired(acquirer, semaphore);

        // Assert
        Assert.False(_harness.ReadIsRace(acquirer, field, instance: null));
    }

    [Fact]
    public void RetractingAnAlreadyAcquiredRelease_KeepsTheEdgeItGave()
    {
        // Arrange
        var releaser = _harness.NewThread();
        var acquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Write(releaser, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreAcquired(acquirer, semaphore);
        _harness.Detector.RecordSemaphoreReleaseRetracted(releaser, semaphore, releaseCount: 1);

        // Assert
        Assert.False(_harness.ReadIsRace(acquirer, field, instance: null));
    }

    [Fact]
    public void ReleasesWithoutAcquires_KeepABoundedNumberOfClocks()
    {
        // Arrange
        var releaser = _harness.NewThread();
        var semaphore = _harness.NewObject();

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        for (var i = 0; i < 100_000; i++)
            _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);

        // Assert
        Assert.Equal(SemaphorePermitPools.MaxReleasedPermitClocks + 1, _harness.Detector.GetReleasedSemaphorePermitClockCount(semaphore));
    }

    [Fact]
    public void ReleasesOnUnseenSemaphore_KeepABoundedNumberOfClocks()
    {
        // Arrange
        var releaser = _harness.NewThread();
        var semaphore = _harness.NewObject();

        // Act
        for (var i = 0; i < 100_000; i++)
            _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);

        // Assert
        Assert.Equal(SemaphorePermitPools.MaxReleasedPermitClocks + 1, _harness.Detector.GetReleasedSemaphorePermitClockCount(semaphore));
    }

    [Fact]
    public void Releases_KeepNoMoreClocksThanTheSemaphoreHoldsPermits()
    {
        // Arrange
        var releaser = _harness.NewThread();
        var semaphore = _harness.NewObject();

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0, maximumCount: 2);
        for (var i = 0; i < 5; i++)
            _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);

        // Assert
        Assert.Equal(3, _harness.Detector.GetReleasedSemaphorePermitClockCount(semaphore));
    }

    [Fact]
    public void ReleaseOfVeryManyPermits_KeepsABoundedNumberOfClocks()
    {
        // Arrange
        var releaser = _harness.NewThread();
        var semaphore = _harness.NewObject();

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: int.MaxValue);

        // Assert
        Assert.Equal(SemaphorePermitPools.MaxReleasedPermitClocks + 1, _harness.Detector.GetReleasedSemaphorePermitClockCount(semaphore));
    }

    [Fact]
    public void ReleaseOfMorePermitsThanClocksAreKept_OrdersEveryAcquirer()
    {
        // Arrange
        const int permits = SemaphorePermitPools.MaxReleasedPermitClocks + 76;
        var writer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: permits);

        // Assert
        for (var i = 0; i < permits; i++)
        {
            var acquirer = _harness.NewThread();
            _harness.Detector.RecordSemaphoreAcquired(acquirer, semaphore);
            Assert.False(_harness.ReadIsRace(acquirer, field, instance: null));
        }

        var lateAcquirer = _harness.NewThread();
        _harness.Detector.RecordSemaphoreAcquired(lateAcquirer, semaphore);
        Assert.True(_harness.ReadIsRace(lateAcquirer, field, instance: null));
    }

    [Fact]
    public void MoreSingleReleasesThanClocksAreKept_OrderEveryAcquirerAfterItsOwnPermit()
    {
        // Arrange
        const int permits = SemaphorePermitPools.MaxReleasedPermitClocks + 1;
        var releaser = _harness.NewThread();
        var writer = _harness.NewThread();
        var firstAcquirer = _harness.NewThread();
        var secondAcquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);
        for (var i = 2; i < permits; i++)
            _harness.Detector.RecordSemaphoreReleased(releaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreAcquired(firstAcquirer, semaphore);
        _harness.Detector.RecordSemaphoreAcquired(secondAcquirer, semaphore);

        // Assert
        Assert.False(_harness.ReadIsRace(secondAcquirer, field, instance: null));
    }

    [Fact]
    public void RetractedReleaseOnAFullSemaphore_KeepsThePermitThatWasThere()
    {
        // Arrange
        var writer = _harness.NewThread();
        var phantomReleaser = _harness.NewThread();
        var acquirer = _harness.NewThread();
        var lateAcquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");
        var phantomField = _harness.NewStaticField("WrittenByPhantomReleaser");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0, maximumCount: 1);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);
        _harness.Write(phantomReleaser, phantomField, instance: null);
        _harness.Detector.RecordSemaphoreReleased(phantomReleaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreReleaseRetracted(phantomReleaser, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreAcquired(acquirer, semaphore);
        _harness.Detector.RecordSemaphoreAcquired(lateAcquirer, semaphore);

        // Assert
        Assert.False(_harness.ReadIsRace(acquirer, field, instance: null));
        Assert.True(_harness.ReadIsRace(acquirer, phantomField, instance: null));
        Assert.True(_harness.ReadIsRace(lateAcquirer, phantomField, instance: null));
    }

    [Fact]
    public void RetractedReleaseOnAFullSemaphore_KeepsEveryEarlierPermitAndItsOrdering()
    {
        // Arrange
        var firstWriter = _harness.NewThread();
        var secondWriter = _harness.NewThread();
        var phantomReleaser = _harness.NewThread();
        var firstAcquirer = _harness.NewThread();
        var secondAcquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var firstField = _harness.NewStaticField("First");
        var secondField = _harness.NewStaticField("Second");
        var phantomField = _harness.NewStaticField("WrittenByPhantomReleaser");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0, maximumCount: 2);
        _harness.Write(firstWriter, firstField, instance: null);
        _harness.Detector.RecordSemaphoreReleased(firstWriter, semaphore, releaseCount: 1);
        _harness.Write(secondWriter, secondField, instance: null);
        _harness.Detector.RecordSemaphoreReleased(secondWriter, semaphore, releaseCount: 1);
        _harness.Write(phantomReleaser, phantomField, instance: null);
        _harness.Detector.RecordSemaphoreReleased(phantomReleaser, semaphore, releaseCount: 2);
        _harness.Detector.RecordSemaphoreReleaseRetracted(phantomReleaser, semaphore, releaseCount: 2);

        // Assert
        _harness.Detector.RecordSemaphoreAcquired(firstAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(firstAcquirer, firstField, instance: null));
        _harness.Detector.RecordSemaphoreAcquired(secondAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(secondAcquirer, secondField, instance: null));
        Assert.True(_harness.ReadIsRace(secondAcquirer, phantomField, instance: null));
    }

    [Fact]
    public void RetractedRelease_DoesNotTakeAwayAPermitThatALaterReleasePushedBehindIt()
    {
        // Arrange
        var writer = _harness.NewThread();
        var phantomReleaser = _harness.NewThread();
        var laterWriter = _harness.NewThread();
        var firstAcquirer = _harness.NewThread();
        var secondAcquirer = _harness.NewThread();
        var semaphore = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");
        var laterField = _harness.NewStaticField("WrittenByLaterReleaser");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0, maximumCount: 2);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreReleased(phantomReleaser, semaphore, releaseCount: 1);
        _harness.Write(laterWriter, laterField, instance: null);
        _harness.Detector.RecordSemaphoreReleased(laterWriter, semaphore, releaseCount: 1);
        _harness.Detector.RecordSemaphoreReleaseRetracted(phantomReleaser, semaphore, releaseCount: 1);

        // Assert
        _harness.Detector.RecordSemaphoreAcquired(firstAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(firstAcquirer, field, instance: null));
        _harness.Detector.RecordSemaphoreAcquired(secondAcquirer, semaphore);
        Assert.False(_harness.ReadIsRace(secondAcquirer, laterField, instance: null));
    }
}
