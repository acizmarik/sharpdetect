// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands.Run;

public record RedirectInputOutputConfigurationArgs(
    bool SingleConsoleMode,
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? StdinFilePath,
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? StdoutFilePath,
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? StderrFilePath);