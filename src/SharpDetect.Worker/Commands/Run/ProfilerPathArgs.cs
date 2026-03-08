// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands.Run;

public record ProfilerPathArgs(
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string WindowsX64,
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string LinuxX64);