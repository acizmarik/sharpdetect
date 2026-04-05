// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;

namespace SharpDetect.Worker.Commands.Run;

public record ProfilerConfigurationArgs(
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? PathWindowsX64 = "%SHARPDETECT_PROFILERS%/win-x64/SharpDetect.Concurrency.Profiler.dll",
    [property: JsonConverter(typeof(NormalizedPathJsonConverter))]
    string? PathLinuxX64 = "%SHARPDETECT_PROFILERS%/linux-x64/SharpDetect.Concurrency.Profiler.so",
    string Clsid = ProfilerConfigurationArgs.ConcurrencyProfilerClsid,
    ProfilerLogLevel LogLevel = ProfilerLogLevel.Warning)
{
    public const string ConcurrencyProfilerClsid = "{b2c60596-b36d-460b-902a-3d91f5878529}";
}