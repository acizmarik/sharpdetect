// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class CommandLineArgumentsJsonConverterTests
{
    private static RunCommandArgs Deserialize(string targetArgsJson, string runnerArgsJson = "null")
        => CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(
            $$"""
            {
              "Target": {
                "Path": "app.dll",
                "Args": {{targetArgsJson}},
                "Kind": "TestAssembly",
                "Test": { "AdditionalRunnerArgs": {{runnerArgsJson}} }
              },
              "Runtime": { "Host": { "Args": {{targetArgsJson}} } },
              "Analysis": { "PluginName": "FastTrack" }
            }
            """);

    [Fact]
    public void Array_IsReadElementByElement()
    {
        var args = Deserialize("""[ "race", "two words", "" ]""");

        Assert.Equal(new[] { "race", "two words", "" }, args.Target.Args);
        Assert.Equal(new[] { "race", "two words", "" }, args.Runtime.Host!.Args);
    }

    [Fact]
    public void String_IsSplitOnWhitespace()
    {
        var args = Deserialize("\"race  x\\ty\"", "\"--report-trx --results-directory out\"");

        Assert.Equal(new[] { "race", "x", "y" }, args.Target.Args);
        Assert.Equal(new[] { "--report-trx", "--results-directory", "out" }, args.Target.Test!.AdditionalRunnerArgs);
    }

    [Theory]
    [InlineData("a \"b c\" d", new[] { "a", "b c", "d" })]
    [InlineData("a 'b \"c\"' d", new[] { "a", "b \"c\"", "d" })]
    [InlineData("pre\"fix suf\"fix", new[] { "prefix suffix" })]
    [InlineData("a \\\"b c", new[] { "a", "\"b", "c" })]
    [InlineData("a\\ b", new[] { "a b" })]
    [InlineData("C:\\\\dir\\\\app.dll", new[] { "C:\\dir\\app.dll" })]
    [InlineData("\"\" ''", new[] { "", "" })]
    [InlineData("", new string[0])]
    [InlineData("   ", new string[0])]
    public void Split_GroupsQuotesAndHonoursEscapes(string commandLine, string[] expected)
    {
        Assert.Equal(expected, CommandLineArgumentsJsonConverter.Split(commandLine));
    }

    [Theory]
    [InlineData("a \"b")]
    [InlineData("a 'b")]
    [InlineData("a\\")]
    public void Split_MalformedCommandLine_Throws(string commandLine)
    {
        Assert.Throws<JsonException>(() => CommandLineArgumentsJsonConverter.Split(commandLine));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    public void NullOrEmpty_IsEmpty(string json)
    {
        var args = Deserialize(json);

        Assert.Empty(args.Target.Args);
        Assert.Empty(args.Target.Test!.AdditionalRunnerArgs);
    }

    [Fact]
    public void Missing_IsEmpty()
    {
        var args = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(
            """{ "Target": { "Path": "app.dll" }, "Analysis": { "PluginName": "FastTrack" } }""");

        Assert.Empty(args.Target.Args);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("[ \"a\", 1 ]")]
    [InlineData("{ }")]
    public void NonStringValues_AreRejectedWithPath(string json)
    {
        var exception = Assert.Throws<ArgumentException>(() => Deserialize(json));

        Assert.Contains("$.Target.Args", exception.Message);
    }

    [Fact]
    public void Write_ProducesArray()
    {
        var target = new TargetConfigurationArgs(path: "app.dll", args: ["a b", "c"]);

        var json = JsonSerializer.Serialize(target);

        Assert.Contains("\"Args\":[\"a b\",\"c\"]", json);
    }
}
