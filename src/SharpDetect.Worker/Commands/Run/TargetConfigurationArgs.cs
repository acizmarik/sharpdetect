// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands.Run;

public record TargetConfigurationArgs(
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string Path,
    string? Args, 
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? WorkingDirectory, 
    KeyValuePair<string, string>[]? AdditionalEnvironmentVariables,
    RedirectInputOutputConfigurationArgs? RedirectInputOutput);