// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class ValuePublicationReportingOrderTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void ObserverSeenBeforeThePublisher_IsStillOrderedAfterThePublication()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(publisher, field, value);
        _harness.Detector.RecordValueObserved(observer, container, value);
        _harness.Detector.RecordValuePublished(publisher, container, value, onlyIfAbsent: true);

        // Assert
        Assert.False(_harness.ReadIsRace(observer, field, value));
    }

    [Fact]
    public void ObserverSeenAfterThePublisher_IsOrderedAfterThePublication()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(publisher, field, value);
        _harness.Detector.RecordValuePublished(publisher, container, value, onlyIfAbsent: true);
        _harness.Detector.RecordValueObserved(observer, container, value);

        // Assert
        Assert.False(_harness.ReadIsRace(observer, field, value));
    }

    [Fact]
    public void ObserverSeenBeforeThePublisher_IsNotOrderedAgainstPostPublicationWork()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Detector.RecordValueObserved(observer, container, value);
        _harness.Detector.RecordValuePublished(publisher, container, value, onlyIfAbsent: true);
        _harness.Write(publisher, field, value);

        // Assert
        Assert.True(_harness.ReadIsRace(observer, field, value));
    }

    [Fact]
    public void ObserverOfADifferentContainer_IsNotOrderedByALaterPublication()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var container = _harness.NewObject();
        var unrelatedContainer = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(publisher, field, value);
        _harness.Detector.RecordValueObserved(observer, unrelatedContainer, value);
        _harness.Detector.RecordValuePublished(publisher, container, value, onlyIfAbsent: true);

        // Assert
        Assert.True(_harness.ReadIsRace(observer, field, value));
    }

    [Fact]
    public void PendingObservers_DoNotCarryEachOthersWork()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var mutator = _harness.NewThread();
        var observer = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();
        var unrelated = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(mutator, field, unrelated);
        _harness.Detector.RecordValueObserved(mutator, container, value);
        _harness.Detector.RecordValueObserved(observer, container, value);
        _harness.Detector.RecordValuePublished(publisher, container, value, onlyIfAbsent: true);

        // Assert
        Assert.True(_harness.ReadIsRace(observer, field, unrelated));
    }

    [Fact]
    public void PendingObservations_AreBounded()
    {
        // Arrange
        var observer = _harness.NewThread();

        // Act
        for (var i = 0; i < FastTrackDetector.MaxPendingPublicationObservations * 2; i++)
            _harness.Detector.RecordValueObserved(observer, _harness.NewObject(), _harness.NewObject());

        // Assert
        Assert.Equal(
            FastTrackDetector.MaxPendingPublicationObservations,
            _harness.Detector.GetPublicationObserverEntryCount());
        Assert.Equal(
            FastTrackDetector.MaxPendingPublicationObservations * 2,
            _harness.Detector.GetIndexedPublicationParticipantCount());
    }

    [Fact]
    public void EvictedObservation_NoLongerOrdersItsObserverAfterALaterPublication()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();
        var field = _harness.NewInstanceField("State");

        // Act
        _harness.Write(publisher, field, value);
        _harness.Detector.RecordValueObserved(observer, container, value);
        for (var i = 0; i <= FastTrackDetector.MaxPendingPublicationObservations; i++)
            _harness.Detector.RecordValueObserved(observer, _harness.NewObject(), _harness.NewObject());

        _harness.Detector.RecordValuePublished(publisher, container, value, onlyIfAbsent: true);

        // Assert
        Assert.True(_harness.ReadIsRace(observer, field, value));
    }

    [Fact]
    public void CollectedValue_ReleasesThePendingObserversRecordedForItsSlots()
    {
        // Arrange
        var observer = _harness.NewThread();
        var container = _harness.NewObject();
        var value = _harness.NewObject();

        // Act
        _harness.Detector.RecordValueObserved(observer, container, value);
        _harness.CollectObjects(value);

        // Assert
        Assert.Equal(0, _harness.Detector.GetPublicationObserverEntryCount());
        Assert.Equal(0, _harness.Detector.GetIndexedPublicationParticipantCount());
    }
}
