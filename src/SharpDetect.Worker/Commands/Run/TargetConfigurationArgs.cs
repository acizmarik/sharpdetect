// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public sealed class TargetConfigurationArgs
{
    public string Path { get; }
    public string? Args { get; }
    public string? WorkingDirectory { get; }
    public KeyValuePair<string, string>[]? AdditionalEnvironmentVariables { get; }
    public RedirectInputOutputConfigurationArgs? RedirectInputOutput { get; }

    [JsonConstructor]
    public TargetConfigurationArgs(
        string path,
        string? args = null,
        string? workingDirectory = null,
        KeyValuePair<string, string>[]? additionalEnvironmentVariables = null,
        RedirectInputOutputConfigurationArgs? redirectInputOutput = null)
    {
        Guard.IsNotNullOrWhiteSpace(path);
        Path = EnvironmentUtils.ExpandEnvironmentVariablesForPath(path);
        Args = args;
        WorkingDirectory = workingDirectory == null ? null : EnvironmentUtils.ExpandEnvironmentVariablesForPath(workingDirectory);
        AdditionalEnvironmentVariables = additionalEnvironmentVariables;
        RedirectInputOutput = redirectInputOutput;
    }
}