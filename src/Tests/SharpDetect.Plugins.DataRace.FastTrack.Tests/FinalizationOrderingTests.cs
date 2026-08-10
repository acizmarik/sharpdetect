// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class FinalizationOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void FinalizerWrite_AfterConstructorWriteOnAnotherThread_IsNotReported()
    {
        // Arrange
        var owner = _harness.NewThread();
        var finalizer = _harness.NewThread();
        var instance = _harness.NewObject();
        var field = _harness.NewInstanceField("Handle");

        // Act
        _harness.Write(owner, field, instance);
        _harness.QueueForFinalization(instance);

        // Assert
        Assert.False(_harness.WriteIsRace(finalizer, field, instance));
    }

    [Fact]
    public void FinalizerRead_AfterWriteOnAnotherThread_IsNotReported()
    {
        // Arrange
        var owner = _harness.NewThread();
        var finalizer = _harness.NewThread();
        var instance = _harness.NewObject();
        var field = _harness.NewInstanceField("Handle");

        // Act
        _harness.Write(owner, field, instance);
        _harness.QueueForFinalization(instance);

        // Assert
        Assert.False(_harness.ReadIsRace(finalizer, field, instance));
    }

    [Fact]
    public void FinalizerWrite_DoesNotLeaveAnEpochThatSuppressesLaterReaders()
    {
        // Arrange
        var owner = _harness.NewThread();
        var finalizer = _harness.NewThread();
        var reader = _harness.NewThread();
        var instance = _harness.NewObject();
        var field = _harness.NewInstanceField("Handle");

        // Act
        _harness.Write(owner, field, instance);
        _harness.QueueForFinalization(instance);
        _harness.Write(finalizer, field, instance);

        // Assert
        Assert.False(_harness.ReadIsRace(reader, field, instance));
    }

    [Fact]
    public void WriteBeforeTheObjectIsQueued_IsReported()
    {
        // Arrange
        var owner = _harness.NewThread();
        var other = _harness.NewThread();
        var instance = _harness.NewObject();
        var field = _harness.NewInstanceField("Handle");

        // Act
        _harness.Write(owner, field, instance);

        // Assert
        Assert.True(_harness.WriteIsRace(other, field, instance));
    }

    [Fact]
    public void QueueingOneObject_DoesNotExemptAnother()
    {
        // Arrange
        var owner = _harness.NewThread();
        var finalizer = _harness.NewThread();
        var queued = _harness.NewObject();
        var live = _harness.NewObject();
        var field = _harness.NewInstanceField("Handle");

        // Act
        _harness.Write(owner, field, live);
        _harness.QueueForFinalization(queued);

        // Assert
        Assert.True(_harness.WriteIsRace(finalizer, field, live));
    }

    [Fact]
    public void QueueingAnObject_DoesNotExemptStaticFields()
    {
        // Arrange
        var owner = _harness.NewThread();
        var finalizer = _harness.NewThread();
        var instance = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act
        _harness.Write(owner, field, instance: null);
        _harness.QueueForFinalization(instance);

        // Assert
        Assert.True(_harness.WriteIsRace(finalizer, field, instance: null));
    }

    [Fact]
    public void CollectedObject_DropsItsFinalizationExemption()
    {
        // Arrange
        var owner = _harness.NewThread();
        var finalizer = _harness.NewThread();
        var instance = DetectorHarness.ObjectWithId(700);
        var field = _harness.NewInstanceField("Handle");

        // Act
        _harness.QueueForFinalization(instance);
        _harness.CollectObjects(instance);
        _harness.Write(owner, field, DetectorHarness.ObjectWithId(700));

        // Assert
        Assert.True(_harness.WriteIsRace(finalizer, field, DetectorHarness.ObjectWithId(700)));
    }
}
