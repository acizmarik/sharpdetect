// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class FieldIdentityTests
{
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
