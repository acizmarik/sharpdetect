// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class ValuePublicationAliasingTests
{
    private const string PublicationClocksAreKeyedByValue =
        "Publication clocks are keyed by the published value rather than by (container, key)";

    private readonly DetectorHarness _harness = new();
    
    [Fact(Skip = PublicationClocksAreKeyedByValue)]
    public void SecondContainerPublishingAMutatedValue_OrdersTheConsumerAfterTheMutation()
    {
        // Arrange
        var producer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Detector.RecordValuePublished(producer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, value);
        _harness.Write(producer, field, value);
        _harness.Detector.RecordValuePublished(producer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, value);
        _harness.Detector.RecordValuePublished(consumer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(consumer, value);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, field, value));
    }

    [Fact(Skip = PublicationClocksAreKeyedByValue)]
    public void ConsumerOfTheSecondContainer_IsNotOrderedAfterTheFirstContainersPublisher()
    {
        // Arrange
        var firstProducer = _harness.NewThread();
        var secondProducer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(firstProducer, field, value);
        _harness.Detector.RecordValuePublished(firstProducer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValuePublished(secondProducer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(consumer, value);

        // Assert
        Assert.True(_harness.ReadIsRace(consumer, field, value));
    }

    [Fact]
    public void SecondPublicationThatJoins_OrdersTheConsumerAfterTheMutation()
    {
        // Arrange
        var producer = _harness.NewThread();
        var consumer = _harness.NewThread();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Detector.RecordValuePublished(producer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, value);
        _harness.Write(producer, field, value);
        _harness.Detector.RecordValuePublished(producer, value, onlyIfAbsent: false);
        _harness.Detector.RecordValueObserved(producer, value);
        _harness.Detector.RecordValuePublished(consumer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(consumer, value);

        // Assert
        Assert.False(_harness.ReadIsRace(consumer, field, value));
    }

    [Fact]
    public void TwoFactoriesMutatingTheSameValueConcurrently_IsReported()
    {
        // Arrange
        var producer = _harness.NewThread();
        var contender = _harness.NewThread();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Detector.RecordValuePublished(producer, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(producer, value);
        _harness.Detector.RecordValuePublished(contender, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(contender, value);

        _harness.Write(producer, field, value);

        // Assert
        var race = _harness.Write(contender, field, value);
        Assert.NotNull(race);
        Assert.Equal(producer, race.LastAccess.ProcessThreadId);
        Assert.Equal(contender, race.CurrentAccess.ProcessThreadId);
    }
}
