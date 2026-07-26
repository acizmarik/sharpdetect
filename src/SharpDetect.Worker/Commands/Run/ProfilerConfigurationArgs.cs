// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json.Serialization;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Worker.Commands.Run;

public sealed class ProfilerConfigurationArgs
{
    public const string DefaultPathWindowsX64 = "%SHARPDETECT_PROFILERS%/win-x64/SharpDetect.Concurrency.Profiler.dll";
    public const string DefaultPathLinuxX64 = "%SHARPDETECT_PROFILERS%/linux-x64/SharpDetect.Concurrency.Profiler.so";
    public const string DefaultClsid = "{b2c60596-b36d-460b-902a-3d91f5878529}";
    public const ProfilerLogLevel DefaultLogLevel = ProfilerLogLevel.Warning;
    [JsonPropertyName(nameof(PathWindowsX64))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PathWindowsX64Raw { get; }

    [JsonPropertyName(nameof(PathLinuxX64))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PathLinuxX64Raw { get; }

    [JsonPropertyName(nameof(Clsid))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClsidRaw { get; }

    [JsonIgnore] public string PathWindowsX64 { get; }
    [JsonIgnore] public string PathLinuxX64 { get; }
    [JsonIgnore] public string Clsid { get; }
    public ProfilerLogLevel LogLevel { get; }

    [JsonConstructor]
    public ProfilerConfigurationArgs(
        string? pathWindowsX64Raw = null,
        string? pathLinuxX64Raw = null,
        string? clsidRaw = null,
        ProfilerLogLevel logLevel = DefaultLogLevel)
    {
        PathWindowsX64Raw = pathWindowsX64Raw;
        PathLinuxX64Raw = pathLinuxX64Raw;
        ClsidRaw = clsidRaw;
        PathWindowsX64 = EnvironmentUtils.ExpandEnvironmentVariablesForPath(pathWindowsX64Raw ?? DefaultPathWindowsX64);
        PathLinuxX64 = EnvironmentUtils.ExpandEnvironmentVariablesForPath(pathLinuxX64Raw ?? DefaultPathLinuxX64);
        Clsid = clsidRaw ?? DefaultClsid;
        LogLevel = logLevel;
    }
}
