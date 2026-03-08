// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;
using SharpDetect.Worker.Commands;
using Xunit;

namespace SharpDetect.Worker.Tests.Commands;

public class NormalizedPathJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new NormalizedPathJsonConverter() }
    };

    [Fact]
    public void NormalizedPathJsonConverter_Read_ForwardSlashes_Unchanged()
    {
        // Arrange
        var json = """{"Value": "C:/Users/test/file.dll"}""";

        // Act
        var result = JsonSerializer.Deserialize<Wrapper>(json, Options);

        // Assert
        Assert.Equal("C:/Users/test/file.dll", result!.Value);
    }

    [Fact]
    public void NormalizedPathJsonConverter_Read_BackSlashes_NormalizedToForwardSlashes()
    {
        // Arrange
        var json = """{"Value": "C:\\Users\\test\\file.dll"}""";

        // Act
        var result = JsonSerializer.Deserialize<Wrapper>(json, Options);

        // Assert
        Assert.Equal("C:/Users/test/file.dll", result!.Value);
    }

    [Fact]
    public void NormalizedPathJsonConverter_Read_MixedSlashes_NormalizedToForwardSlashes()
    {
        // Arrange
        var json = """{"Value": "C:\\Users/test\\file.dll"}""";

        // Act
        var result = JsonSerializer.Deserialize<Wrapper>(json, Options);

        // Assert
        Assert.Equal("C:/Users/test/file.dll", result!.Value);
    }

    [Fact]
    public void NormalizedPathJsonConverter_Read_NullValue_ReturnsNull()
    {
        // Arrange
        var json = """{"Value": null}""";

        // Act
        var result = JsonSerializer.Deserialize<NullableWrapper>(json, Options);

        // Assert
        Assert.Null(result!.Value);
    }

    [Fact]
    public void NormalizedPathJsonConverter_Write_DoesNotModifyValue()
    {
        // Arrange
        var wrapper = new Wrapper("C:/Users/test/file.dll");

        // Act
        var json = JsonSerializer.Serialize(wrapper, Options);

        // Assert
        Assert.Contains("C:/Users/test/file.dll", json);
    }

    private record Wrapper(
        [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
        string Value);

    private record NullableWrapper(
        [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
        string? Value);
}

