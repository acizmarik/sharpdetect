// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Cli;

public enum ExitCode
{
    /// <summary>Analysis completed successfully with no issues found</summary>
    Success = 0,
    /// <summary>Analysis completed and one or more issues were reported by the plugin</summary>
    IssuesFound = 1,
    /// <summary>Analysis failed due to an internal error</summary>
    AnalysisError = 2,
    /// <summary>Invalid or missing configuration, or plugin failed to load</summary>
    ConfigurationError = 3,
    /// <summary>Analysis was cancelled</summary>
    Cancelled = 4,
}
