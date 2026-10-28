// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class FieldIdentityTests
{
    private const nuint IntInstantiation = 0x1000;
    private const nuint LongInstantiation = 0x2000;

    private readonly DetectorHarness _harness = new();

    [Fact]
    public void InstanceField_AccessedThroughDifferentTokens_IsReported()
    {
        // Arrange
        var reader = _harness.NewThread();
        var writer = _harness.NewThread();
        var instance = _harness.NewObject();
        var definition = _harness.NewInstanceField("Value");
        var reference = _harness.NewFieldReference(definition);

        // Act
        _harness.Read(reader, definition, instance);
        var race = _harness.Write(writer, reference, instance);

        // Assert
        Assert.NotNull(race);
    }

    [Fact]
    public void InstanceField_PublishedThroughDifferentTokens_IsNotReported()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var instance = _harness.NewObject();
        var data = _harness.NewStaticField("Data");
        var definition = _harness.NewInstanceField("Flag");
        var reference = _harness.NewFieldReference(definition);

        // Act
        _harness.Write(publisher, data, instance: null);
        _harness.VolatileWrite(publisher, definition, instance);
        _harness.VolatileRead(observer, reference, instance);
        var race = _harness.Read(observer, data, instance: null);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void StaticField_AccessedThroughDifferentTokens_IsReported()
    {
        // Arrange
        var reader = _harness.NewThread();
        var writer = _harness.NewThread();
        var definition = _harness.NewStaticField("Value");
        var reference = _harness.NewFieldReference(definition);

        // Act
        _harness.Read(reader, definition, instance: null);
        var race = _harness.Write(writer, reference, instance: null);

        // Assert
        Assert.NotNull(race);
    }

    [Fact]
    public void StaticField_PublishedThroughDifferentTokens_IsNotReported()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var data = _harness.NewStaticField("Data");
        var definition = _harness.NewStaticField("Flag");
        var reference = _harness.NewFieldReference(definition);

        // Act
        _harness.Write(publisher, data, instance: null);
        _harness.VolatileWrite(publisher, definition, instance: null);
        _harness.VolatileRead(observer, reference, instance: null);
        var race = _harness.Read(observer, data, instance: null);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void StaticField_OfSameInstantiation_PublishedThroughDifferentTokens_IsNotReported()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var data = _harness.NewStaticField("Data");
        var type = _harness.NewGenericType("Flags`1");
        var definition = _harness.NewStaticField(type, "Flag");
        var reference = _harness.NewFieldReference(definition);

        // Act
        _harness.Write(publisher, data, instance: null);
        _harness.VolatileWrite(publisher, definition, instance: null, IntInstantiation);
        _harness.VolatileRead(observer, reference, instance: null, IntInstantiation);
        var race = _harness.Read(observer, data, instance: null);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void StaticField_OfOtherInstantiation_DoesNotPublish()
    {
        // Arrange
        var publisher = _harness.NewThread();
        var observer = _harness.NewThread();
        var data = _harness.NewStaticField("Data");
        var type = _harness.NewGenericType("Flags`1");
        var definition = _harness.NewStaticField(type, "Flag");
        var reference = _harness.NewFieldReference(definition);

        // Act
        _harness.Write(publisher, data, instance: null);
        _harness.VolatileWrite(publisher, reference, instance: null, LongInstantiation);
        _harness.VolatileRead(observer, reference, instance: null, IntInstantiation);
        var race = _harness.Read(observer, data, instance: null);

        // Assert
        Assert.NotNull(race);
    }

    [Fact]
    public void StaticField_OfOtherInstantiation_IsNotReported()
    {
        // Arrange
        var first = _harness.NewThread();
        var second = _harness.NewThread();
        var type = _harness.NewGenericType("Holder`1");
        var definition = _harness.NewStaticField(type, "Value");

        // Act
        _harness.Write(first, definition, instance: null, IntInstantiation);
        var race = _harness.Write(second, definition, instance: null, LongInstantiation);

        // Assert
        Assert.Null(race);
    }

    [Fact]
    public void StaticField_OfNonGenericType_IgnoresDeclaringTypeHandle()
    {
        // Arrange
        var first = _harness.NewThread();
        var second = _harness.NewThread();
        var definition = _harness.NewStaticField("Value");

        // Act
        _harness.Write(first, definition, instance: null, IntInstantiation);
        var race = _harness.Write(second, definition, instance: null, LongInstantiation);

        // Assert
        Assert.NotNull(race);
    }

    [Fact]
    public void InstanceField_AccessedThroughDifferentTokens_OnDifferentObjects_IsNotReported()
    {
        // Arrange
        var reader = _harness.NewThread();
        var writer = _harness.NewThread();
        var definition = _harness.NewInstanceField("Value");
        var reference = _harness.NewFieldReference(definition);

        // Act
        _harness.Read(reader, definition, _harness.NewObject());
        var race = _harness.Write(writer, reference, _harness.NewObject());

        // Assert
        Assert.Null(race);
    }
}
