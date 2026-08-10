// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class TaskOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void SchedulingATask_OrdersParentWritesBeforeTheBody()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var task = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(parent, field, instance: null);
        _harness.Detector.RecordTaskScheduled(parent, task);
        _harness.Detector.RecordTaskStarted(worker, task);

        // Assert
        Assert.False(_harness.WriteIsRace(worker, field, instance: null));
    }

    [Fact]
    public void JoiningACompletedTask_OrdersTheBodyBeforeTheWaiter()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var task = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordTaskScheduled(parent, task);
        _harness.Detector.RecordTaskStarted(worker, task);
        _harness.Write(worker, field, instance: null);
        _harness.Detector.RecordTaskCompleted(worker, task);
        _harness.Detector.RecordTaskJoinFinished(parent, task);

        // Assert
        Assert.False(_harness.WriteIsRace(parent, field, instance: null));
    }

    [Fact]
    public void TaskThatIsNeverJoined_IsReported()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var task = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordTaskScheduled(parent, task);
        _harness.Detector.RecordTaskStarted(worker, task);
        _harness.Write(worker, field, instance: null);
        _harness.Detector.RecordTaskCompleted(worker, task);

        // Assert
        Assert.True(_harness.WriteIsRace(parent, field, instance: null));
    }

    [Fact]
    public void StartingATaskThatWasNeverScheduled_AddsNoOrdering_AndIsReported()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var task = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(parent, field, instance: null);
        _harness.Detector.RecordTaskStarted(worker, task);

        // Assert
        Assert.True(_harness.WriteIsRace(worker, field, instance: null));
    }

    [Fact]
    public void SiblingTasksOfTheSameParent_AreUnorderedWithEachOther()
    {
        // Arrange
        var parent = _harness.NewThread();
        var firstWorker = _harness.NewThread();
        var secondWorker = _harness.NewThread();
        var firstTask = _harness.NewObject();
        var secondTask = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordTaskScheduled(parent, firstTask);
        _harness.Detector.RecordTaskScheduled(parent, secondTask);
        _harness.Detector.RecordTaskStarted(firstWorker, firstTask);
        _harness.Detector.RecordTaskStarted(secondWorker, secondTask);
        _harness.Write(firstWorker, field, instance: null);

        // Assert
        Assert.True(_harness.WriteIsRace(secondWorker, field, instance: null));
    }
    
    [Fact]
    public void JoiningBeforeCompletion_ConveysOnlyTheSchedulingClock()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var waiter = _harness.NewThread();
        var task = _harness.NewObject();
        var parentField = _harness.NewStaticField("WrittenByParent");
        var bodyField = _harness.NewStaticField("WrittenByBody");

        // Act
        _harness.Write(parent, parentField, instance: null);
        _harness.Detector.RecordTaskScheduled(parent, task);
        _harness.Detector.RecordTaskStarted(worker, task);
        _harness.Write(worker, bodyField, instance: null);
        _harness.Detector.RecordTaskJoinFinished(waiter, task);

        // Assert
        Assert.False(_harness.ReadIsRace(waiter, parentField, instance: null));
        Assert.True(_harness.ReadIsRace(waiter, bodyField, instance: null));
    }

    [Fact]
    public void CompletingPromise_OrdersTheCompleterBeforeEveryLaterJoiner()
    {
        // Arrange
        var completer = _harness.NewThread();
        var waiter = _harness.NewThread();
        var promise = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(completer, field, instance: null);
        _harness.Detector.RecordTaskPromiseCompleted(completer, promise);
        _harness.Detector.RecordTaskJoinFinished(waiter, promise);

        // Assert
        Assert.False(_harness.ReadIsRace(waiter, field, instance: null));
    }

    [Fact]
    public void RepeatedCompletion_PreservesTheFirstCompletersRelease()
    {
        // Arrange
        var winner = _harness.NewThread();
        var loser = _harness.NewThread();
        var waiter = _harness.NewThread();
        var promise = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(winner, field, instance: null);
        _harness.Detector.RecordTaskPromiseCompleted(winner, promise);
        _harness.Detector.RecordTaskPromiseCompleted(loser, promise);
        _harness.Detector.RecordTaskJoinFinished(waiter, promise);

        // Assert
        Assert.False(_harness.ReadIsRace(waiter, field, instance: null));
    }

    [Fact]
    public void RegisteringContinuation_OrdersRegistrarWritesBeforeTheContinuationBody()
    {
        // Arrange
        var registrar = _harness.NewThread();
        var worker = _harness.NewThread();
        var continuation = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(registrar, field, instance: null);
        _harness.Detector.RecordTaskContinuationRegistered(registrar, continuation);
        _harness.Detector.RecordTaskStarted(worker, continuation);

        // Assert
        Assert.False(_harness.ReadIsRace(worker, field, instance: null));
    }

    [Fact]
    public void SchedulingContinuationFromTheCompleter_DoesNotDiscardTheRegistrarsRelease()
    {
        // Arrange
        var registrar = _harness.NewThread();
        var completer = _harness.NewThread();
        var worker = _harness.NewThread();
        var continuation = _harness.NewObject();
        var registrarField = _harness.NewStaticField("RegistrarShared");
        var completerField = _harness.NewStaticField("CompleterShared");

        // Act
        _harness.Write(registrar, registrarField, instance: null);
        _harness.Detector.RecordTaskContinuationRegistered(registrar, continuation);
        _harness.Write(completer, completerField, instance: null);
        _harness.Detector.RecordTaskScheduled(completer, continuation);
        _harness.Detector.RecordTaskStarted(worker, continuation);

        // Assert
        Assert.False(_harness.ReadIsRace(worker, registrarField, instance: null));
        Assert.False(_harness.ReadIsRace(worker, completerField, instance: null));
    }

    [Fact]
    public void WritingAfterRegisteringContinuation_IsNotOrderedBeforeTheBody()
    {
        // Arrange
        var registrar = _harness.NewThread();
        var worker = _harness.NewThread();
        var continuation = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordTaskContinuationRegistered(registrar, continuation);
        _harness.Write(registrar, field, instance: null);
        _harness.Detector.RecordTaskStarted(worker, continuation);

        // Assert
        Assert.True(_harness.ReadIsRace(worker, field, instance: null));
    }

    [Fact]
    public void RegisteredContinuationClock_IsNotReusedByLaterTaskWithTheSameId()
    {
        // Arrange
        var registrar = _harness.NewThread();
        var firstWorker = _harness.NewThread();
        var secondWorker = _harness.NewThread();
        var continuation = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Detector.RecordTaskContinuationRegistered(registrar, continuation);
        _harness.Detector.RecordTaskStarted(firstWorker, continuation);
        _harness.Write(registrar, field, instance: null);
        _harness.Detector.RecordTaskStarted(secondWorker, continuation);

        // Assert
        Assert.True(_harness.ReadIsRace(secondWorker, field, instance: null));
    }
}
