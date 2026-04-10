// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public sealed class RedirectInputOutputConfigurationArgs
{
    public bool SingleConsoleMode { get; }
    public string? StdinFilePath { get; }
    public string? StdoutFilePath { get; }
    public string? StderrFilePath { get; }

    [JsonConstructor]
    public RedirectInputOutputConfigurationArgs(
        bool singleConsoleMode = false,
        string? stdinFilePath = null,
        string? stdoutFilePath = null,
        string? stderrFilePath = null)
    {
        SingleConsoleMode = singleConsoleMode;
        StdinFilePath = stdinFilePath == null ? null : EnvironmentUtils.ExpandEnvironmentVariablesForPath(stdinFilePath);
        StdoutFilePath = stdoutFilePath == null ? null : EnvironmentUtils.ExpandEnvironmentVariablesForPath(stdoutFilePath);
        StderrFilePath = stderrFilePath == null ? null : EnvironmentUtils.ExpandEnvironmentVariablesForPath(stderrFilePath);
    }
}