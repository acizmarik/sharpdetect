// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class ValuePublicationAliasingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void SecondContainerPublishingAMutatedValue_OrdersTheConsumerAfterTheMutation()
    {
        // Arrange
        var producer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var firstContainer = _harness.NewObject();
        var secondContainer = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Detector.RecordValuePublished(producer, firstContainer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, firstContainer, value);
        _harness.Write(producer, field, value);
        _harness.Detector.RecordValuePublished(producer, secondContainer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, secondContainer, value);
        _harness.Detector.RecordValuePublished(consumer, secondContainer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(consumer, secondContainer, value);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, field, value));
    }

    [Fact]
    public void ConsumerOfTheSecondContainer_IsNotOrderedAfterTheFirstContainersPublisher()
    {
        // Arrange
        var firstProducer = _harness.NewThread();
        var secondProducer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var firstContainer = _harness.NewObject();
        var secondContainer = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(firstProducer, field, value);
        _harness.Detector.RecordValuePublished(firstProducer, firstContainer, value);
        _harness.Detector.RecordValuePublished(secondProducer, secondContainer, value);
        _harness.Detector.RecordValueObserved(consumer, secondContainer, value);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, field, value));
    }

    [Fact]
    public void ConsumerOfAContainerTheValueWasNeverPublishedInto_IsNotOrderedAfterThePublisher()
    {
        // Arrange
        var producer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var publishedContainer = _harness.NewObject();
        var unrelatedContainer = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(producer, field, value);
        _harness.Detector.RecordValuePublished(producer, publishedContainer, value);
        _harness.Detector.RecordValueObserved(consumer, unrelatedContainer, value);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, field, value));
    }

    [Fact]
    public void SecondPublicationAsAnUnconditionalStore_OrdersTheConsumerAfterTheMutation()
    {
        // Arrange
        var producer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Detector.RecordValuePublished(producer, container, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, container, value);
        _harness.Write(producer, field, value);
        _harness.Detector.RecordValuePublished(producer, container, value, onlyIfAbsent: false);
        _harness.Detector.RecordValueObserved(producer, container, value);
        _harness.Detector.RecordValuePublished(consumer, container, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(consumer, container, value);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, field, value));
    }

    [Fact]
    public void TwoFactoriesMutatingTheSameValueConcurrently_IsReported()
    {
        // Arrange
        var producer = _harness.NewThread();
        var contender = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Detector.RecordValuePublished(producer, container, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, container, value);
        _harness.Detector.RecordValuePublished(contender, container, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(contender, container, value);

        _harness.Write(producer, field, value);

        // Assert
        var race = _harness.Write(contender, field, value);
        Assert.NotNull(race);
        Assert.Equal(producer, race.LastAccess.ProcessThreadId);
        Assert.Equal(contender, race.CurrentAccess.ProcessThreadId);
    }
}
