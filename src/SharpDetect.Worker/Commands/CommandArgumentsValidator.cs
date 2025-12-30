// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Worker.Commands;

public static class CommandArgumentsValidator
{
    public static void ValidateRunCommandArguments(RunCommandArgs arguments)
    {
        ThrowOnInvalidTargetConfiguration(arguments.Target);
        ThrowOnInvalidRuntimeConfiguration(arguments.Runtime);
        ThrowOnInvalidAnalysisConfiguration(arguments.Analysis);
    }
    
    private static void ThrowOnInvalidTargetConfiguration(TargetConfigurationArgs configArgs)
    {
        var target = configArgs.Path;
        var architecture = configArgs.Architecture;

        if (!File.Exists(target))
            throw new ArgumentException($"Could not find target assembly: \"{target}\".");
        if (architecture != Architecture.X64)
            throw new ArgumentException($"Unsupported architecture: \"{architecture}\".");
    }

    private static void ThrowOnInvalidRuntimeConfiguration(RuntimeConfigurationArgs configArgs)
    {
        var profilerClsid = configArgs.Profiler.Clsid;
        var profilerPaths = configArgs.Profiler.Path;

        if (!Guid.TryParse(profilerClsid, out var parsedClsid) || parsedClsid == Guid.Empty)
            throw new ArgumentException($"Invalid profiler CLSID: \"{profilerClsid}\".");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            if (string.IsNullOrEmpty(profilerPaths.WindowsX64))
                throw new ArgumentException($"No profiler path specified for Windows x64 platform.");
            if (!File.Exists(profilerPaths.WindowsX64))
                throw new ArgumentException($"Could not find Windows x64 profiler library: \"{profilerPaths.WindowsX64}\".");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            if (string.IsNullOrEmpty(profilerPaths.LinuxX64))
                throw new ArgumentException($"No profiler path specified for Linux x64 platform.");
            if (!File.Exists(profilerPaths.LinuxX64))
                throw new ArgumentException($"Could not find Linux x64 profiler library: \"{profilerPaths.LinuxX64}\".");
        }

        if (configArgs.Host is { } host)
        {
            var hostPath = host.Path;

            if (string.IsNullOrWhiteSpace(hostPath))
                throw new ArgumentException($"Invalid host path: \"{hostPath}\".");
            if (!File.Exists(hostPath))
                throw new ArgumentException($"Could not find host executable: \"{hostPath}\".");
        }
    }
    
    private static void ThrowOnInvalidAnalysisConfiguration(AnalysisPluginConfigurationArgs configArgs)
    {
        var pluginTypeName = configArgs.FullTypeName;
        var pluginPath = configArgs.Path;

        if (string.IsNullOrWhiteSpace(pluginPath))
            throw new ArgumentException($"Invalid plugin path: \"{pluginPath}\".");
        if (!File.Exists(pluginPath))
            throw new ArgumentException($"Could not find plugin assembly: \"{pluginPath}\".");
        if (string.IsNullOrWhiteSpace(pluginTypeName))
            throw new ArgumentException($"Invalid plugin type fullname: \"{pluginTypeName}\".");
    }
}