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
    
    [JsonIgnore] public string PathWindowsX64 { get; }
    [JsonIgnore] public string PathLinuxX64 { get; }
    [JsonIgnore] public string Clsid { get; }
    public ProfilerLogLevel LogLevel { get; }

    [JsonConstructor]
    public ProfilerConfigurationArgs(
        string pathWindowsX64 = DefaultPathWindowsX64,
        string pathLinuxX64 = DefaultPathLinuxX64,
        string clsid = DefaultClsid,
        ProfilerLogLevel logLevel = DefaultLogLevel)
    {
        PathWindowsX64 = EnvironmentUtils.ExpandEnvironmentVariablesForPath(pathWindowsX64);
        PathLinuxX64 = EnvironmentUtils.ExpandEnvironmentVariablesForPath(pathLinuxX64);
        Clsid = clsid;
        LogLevel = logLevel;
    }
    
    [JsonPropertyName(nameof(PathWindowsX64))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? PathWindowsX64Serialized => PathWindowsX64 == DefaultPathWindowsX64 ? null : PathWindowsX64;
    
    [JsonPropertyName(nameof(PathLinuxX64))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? PathLinuxX64Serialized => PathLinuxX64 == DefaultPathLinuxX64 ? null : PathLinuxX64;
    
    [JsonPropertyName(nameof(Clsid))]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    private string? ClsidSerialized => Clsid == DefaultClsid ? null : Clsid;
}