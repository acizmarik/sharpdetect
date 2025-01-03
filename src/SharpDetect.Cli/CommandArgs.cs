// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events.Descriptors.Profiler;
using System.Runtime.InteropServices;

namespace SharpDetect.Cli
{
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
        bool RenderReport);

    public record ProfilerConfigurationArgs(
        string Clsid, 
        string Path, 
        bool CollectFullStackTraces, 
        COR_PRF_MONITOR[] EventMask);
}
