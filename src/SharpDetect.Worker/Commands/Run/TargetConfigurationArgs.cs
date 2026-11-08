// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public sealed class TargetConfigurationArgs
{
    public const TargetKind DefaultKind = TargetKind.Executable;

    public string Path { get; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TargetKind Kind { get; }
    [JsonConverter(typeof(CommandLineArgumentsJsonConverter))]
    public ImmutableArray<string> Args { get; }
    public string? WorkingDirectory { get; }
    public KeyValuePair<string, string>[]? AdditionalEnvironmentVariables { get; }
    public RedirectInputOutputConfigurationArgs? RedirectInputOutput { get; }
    public TestTargetConfigurationArgs? Test { get; }

    [JsonConstructor]
    public TargetConfigurationArgs(
        string path,
        ImmutableArray<string> args = default,
        string? workingDirectory = null,
        KeyValuePair<string, string>[]? additionalEnvironmentVariables = null,
        RedirectInputOutputConfigurationArgs? redirectInputOutput = null,
        TargetKind kind = DefaultKind,
        TestTargetConfigurationArgs? test = null)
    {
        Guard.IsNotNullOrWhiteSpace(path);
        Path = EnvironmentUtils.ExpandEnvironmentVariablesForPath(path);
        Kind = kind;
        Args = args.IsDefault ? [] : args;
        WorkingDirectory = workingDirectory == null ? null : EnvironmentUtils.ExpandEnvironmentVariablesForPath(workingDirectory);
        AdditionalEnvironmentVariables = additionalEnvironmentVariables;
        RedirectInputOutput = redirectInputOutput;
        Test = test;
    }
}
