// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands.Run;

public record HostConfigurationArgs(
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string Path, 
    string? Args,
    KeyValuePair<string, string>[]? AdditionalEnvironmentVariables);