// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class EventWaitHandleOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void SetAfterWrite_OrdersTheWaiter()
    {
        // Arrange
        var signaler = _harness.NewThread();
        var waiter = _harness.NewThread();
        var handle = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordEventCreated(handle, initialState: false);
        _harness.Write(signaler, field, instance: null);
        _harness.Detector.RecordEventSignaled(signaler, handle);
        _harness.Detector.RecordEventWaitReturned(waiter, handle, isAutoReset: false);

        // Assert
        Assert.False(_harness.WriteIsRace(waiter, field, instance: null));
    }

    [Fact]
    public void ManualResetEvent_StaysSignaled_AndOrdersEveryWaiter()
    {
        // Arrange
        var signaler = _harness.NewThread();
        var firstWaiter = _harness.NewThread();
        var secondWaiter = _harness.NewThread();
        var handle = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act & Assert
        _harness.Detector.RecordEventCreated(handle, initialState: false);
        _harness.Write(signaler, field, instance: null);
        _harness.Detector.RecordEventSignaled(signaler, handle);
        _harness.Detector.RecordEventWaitReturned(firstWaiter, handle, isAutoReset: false);
        Assert.False(_harness.ReadIsRace(firstWaiter, field, instance: null));
        _harness.Detector.RecordEventWaitReturned(secondWaiter, handle, isAutoReset: false);
        Assert.False(_harness.ReadIsRace(secondWaiter, field, instance: null));
    }

    [Fact]
    public void AutoResetEvent_ConsumesTheSignal_AndLeavesTheSecondWaiterUnordered()
    {
        // Arrange
        var signaler = _harness.NewThread();
        var firstWaiter = _harness.NewThread();
        var secondWaiter = _harness.NewThread();
        var handle = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act & Assert
        _harness.Detector.RecordEventCreated(handle, initialState: false);
        _harness.Write(signaler, field, instance: null);
        _harness.Detector.RecordEventSignaled(signaler, handle);
        _harness.Detector.RecordEventWaitReturned(firstWaiter, handle, isAutoReset: true);
        Assert.False(_harness.ReadIsRace(firstWaiter, field, instance: null));
        _harness.Detector.RecordEventWaitReturned(secondWaiter, handle, isAutoReset: true);
        Assert.True(_harness.ReadIsRace(secondWaiter, field, instance: null));
    }

    [Fact]
    public void EventCreatedAlreadySignaled_CarriesNoHappensBefore_AndIsReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var waiter = _harness.NewThread();
        var handle = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordEventCreated(handle, initialState: true);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordEventWaitReturned(waiter, handle, isAutoReset: false);

        // Assert
        Assert.True(_harness.WriteIsRace(waiter, field, instance: null));
    }

    [Fact]
    public void WaitReturningOnAnUnsignaledEvent_AddsNoOrdering_AndIsReported()
    {
        // Arrange
        var writer = _harness.NewThread();
        var waiter = _harness.NewThread();
        var handle = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordEventCreated(handle, initialState: false);
        _harness.Write(writer, field, instance: null);
        _harness.Detector.RecordEventWaitReturned(waiter, handle, isAutoReset: false);

        // Assert
        Assert.True(_harness.WriteIsRace(waiter, field, instance: null));
    }

    [Fact]
    public void SignalsFromTwoThreads_AreBothVisibleToTheWaiter()
    {
        // Arrange
        var firstSignaler = _harness.NewThread();
        var secondSignaler = _harness.NewThread();
        var waiter = _harness.NewThread();
        var handle = _harness.NewObject();
        var firstField = _harness.NewStaticField("First");
        var secondField = _harness.NewStaticField("Second");

        // Act
        _harness.Detector.RecordEventCreated(handle, initialState: false);
        _harness.Write(firstSignaler, firstField, instance: null);
        _harness.Detector.RecordEventSignaled(firstSignaler, handle);
        _harness.Write(secondSignaler, secondField, instance: null);
        _harness.Detector.RecordEventSignaled(secondSignaler, handle);
        _harness.Detector.RecordEventWaitReturned(waiter, handle, isAutoReset: false);

        // Assert
        Assert.False(_harness.ReadIsRace(waiter, firstField, instance: null));
        Assert.False(_harness.ReadIsRace(waiter, secondField, instance: null));
    }
    
    [Fact]
    public void ResetDropsOrderingAccumulatedBeforeIt_AndIsReported()
    {
        // Arrange
        var firstSignaler = _harness.NewThread();
        var secondSignaler = _harness.NewThread();
        var waiter = _harness.NewThread();
        var handle = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordEventCreated(handle, initialState: false);
        _harness.Write(firstSignaler, field, instance: null);
        _harness.Detector.RecordEventSignaled(firstSignaler, handle);
        _harness.Detector.RecordEventReset(handle);
        _harness.Detector.RecordEventSignaled(secondSignaler, handle);
        _harness.Detector.RecordEventWaitReturned(waiter, handle, isAutoReset: false);

        // Assert
        Assert.True(_harness.ReadIsRace(waiter, field, instance: null));
    }
}
