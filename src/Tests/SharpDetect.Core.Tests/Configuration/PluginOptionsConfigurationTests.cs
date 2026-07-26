// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using SharpDetect.Core.Configuration;
using Xunit;

namespace SharpDetect.Core.Tests.Configuration;

public class PluginOptionsConfigurationTests
{
    private sealed class TestPluginConfig : IPluginOptionsConfig<TestPluginConfig>
    {
        public static TestPluginConfig Default => new();

        public bool EnableSomething { get; init; } = true;
        public string[] SkipInstrumentationForAssemblies { get; init; } = [];
    }

    private static PluginOptionsConfiguration FromJson(string json)
        => new() { RawJsonConfiguration = JsonSerializer.Deserialize<JsonElement>(json) };

    [Fact]
    public void Parse_NoConfiguration_ReturnsDefaults()
    {
        var configuration = new PluginOptionsConfiguration();
        var parsed = configuration.ParseConfigurationOrDefault<TestPluginConfig>();
        Assert.True(parsed.EnableSomething);
    }

    [Fact]
    public void Parse_KnownOptions_AreHonored()
    {
        var configuration = FromJson("""{ "EnableSomething": false, "SkipInstrumentationForAssemblies": [ "System." ] }""");
        var parsed = configuration.ParseConfigurationOrDefault<TestPluginConfig>();
        Assert.False(parsed.EnableSomething);
        Assert.Equal(["System."], parsed.SkipInstrumentationForAssemblies);
    }

    [Fact]
    public void Parse_MisspelledOption_IsRejected()
    {
        var configuration = FromJson("""{ "SkipInstrumentationForAssemblys": [ "System." ] }""");
        var exception = Assert.Throws<ArgumentException>(configuration.ParseConfigurationOrDefault<TestPluginConfig>);
        Assert.Contains("SkipInstrumentationForAssemblys", exception.Message);
    }

    [Fact]
    public void Parse_WrongOptionType_IsRejected()
    {
        var configuration = FromJson("""{ "EnableSomething": "yes" }""");
        var exception = Assert.Throws<ArgumentException>(configuration.ParseConfigurationOrDefault<TestPluginConfig>);
        Assert.Contains("EnableSomething", exception.Message);
    }

    [Fact]
    public void Parse_MalformedJsonString_IsRejected()
    {
        var configuration = new PluginOptionsConfiguration { RawJsonConfiguration = "{ \"EnableSomething\": " };
        Assert.Throws<ArgumentException>(configuration.ParseConfigurationOrDefault<TestPluginConfig>);
    }
}
