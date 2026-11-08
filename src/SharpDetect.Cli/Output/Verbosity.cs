// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Output;

public enum Verbosity
{
    Quiet,
    Normal,
    Detailed,
    Diagnostic
}

internal static class VerbosityExtensions
{
    extension(Verbosity verbosity)
    {
        public LogLevel ToAnalysisLogLevel() => verbosity switch
        {
            Verbosity.Quiet => LogLevel.Error,
            Verbosity.Normal => LogLevel.Warning,
            Verbosity.Detailed => LogLevel.Information,
            Verbosity.Diagnostic => LogLevel.Debug,
            _ => throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, null)
        };

        public ProfilerLogLevel ToProfilerLogLevel() => verbosity switch
        {
            Verbosity.Quiet => ProfilerLogLevel.Error,
            Verbosity.Normal or Verbosity.Detailed => ProfilerLogLevel.Warning,
            Verbosity.Diagnostic => ProfilerLogLevel.Information,
            _ => throw new ArgumentOutOfRangeException(nameof(verbosity), verbosity, null)
        };
    }
    
    public static Verbosity FromAnalysisLogLevel(LogLevel logLevel) => logLevel switch
    {
        >= LogLevel.Error => Verbosity.Quiet,
        LogLevel.Warning => Verbosity.Normal,
        LogLevel.Information => Verbosity.Detailed,
        _ => Verbosity.Diagnostic
    };
}
