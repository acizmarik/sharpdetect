// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace SharpDetect.Cli;

public record RunCommandArgs(
    RuntimeConfigurationArgs Runtime,
    TargetConfigurationArgs Target,
    AnalysisPluginConfigurationArgs Analysis);

public record RuntimeConfigurationArgs(
    HostConfigurationArgs? Host, 
    ProfilerConfigurationArgs Profiler);

public record HostConfigurationArgs(
    string Path, 
    string? Args,
    KeyValuePair<string, string>[]? AdditionalEnvironmentVariables);

public record TargetConfigurationArgs(
    string Path,
    Architecture Architecture, 
    string? Args, 
    KeyValuePair<string, string>[]? AdditionalEnvironmentVariables,
    RedirectInputOutputConfigurationArgs? RedirectInputOutput);

public record RedirectInputOutputConfigurationArgs(
    bool SingleConsoleMode,
    string? StdinFilePath,
    string? StdoutFilePath,
    string? StderrFilePath);

public record AnalysisPluginConfigurationArgs(
    string Path, 
    string FullTypeName,
    string Configuration,
    bool RenderReport = false,
    LogLevel LogLevel = LogLevel.Warning);

public enum ProfilerLogLevel
{
    Information = 0,
    Warning = -1,
    Error = -2
}

public record ProfilerConfigurationArgs(
    string Clsid,
    ProfilerPathArgs Path, 
    bool CollectFullStackTraces = false,
    ProfilerLogLevel LogLevel = ProfilerLogLevel.Warning);

public record ProfilerPathArgs(
    string WindowsX64,
    string LinuxX64);