// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands.Run;

public sealed class TestTargetConfigurationArgs
{
    public const TestRunner DefaultRunner = TestRunner.Mtp;

    public TestRunner Runner { get; }
    public string? Filter { get; }
    [JsonConverter(typeof(CommandLineArgumentsJsonConverter))]
    public ImmutableArray<string> AdditionalRunnerArgs { get; }

    [JsonConstructor]
    public TestTargetConfigurationArgs(
        TestRunner runner = DefaultRunner,
        string? filter = null,
        ImmutableArray<string> additionalRunnerArgs = default)
    {
        Runner = runner;
        Filter = filter;
        AdditionalRunnerArgs = additionalRunnerArgs.IsDefault ? [] : additionalRunnerArgs;
    }
}
