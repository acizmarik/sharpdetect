// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class TypeInitializationOrderingTests
{
    private readonly DetectorHarness _harness = new();

    [Fact]
    public void StaticFieldWrittenByTypeInitializer_ThenReadByAnotherThread_IsNotReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var field = _harness.NewStaticField(holder, "Value");
        var cctor = _harness.NewTypeInitializer(holder);

        // Act
        _harness.Write(initializer, field, instance: null);
        _harness.TypeInitialized(initializer, cctor);

        // Assert
        Assert.False(_harness.ReadIsRace(reader, field, instance: null));
    }

    [Fact]
    public void StaticFieldWrittenByTypeInitializer_ThenWrittenByAnotherThread_IsNotReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var writer = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var field = _harness.NewStaticField(holder, "Value");
        var cctor = _harness.NewTypeInitializer(holder);

        // Act
        _harness.Write(initializer, field, instance: null);
        _harness.TypeInitialized(initializer, cctor);

        // Assert
        Assert.False(_harness.WriteIsRace(writer, field, instance: null));
    }

    [Fact]
    public void StaticFieldWrite_WithoutTypeInitializerCompleting_IsReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var field = _harness.NewStaticField(holder, "Value");
        _ = _harness.NewTypeInitializer(holder);

        // Act
        _harness.Write(initializer, field, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(reader, field, instance: null));
    }

    [Fact]
    public void NestedObjectsPublishedThroughReadonlyStaticField_AreOrderedForEveryReader()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var firstReader = _harness.NewThread();
        var secondReader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var root = _harness.NewStaticField(holder, "Root", isReadOnly: true);
        var cctor = _harness.NewTypeInitializer(holder);
        var child = _harness.NewInstanceField("Child");
        var leaf = _harness.NewInstanceField("Leaf");
        var rootObject = _harness.NewObject();
        var childObject = _harness.NewObject();

        // Act
        _harness.Write(initializer, leaf, childObject);
        _harness.Write(initializer, child, rootObject);
        _harness.Write(initializer, root, instance: null);
        _harness.TypeInitialized(initializer, cctor);
        _harness.Read(firstReader, root, instance: null);
        _harness.Read(secondReader, root, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(firstReader, child, rootObject));
        Assert.False(_harness.ReadIsRace(firstReader, leaf, childObject));
        Assert.False(_harness.ReadIsRace(secondReader, child, rootObject));
        Assert.False(_harness.ReadIsRace(secondReader, leaf, childObject));
    }

    [Fact]
    public void ThreadAccessingTheTypeWhileItIsBeingInitialized_IsOrderedOnceTheInitializerCompletes()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var waiter = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var root = _harness.NewStaticField(holder, "Root", isReadOnly: true);
        var cctor = _harness.NewTypeInitializer(holder);
        var child = _harness.NewInstanceField("Child");
        var rootObject = _harness.NewObject();

        // Act
        _harness.Read(waiter, root, instance: null);
        _harness.Write(initializer, child, rootObject);
        _harness.Write(initializer, root, instance: null);
        _harness.TypeInitialized(initializer, cctor);

        // Assert
        Assert.False(_harness.ReadIsRace(waiter, child, rootObject));
    }

    [Fact]
    public void ThreadThatWaitedForAnInitializer_IsNotOrderedAfterLaterWritesOfTheInitializer()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var waiter = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var root = _harness.NewStaticField(holder, "Root", isReadOnly: true);
        var cctor = _harness.NewTypeInitializer(holder);
        var child = _harness.NewInstanceField("Child");
        var rootObject = _harness.NewObject();

        // Act
        _harness.Read(waiter, root, instance: null);
        _harness.TypeInitialized(initializer, cctor);
        _harness.Write(initializer, child, rootObject);

        // Assert
        Assert.True(_harness.ReadIsRace(waiter, child, rootObject));
    }

    [Fact]
    public void NestedObjectReadWithoutAccessingTheInitializedType_IsReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var root = _harness.NewStaticField(holder, "Root", isReadOnly: true);
        var cctor = _harness.NewTypeInitializer(holder);
        var child = _harness.NewInstanceField("Child");
        var rootObject = _harness.NewObject();

        // Act
        _harness.Write(initializer, child, rootObject);
        _harness.Write(initializer, root, instance: null);
        _harness.TypeInitialized(initializer, cctor);

        // Assert
        Assert.True(_harness.ReadIsRace(reader, child, rootObject));
    }

    [Fact]
    public void TypeInitializerOfUnrelatedType_AddsNoOrdering_AndIsReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var other = _harness.NewType("Other");
        var field = _harness.NewStaticField(holder, "Value");
        _ = _harness.NewTypeInitializer(holder);
        var otherCctor = _harness.NewTypeInitializer(other);

        // Act
        _harness.Write(initializer, field, instance: null);
        _harness.TypeInitialized(initializer, otherCctor);

        // Assert
        Assert.True(_harness.ReadIsRace(reader, field, instance: null));
    }

    [Fact]
    public void WriteAfterTypeInitializerCompleted_IsReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var field = _harness.NewStaticField(holder, "Value");
        var cctor = _harness.NewTypeInitializer(holder);

        // Act
        _harness.TypeInitializerEntered(initializer, cctor);
        _harness.Write(initializer, field, instance: null);
        _harness.TypeInitialized(initializer, cctor);
        _harness.Write(initializer, field, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(reader, field, instance: null));
    }

    [Fact]
    public void ConcurrentInitializersOfOneGenericDefinition_WritingTheSharedFieldMetadata_AreNotReported()
    {
        // Arrange
        var firstInitializer = _harness.NewThread();
        var secondInitializer = _harness.NewThread();
        var holder = _harness.NewGenericType("Holder`1");
        var field = _harness.NewStaticField(holder, "Values");
        var cctor = _harness.NewTypeInitializer(holder);

        // Act
        _harness.TypeInitializerEntered(firstInitializer, cctor);
        _harness.TypeInitializerEntered(secondInitializer, cctor);
        _harness.Write(firstInitializer, field, instance: null);

        // Assert
        Assert.False(_harness.WriteIsRace(secondInitializer, field, instance: null));
    }

    [Fact]
    public void ThreadWaitingForOneGenericInstantiation_AfterAnotherWasInitialized_IsOrderedOnceItsInitializerCompletes()
    {
        // Arrange
        var first = _harness.NewThread();
        var initializer = _harness.NewThread();
        var waiter = _harness.NewThread();
        var holder = _harness.NewGenericType("Holder`1");
        var root = _harness.NewStaticField(holder, "Root", isReadOnly: true);
        var cctor = _harness.NewTypeInitializer(holder);
        var child = _harness.NewInstanceField("Child");
        var rootObject = _harness.NewObject();

        // Act
        _harness.TypeInitializerEntered(first, cctor);
        _harness.TypeInitialized(first, cctor);
        _harness.TypeInitializerEntered(initializer, cctor);
        _harness.Read(waiter, root, instance: null);
        _harness.Write(initializer, child, rootObject);
        _harness.TypeInitialized(initializer, cctor);

        // Assert
        Assert.False(_harness.ReadIsRace(waiter, child, rootObject));
    }

    [Fact]
    public void ThreadThatAccessedAnInitializedGenericInstantiation_IsNotOrderedAfterALaterInitializerOfAnother()
    {
        // Arrange
        var first = _harness.NewThread();
        var reader = _harness.NewThread();
        var initializer = _harness.NewThread();
        var holder = _harness.NewGenericType("Holder`1");
        var items = _harness.NewStaticField(holder, "Items", isReadOnly: true);
        var cctor = _harness.NewTypeInitializer(holder);
        var shared = _harness.NewInstanceField("Shared");
        var sharedObject = _harness.NewObject();

        // Act
        _harness.TypeInitializerEntered(first, cctor);
        _harness.TypeInitialized(first, cctor);
        _harness.Read(reader, items, instance: null);
        _harness.Write(initializer, shared, sharedObject);
        _harness.TypeInitializerEntered(initializer, cctor);
        _harness.TypeInitialized(initializer, cctor);

        // Assert
        Assert.True(_harness.ReadIsRace(reader, shared, sharedObject));
    }

    [Fact]
    public void InitializerWriteToStaticFieldOfAnotherType_IsReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var writer = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var other = _harness.NewType("Other");
        var otherField = _harness.NewStaticField(other, "Value");
        var cctor = _harness.NewTypeInitializer(holder);

        // Act
        _harness.TypeInitializerEntered(initializer, cctor);
        _harness.Write(initializer, otherField, instance: null);
        _harness.TypeInitialized(initializer, cctor);

        // Assert
        Assert.True(_harness.WriteIsRace(writer, otherField, instance: null));
    }

    [Fact]
    public void StaticWriteAfterInitializerFailed_IsReported()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var writer = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var field = _harness.NewStaticField(holder, "Value");
        var cctor = _harness.NewTypeInitializer(holder);

        // Act
        _harness.TypeInitializerEntered(initializer, cctor);
        _harness.TypeInitializerFailed(initializer, cctor);
        _harness.Write(initializer, field, instance: null);

        // Assert
        Assert.True(_harness.WriteIsRace(writer, field, instance: null));
    }

    [Fact]
    public void FailedInitializer_ReleasesNothing()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var root = _harness.NewStaticField(holder, "Root", isReadOnly: true);
        var cctor = _harness.NewTypeInitializer(holder);
        var child = _harness.NewInstanceField("Child");
        var rootObject = _harness.NewObject();

        // Act
        _harness.TypeInitializerEntered(initializer, cctor);
        _harness.Write(initializer, child, rootObject);
        _harness.TypeInitializerFailed(initializer, cctor);
        _harness.Read(reader, root, instance: null);

        // Assert
        Assert.True(_harness.ReadIsRace(reader, child, rootObject));
    }

    [Fact]
    public void VolatileStaticFieldRead_OrdersAfterTypeInitializer()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var flag = _harness.NewStaticField(holder, "Flag");
        var cctor = _harness.NewTypeInitializer(holder);
        var payload = _harness.NewInstanceField("Payload");
        var payloadObject = _harness.NewObject();

        // Act
        _harness.Write(initializer, payload, payloadObject);
        _harness.TypeInitialized(initializer, cctor);
        _harness.VolatileRead(reader, flag, instance: null);

        // Assert
        Assert.False(_harness.ReadIsRace(reader, payload, payloadObject));
    }

    [Fact]
    public void InstanceFieldAccess_DoesNotAcquireTypeInitialization()
    {
        // Arrange
        var initializer = _harness.NewThread();
        var reader = _harness.NewThread();
        var holder = _harness.NewType("Holder");
        var cctor = _harness.NewTypeInitializer(holder);
        var instanceField = _harness.NewInstanceField(holder, "InstanceValue");
        var guarded = _harness.NewInstanceField("Guarded");
        var instance = _harness.NewObject();
        var guardedObject = _harness.NewObject();

        // Act
        _harness.Write(initializer, guarded, guardedObject);
        _harness.TypeInitialized(initializer, cctor);
        _harness.Read(reader, instanceField, instance);

        // Assert
        Assert.True(_harness.ReadIsRace(reader, guarded, guardedObject));
    }
}
