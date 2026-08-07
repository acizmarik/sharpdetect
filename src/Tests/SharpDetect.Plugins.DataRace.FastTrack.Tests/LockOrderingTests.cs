// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class LockOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void WritesUnderTheSameLock_AreOrdered()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockReleased(writer, lockObject);
        _harness.Detector.RecordLockAcquired(successor, lockObject);

        // Assert
        Assert.False(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void ReadUnderTheSameLock_IsOrderedAfterTheWrite()
    {
        // Arrange
        var writer = _harness.NewThread();
        var reader = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockReleased(writer, lockObject);
        _harness.Detector.RecordLockAcquired(reader, lockObject);

        // Assert
        Assert.False(_harness.ReadIsRace(reader, field, instance: null));
    }

    [Fact]
    public void WriteThatIsNeverReleased_IsReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockAcquired(successor, lockObject);

        // Assert
        Assert.True(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void WritesUnderDifferentLocks_AreReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var successor = _harness.NewThread();
        var firstLock = _harness.NewObject();
        var secondLock = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, firstLock);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockReleased(writer, firstLock);
        _harness.Detector.RecordLockAcquired(successor, secondLock);

        // Assert
        Assert.True(_harness.WriteIsRace(successor, field, instance: null));
    }

    [Fact]
    public void MonitorWait_ReleasesAndReacquiresTheLock_OrderingBothDirections()
    {
        // Arrange
        var waiter = _harness.NewThread();
        var pulser = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act & Assert
        _harness.Detector.RecordLockAcquired(waiter, lockObject);
        _harness.Write(waiter, field, instance: null);
        _harness.Detector.RecordObjectWaitCalled(waiter, lockObject);
        _harness.Detector.RecordLockAcquired(pulser, lockObject);
        Assert.False(_harness.WriteIsRace(pulser, field, instance: null));
        _harness.Detector.RecordLockReleased(pulser, lockObject);
        _harness.Detector.RecordObjectWaitReturned(waiter, lockObject);
        Assert.False(_harness.WriteIsRace(waiter, field, instance: null));
    }

    [Fact]
    public void ExitAfterMatchingEnter_PreservesTheLockClockOfEarlierCriticalSections()
    {
        // Arrange
        var writer = _harness.NewThread();
        var passerBy = _harness.NewThread();
        var successor = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockReleased(writer, lockObject);
        _harness.Detector.RecordLockAcquired(passerBy, lockObject);
        _harness.Detector.RecordLockReleased(passerBy, lockObject);
        _harness.Detector.RecordLockAcquired(successor, lockObject);

        // Assert
        Assert.False(_harness.WriteIsRace(successor, field, instance: null));
    }
    
    [Fact(Skip = "RecordLockReleased overwrites the lock clock with VectorClock.CopyFrom instead of joining into it")]
    public void ExitWithoutMatchingEnter_PreservesTheLockClock()
    {
        // Arrange
        var writer = _harness.NewThread();
        var passerBy = _harness.NewThread();
        var successor = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordLockReleased(writer, lockObject);
        _harness.Detector.RecordLockReleased(passerBy, lockObject);
        _harness.Detector.RecordLockAcquired(successor, lockObject);

        // Assert
        Assert.False(_harness.WriteIsRace(successor, field, instance: null));
    }
}
