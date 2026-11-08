// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Cli.Output;
using Xunit;

namespace SharpDetect.Cli.Tests;

public class SharpDetectConsoleFormatterTests
{
    [Theory]
    [InlineData(LogLevel.Warning, "race  write ↔ write  App.Field")]
    [InlineData(LogLevel.Information, "race  write ↔ write  App.Field")]
    [InlineData(LogLevel.Error, "error: race  write ↔ write  App.Field")]
    [InlineData(LogLevel.Debug, "FastTrackPlugin: race  write ↔ write  App.Field")]
    public void Write_OmitsDefaultCategoryPrefix(LogLevel logLevel, string expected)
    {
        var formatter = new SharpDetectConsoleFormatter();
        var entry = new LogEntry<string>(
            logLevel,
            category: "SharpDetect.Plugins.DataRace.FastTrack.FastTrackPlugin",
            new EventId(0),
            state: "race  write ↔ write  App.Field",
            exception: null,
            formatter: (state, _) => state);
        using var writer = new StringWriter();

        formatter.Write(entry, scopeProvider: null, writer);

        Assert.Equal(expected + Environment.NewLine, writer.ToString());
    }
}
