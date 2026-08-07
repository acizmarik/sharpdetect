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
}
