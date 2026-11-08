// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace SharpDetect.Cli.Output;

internal sealed class SharpDetectConsoleFormatter() : ConsoleFormatter(FormatterName)
{
    public const string FormatterName = "sharpdetect";

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (string.IsNullOrEmpty(message) && logEntry.Exception is null)
            return;

        textWriter.Write(GetPrefix(logEntry.LogLevel, logEntry.Category));
        textWriter.WriteLine(message);
        if (logEntry.Exception is { } exception)
            textWriter.WriteLine(exception.ToString());
    }

    private static string GetPrefix(LogLevel logLevel, string category) => logLevel switch
    {
        LogLevel.Trace or LogLevel.Debug => $"{GetShortCategory(category)}: ",
        LogLevel.Error or LogLevel.Critical => "error: ",
        _ => string.Empty
    };

    private static string GetShortCategory(string category)
    {
        var lastDot = category.LastIndexOf('.');
        return lastDot >= 0 ? category[(lastDot + 1)..] : category;
    }
}

internal static class SharpDetectConsoleLoggingExtensions
{
    public static ILoggingBuilder AddSharpDetectConsole(this ILoggingBuilder builder)
    {
        builder.AddConsole(options =>
        {
            options.FormatterName = SharpDetectConsoleFormatter.FormatterName;
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        builder.AddConsoleFormatter<SharpDetectConsoleFormatter, ConsoleFormatterOptions>();
        return builder;
    }
}
