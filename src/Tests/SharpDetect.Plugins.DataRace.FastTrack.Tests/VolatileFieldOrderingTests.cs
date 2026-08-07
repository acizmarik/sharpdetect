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
        _harness.VolatileRead(consumer, flag, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, guardedByPublisher, instance: null));
        Assert.False(_harness.ReadIsRace(consumer, guardedByOverwriter, instance: null));
    }
}
