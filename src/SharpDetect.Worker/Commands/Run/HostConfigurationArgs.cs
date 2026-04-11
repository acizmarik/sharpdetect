// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public sealed class HostConfigurationArgs
{
    public const string DefaultHost = "dotnet";

    public string Path { get; }
    public string? Args { get; }
    public KeyValuePair<string, string>[]? AdditionalEnvironmentVariables { get; }

    [JsonConstructor]
    public HostConfigurationArgs(
        string path = DefaultHost,
        string? args = null,
        KeyValuePair<string, string>[]? additionalEnvironmentVariables = null)
    {
        Guard.IsNotNullOrWhiteSpace(path);
        Path = EnvironmentUtils.ExpandEnvironmentVariablesForPath(path);
        Args = args;
        AdditionalEnvironmentVariables = additionalEnvironmentVariables;
    }
}