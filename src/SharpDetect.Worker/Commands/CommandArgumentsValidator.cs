// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.PortableExecutable;
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

        if (!File.Exists(target))
            throw new ArgumentException($"Could not find target assembly: \"{target}\".");

        if (!IsSupportedTargetAssembly(target))
            throw new ArgumentException("Unsupported target architecture. Only x64, Arm64 and AnyCPU assemblies are supported.");

        switch (configArgs.Kind)
        {
            case TargetKind.Executable:
            case TargetKind.TestAssembly:
                break;
            default:
                throw new ArgumentException($"Unsupported target kind: \"{configArgs.Kind}\".");
        }
    }

    private static bool IsSupportedTargetAssembly(string assemblyPath)
    {
        using var fileStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
        using var peReader = new PEReader(fileStream);
        var machine = peReader.PEHeaders.CoffHeader.Machine;

        switch (machine)
        {
            case Machine.Amd64:
                return RuntimeInformation.ProcessArchitecture == Architecture.X64;
            case Machine.Arm64:
                return RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
            case Machine.I386:
            {
                // AnyCPU is compiled into I386
                var corFlags = peReader.PEHeaders.CorHeader?.Flags ?? default;
                return (corFlags & CorFlags.Requires32Bit) == 0;
            }
            default:
                return false;
        }
    }

    private static void ThrowOnInvalidRuntimeConfiguration(RuntimeConfigurationArgs configArgs)
    {
        var profilerClsid = configArgs.Profiler.Clsid;

        if (!Guid.TryParse(profilerClsid, out var parsedClsid) || parsedClsid == Guid.Empty)
            throw new ArgumentException($"Invalid profiler CLSID: \"{profilerClsid}\".");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var windowsProfilerPath = configArgs.Profiler.PathWindowsX64;
            if (string.IsNullOrEmpty(windowsProfilerPath))
                throw new ArgumentException($"No profiler path specified for Windows x64 platform.");
            if (!File.Exists(windowsProfilerPath))
                throw new ArgumentException($"Could not find Windows x64 profiler library: \"{windowsProfilerPath}\".");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var isArm64 = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
            var platformName = isArm64 ? "Linux arm64" : "Linux x64";
            var linuxProfilerPath = isArm64 ? configArgs.Profiler.PathLinuxArm64 : configArgs.Profiler.PathLinuxX64;
            if (string.IsNullOrEmpty(linuxProfilerPath))
                throw new ArgumentException($"No profiler path specified for {platformName} platform.");
            if (!File.Exists(linuxProfilerPath))
                throw new ArgumentException($"Could not find {platformName} profiler library: \"{linuxProfilerPath}\".");
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
        var pluginTypeName = configArgs.PluginFullTypeName;
        var pluginName = configArgs.PluginName;
        var pluginPath = configArgs.Path;

        if (string.IsNullOrWhiteSpace(pluginPath))
            throw new ArgumentException($"Invalid plugin path: \"{pluginPath}\".");
        if (!File.Exists(pluginPath))
            throw new ArgumentException($"Could not find plugin assembly: \"{pluginPath}\".");
        if (string.IsNullOrWhiteSpace(pluginTypeName) && string.IsNullOrWhiteSpace(pluginName))
            throw new ArgumentException($"Either \"{nameof(configArgs.PluginFullTypeName)}\" or \"{configArgs.PluginName}\" must be specified in the analysis configuration.");
    }
}