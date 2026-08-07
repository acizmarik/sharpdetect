// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class TrackedObjectCleanupTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void CollectedLockObject_DoesNotOrderAccessesThroughAReusedId()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var lockObject = DetectorHarness.ObjectWithId(500);
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockReleased(writer, lockObject);
        _harness.CollectObjects(lockObject);
        _harness.Detector.RecordLockAcquired(successor, DetectorHarness.ObjectWithId(500));

        // Assert
        Assert.True(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void CollectedSemaphore_DoesNotHandOutPermitsThroughAReusedId()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var semaphore = DetectorHarness.ObjectWithId(501);
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordSemaphoreCreated(semaphore, initialCount: 0);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordSemaphoreReleased(writer, semaphore, releaseCount: 1);
        _harness.CollectObjects(semaphore);
        _harness.Detector.RecordSemaphoreAcquired(successor, DetectorHarness.ObjectWithId(501));

        // Assert
        Assert.True(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void CollectedEventHandle_DoesNotStaySignaledThroughAReusedId()
    {
        // Arrange
        var signaler = _harness.NewThread();
        var waiter = _harness.NewThread();
        var handle = DetectorHarness.ObjectWithId(502);
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordEventCreated(handle, initialState: false);
        _harness.Write(signaler, field, instance: null);
        _harness.Detector.RecordEventSignaled(signaler, handle);
        _harness.CollectObjects(handle);
        _harness.Detector.RecordEventWaitReturned(waiter, DetectorHarness.ObjectWithId(502), isAutoReset: false);

        // Assert
        Assert.True(_harness.WriteIsRace(waiter, field, instance: null));
    }

    [Fact]
    public void CollectedTask_DoesNotOrderAJoinThroughAReusedId()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var task = DetectorHarness.ObjectWithId(503);
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordTaskScheduled(parent, task);
        _harness.Detector.RecordTaskStarted(worker, task);
        _harness.Write(worker, field, instance: null);
        _harness.Detector.RecordTaskCompleted(worker, task);
        _harness.CollectObjects(task);
        _harness.Detector.RecordTaskJoinFinished(parent, DetectorHarness.ObjectWithId(503));

        // Assert
        Assert.True(_harness.WriteIsRace(parent, field, instance: null));
    }

    [Fact]
    public void CollectedValue_DoesNotPublishThroughAReusedId()
    {
        // Arrange
        var producer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var value = DetectorHarness.ObjectWithId(504);
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(producer, field, instance: null);
        _harness.Detector.RecordValuePublished(producer, value);
        _harness.CollectObjects(value);
        _harness.Detector.RecordValueObserved(consumer, DetectorHarness.ObjectWithId(504));

        // Assert
        Assert.True(_harness.WriteIsRace(consumer, field, instance: null));
    }

    [Fact]
    public void CollectedInstance_ResetsShadowMemoryForItsFields()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var instance = DetectorHarness.ObjectWithId(505);
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(writer, field, instance);
        _harness.CollectObjects(instance);

        // Assert
        Assert.False(_harness.WriteIsRace(successor, field, DetectorHarness.ObjectWithId(505)));
    }

    [Fact]
    public void CollectingOneObject_LeavesOtherObjectsUntouched()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var collectedLock = _harness.NewObject();
        var survivingLock = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, survivingLock);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockReleased(writer, survivingLock);
        _harness.CollectObjects(collectedLock);
        _harness.Detector.RecordLockAcquired(successor, survivingLock);

        // Assert
        Assert.False(_harness.WriteIsRace(successor, field, instance: null));
    }
}
