// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class ReadWriteRaceAttributionTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void WriteAfterOwnRead_IsReportedAgainstTheOtherReader()
    {
        // Arrange
        var reader = _harness.NewThread();
        var writer = _harness.NewThread();
        var field = _harness.NewStaticField("Counter");

        // Act
        _harness.Read(reader, field, instance: null);
        _harness.Read(writer, field, instance: null);
        var race = _harness.Write(writer, field, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(reader, race.LastAccess.ProcessThreadId);
        Assert.Equal(AccessType.Read, race.LastAccess.AccessType);
    }

    [Fact]
    public void WriteAfterOwnRead_OnInstanceField_IsReportedAgainstTheOtherReader()
    {
        // Arrange
        var reader = _harness.NewThread();
        var writer = _harness.NewThread();
        var instance = _harness.NewObject();
        var field = _harness.NewInstanceField("Counter");

        // Act
        _harness.Read(reader, field, instance);
        _harness.Read(writer, field, instance);
        var race = _harness.Write(writer, field, instance);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(reader, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void WriteRacingOneOfSeveralReaders_IsReportedAgainstTheRacingReader()
    {
        // Arrange
        var racingReader = _harness.NewThread();
        var orderedReader = _harness.NewThread();
        var writer = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Counter");

        // Act
        _harness.Read(racingReader, field, instance: null);
        _harness.Detector.RecordLockAcquired(orderedReader, lockObject);
        _harness.Read(orderedReader, field, instance: null);
        _harness.Detector.RecordLockReleased(orderedReader, lockObject);
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Read(writer, field, instance: null);
        var race = _harness.Write(writer, field, instance: null);

        // Assert
        Assert.NotNull(race);
        Assert.Equal(racingReader, race.LastAccess.ProcessThreadId);
    }

    [Fact]
    public void WriteAfterOrderedReads_IsNotReported()
    {
        // Arrange
        var reader = _harness.NewThread();
        var writer = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Counter");

        // Act
        _harness.Detector.RecordLockAcquired(reader, lockObject);
        _harness.Read(reader, field, instance: null);
        _harness.Detector.RecordLockReleased(reader, lockObject);
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Read(writer, field, instance: null);

        // Assert
        Assert.False(_harness.WriteIsRace(writer, field, instance: null));
    }

    [Fact]
    public void ReadsBeforeAnOrderedWrite_DoNotRaceWithALaterWrite()
    {
        // Arrange
        var reader = _harness.NewThread();
        var writer = _harness.NewThread();
        var lockObject = _harness.NewObject();
        var field = _harness.NewStaticField("Counter");

        // Act
        _harness.Detector.RecordLockAcquired(reader, lockObject);
        _harness.Read(reader, field, instance: null);
        _harness.Read(writer, field, instance: null);
        _harness.Write(reader, field, instance: null);
        _harness.Detector.RecordLockReleased(reader, lockObject);
        _harness.Detector.RecordLockAcquired(writer, lockObject);
        _harness.Read(writer, field, instance: null);

        // Assert
        Assert.False(_harness.WriteIsRace(writer, field, instance: null));
    }
}
