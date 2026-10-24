// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class MixedAtomicAccessTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void PlainWriteAfterUnorderedReadModifyWrite_IsRace()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act
        _harness.ReadModifyWrite(incrementer, counter, instance: null);
        _harness.Read(plainWriter, counter, instance: null);
        var race = _harness.Write(plainWriter, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(AccessType.Write, race.CurrentAccess.AccessType);
        Assert.Equal(incrementer, race.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.AtomicWrite, race.LastAccess.AccessType);
    }

    [Fact]
    public void ReadModifyWriteAfterUnorderedPlainWrite_IsRace()
    {
        // Arrange
        var plainWriter = _harness.NewThread();
        var incrementer = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act
        _harness.Read(plainWriter, counter, instance: null);
        _harness.Write(plainWriter, counter, instance: null);
        var race = _harness.ReadModifyWrite(incrementer, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(AccessType.AtomicWrite, race.CurrentAccess.AccessType);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.Write, race.LastAccess.AccessType);
    }

    [Fact]
    public void ReadModifyWriteOnInstanceField_RacesWithPlainWriteOnTheSameObjectOnly()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var counter = _harness.NewInstanceField("Counter");
        var incremented = _harness.NewObject();
        var other = _harness.NewObject();

        // Act
        _harness.ReadModifyWrite(incrementer, counter, incremented);

        // Assert
        Assert.False(_harness.WriteIsRace(plainWriter, counter, other));
        Assert.True(_harness.WriteIsRace(plainWriter, counter, incremented));
    }

    [Fact]
    public void AtomicReadAfterUnorderedPlainWrite_IsRace()
    {
        // Arrange
        var plainWriter = _harness.NewThread();
        var reader = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(plainWriter, flag, instance: null);
        var race = _harness.VolatileRead(reader, flag, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(AccessType.AtomicRead, race.CurrentAccess.AccessType);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void PlainWriteAfterUnorderedAtomicRead_IsRace()
    {
        // Arrange
        var reader = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.VolatileRead(reader, flag, instance: null);
        var race = _harness.Write(plainWriter, flag, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(reader, race.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.AtomicRead, race.LastAccess.AccessType);
    }

    [Fact]
    public void UnorderedAtomicAccesses_NeverRace()
    {
        // Arrange
        var first = _harness.NewThread();
        var second = _harness.NewThread();
        var location = _harness.NewStaticField("Location");

        // Act
        var races = new[]
        {
            _harness.VolatileWrite(first, location, instance: null),
            _harness.VolatileWrite(second, location, instance: null),
            _harness.ReadModifyWrite(first, location, instance: null),
            _harness.VolatileRead(second, location, instance: null),
            CompareExchange(_harness, first, location, succeeded: false),
            CompareExchange(_harness, second, location, succeeded: true)
        };

        // Assert
        Assert.All(races, Assert.Null);
    }

    [Fact]
    public void PlainAndAtomicAccessesBySameThread_DoNotRace()
    {
        // Arrange
        var thread = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act
        var races = new[]
        {
            _harness.Write(thread, counter, instance: null),
            _harness.ReadModifyWrite(thread, counter, instance: null),
            _harness.Write(thread, counter, instance: null),
            _harness.VolatileRead(thread, counter, instance: null)
        };

        // Assert
        Assert.All(races, Assert.Null);
    }

    [Fact]
    public void PlainWriteAfterAcquiringTheReadModifyWrite_IsNotRace()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the acquire sees the clock of the release, so the operation must be recorded at that epoch
        _harness.ReadModifyWrite(incrementer, counter, instance: null);
        _harness.VolatileRead(plainWriter, counter, instance: null);

        // Assert
        Assert.False(_harness.WriteIsRace(plainWriter, counter, instance: null));
    }

    [Fact]
    public void PlainWriteAfterAcquiringTheSucceededCompareExchange_IsNotRace()
    {
        // Arrange
        var exchanger = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var location = _harness.NewStaticField("Location");

        // Act
        CompareExchange(_harness, exchanger, location, succeeded: true);
        _harness.VolatileRead(plainWriter, location, instance: null);

        // Assert
        Assert.False(_harness.WriteIsRace(plainWriter, location, instance: null));
    }

    [Fact]
    public void PlainWriteAfterAcquiringTheVolatileWrite_IsNotRace()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.VolatileWrite(publisher, flag, instance: null);
        _harness.VolatileRead(plainWriter, flag, instance: null);

        // Assert
        Assert.False(_harness.WriteIsRace(plainWriter, flag, instance: null));
    }

    [Fact]
    public void AtomicAccessAfterAcquiringThePlainWrite_IsNotRace()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var incrementer = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");
        var lockObject = _harness.NewObject();

        // Act
        _harness.Write(initializer, counter, instance: null);
        _harness.Detector.RecordLockReleased(initializer, lockObject);
        _harness.Detector.RecordLockAcquired(incrementer, lockObject);

        // Assert
        Assert.Null(_harness.ReadModifyWrite(incrementer, counter, instance: null));
    }

    [Fact]
    public void ReadModifyWriteThatThrew_IsNotRecorded()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the operation never reports its completion, so it did not access the location
        _harness.AtomicRelease(incrementer, counter, instance: null);

        // Assert
        Assert.False(_harness.WriteIsRace(plainWriter, counter, instance: null));
    }

    [Fact]
    public void PlainWriteRacingWithOneOfSeveralAtomicReaders_IsReportedAgainstThatReader()
    {
        // Arrange
        var orderedReader = _harness.NewThread();
        var unorderedReader = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");
        var lockObject = _harness.NewObject();

        // Act
        _harness.VolatileRead(unorderedReader, flag, instance: null);
        _harness.VolatileRead(orderedReader, flag, instance: null);
        _harness.Detector.RecordLockReleased(orderedReader, lockObject);
        _harness.Detector.RecordLockAcquired(plainWriter, lockObject);
        var race = _harness.Write(plainWriter, flag, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(unorderedReader, race.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.AtomicRead, race.LastAccess.AccessType);
    }

    [Fact]
    public void PlainWrite_ForgetsTheAtomicAccessesOrderedBeforeIt()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var resetter = _harness.NewThread();
        var successor = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");
        var lockObject = _harness.NewObject();

        // Act - the reset races with the increment, and the successor is ordered after the reset only
        _harness.ReadModifyWrite(incrementer, counter, instance: null);
        var resetRace = _harness.Write(resetter, counter, instance: null);
        _harness.Detector.RecordLockReleased(resetter, lockObject);
        _harness.Detector.RecordLockAcquired(successor, lockObject);
        var successorRace = _harness.Write(successor, counter, instance: null);

        // Assert - the race was reported once, at the reset
        Assert.NotNull(resetRace);
        Assert.Null(successorRace);
    }

    [Fact]
    public void ReadModifyWriteInFlight_DoesNotSubsumeAnAtomicAccessItAcquiresAfterItsRelease()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var reader = _harness.NewThread();
        var otherIncrementer = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");
        var lockObject = _harness.NewObject();

        // Act - the reader observes the in-flight increment, the other increment is ordered after the reader only.
        // The first increment completes last and acquires the other one, which its release did not know about
        _harness.AtomicRelease(incrementer, counter, instance: null);
        _harness.VolatileRead(reader, counter, instance: null);
        _harness.Detector.RecordLockReleased(reader, lockObject);
        _harness.Detector.RecordLockAcquired(otherIncrementer, lockObject);
        _harness.ReadModifyWrite(otherIncrementer, counter, instance: null);
        _harness.AtomicAcquire(incrementer, counter, instance: null);
        var race = _harness.Write(reader, counter, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(otherIncrementer, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void PlainWriteAfterObservingAnInFlightReadModifyWrite_IsNotRace()
    {
        // Arrange
        var first = _harness.NewThread();
        var second = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act - the second decrement reads the first one, which reports its completion only after the reset
        _harness.AtomicRelease(first, counter, instance: null);
        _harness.ReadModifyWrite(second, counter, instance: null);
        _harness.Write(second, counter, instance: null);
        var race = _harness.AtomicAcquire(first, counter, instance: null);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void PlainWriteAfterUnorderedVolatileWrite_IsRace()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var plainWriter = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.VolatileWrite(publisher, flag, instance: null);
        var race = _harness.Write(plainWriter, flag, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(publisher, race.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.AtomicWrite, race.LastAccess.AccessType);
    }

    [Fact]
    public void VolatileWriteAfterUnorderedPlainWrite_IsRace()
    {
        // Arrange
        var plainWriter = _harness.NewThread();
        var publisher = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Write(plainWriter, flag, instance: null);
        var race = _harness.VolatileWrite(publisher, flag, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(AccessType.AtomicWrite, race.CurrentAccess.AccessType);
        Assert.Equal(plainWriter, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void AtomicAccessesOfExcludedField_AreNotRecorded()
    {
        // Arrange
        var thread = _harness.NewThread();
        var readOnlyField = _harness.NewStaticField(_harness.NewType("Holder"), "ReadOnly", isReadOnly: true);

        // Act
        _harness.ReadModifyWrite(thread, readOnlyField, instance: null);
        _harness.VolatileRead(thread, readOnlyField, instance: null);
        _harness.VolatileWrite(thread, readOnlyField, instance: null);

        // Assert
        Assert.Equal(0, _harness.Detector.GetShadowVariableCount());
    }

    [Fact]
    public void FinalizerWriteAfterAtomicAccessOnAnotherThread_IsNotRace()
    {
        // Arrange
        var owner = _harness.NewThread();
        var finalizer = _harness.NewThread();
        var instance = _harness.NewObject();
        var counter = _harness.NewInstanceField("Counter");

        // Act
        _harness.ReadModifyWrite(owner, counter, instance);
        _harness.QueueForFinalization(instance);

        // Assert
        Assert.False(_harness.WriteIsRace(finalizer, counter, instance));
    }

    [Fact]
    public void PlainReadAfterUnorderedAtomicStore_IsNotReportedByDefault()
    {
        // Arrange
        var incrementer = _harness.NewThread();
        var plainReader = _harness.NewThread();
        var counter = _harness.NewStaticField("Counter");

        // Act
        _harness.ReadModifyWrite(incrementer, counter, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(plainReader, counter, instance: null));
    }

    [Fact]
    public void AtomicStoreAfterUnorderedPlainRead_IsNotReportedByDefault()
    {
        // Arrange
        var plainReader = _harness.NewThread();
        var publisher = _harness.NewThread();
        var flag = _harness.NewStaticField("Flag");

        // Act
        _harness.Read(plainReader, flag, instance: null);

        // Assert
        Assert.Null(_harness.VolatileWrite(publisher, flag, instance: null));
    }

    private static DataRaceInfo? CompareExchange(DetectorHarness harness, ProcessThreadId thread, MdToken field, bool succeeded)
    {
        harness.CompareExchangeRelease(thread, field, instance: null);
        return harness.CompareExchangeCompleted(thread, field, instance: null, succeeded);
    }
}
