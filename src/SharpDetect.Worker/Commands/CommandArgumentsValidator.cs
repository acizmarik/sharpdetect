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
        var errors = new List<string>();

        TargetConfigurationArgs? target = arguments.Target;
        AnalysisPluginConfigurationArgs? analysis = arguments.Analysis;

        if (target is null)
            errors.Add($"Configuration is missing the required section \"{nameof(RunCommandArgs.Target)}\".");
        else
            ValidateTargetConfiguration(target, errors);

        ValidateRuntimeConfiguration(arguments.Runtime, errors);

        if (analysis is null)
            errors.Add($"Configuration is missing the required section \"{nameof(RunCommandArgs.Analysis)}\".");
        else
            ValidateAnalysisConfiguration(analysis, errors);

        ThrowIfAnyError(errors);
    }

    private static void ValidateTargetConfiguration(TargetConfigurationArgs configArgs, List<string> errors)
    {
        var target = configArgs.Path;

        if (!File.Exists(target))
        {
            errors.Add($"Could not find target assembly: \"{target}\".");
        }
        else if (!IsX64CompatibleAssembly(target, out var architectureError))
        {
            errors.Add(architectureError);
        }

        switch (configArgs.Kind)
        {
            case TargetKind.Executable:
                if (configArgs.Test is not null)
                    errors.Add($"\"{nameof(TargetConfigurationArgs.Test)}\" is only applicable when \"{nameof(TargetConfigurationArgs.Kind)}\" is \"{TargetKind.TestAssembly}\".");
                break;
            case TargetKind.TestAssembly:
                if (configArgs.Test is null)
                    errors.Add($"\"{nameof(TargetConfigurationArgs.Test)}\" must be specified when \"{nameof(TargetConfigurationArgs.Kind)}\" is \"{TargetKind.TestAssembly}\".");
                break;
            default:
                errors.Add($"Unsupported target kind: \"{configArgs.Kind}\".");
                break;
        }

        if (configArgs.WorkingDirectory is { Length: > 0 } workingDirectory && !Directory.Exists(workingDirectory))
            errors.Add($"Could not find target working directory: \"{workingDirectory}\".");

        // Redirection file paths are only honored outside of the single console mode
        if (configArgs.RedirectInputOutput is { SingleConsoleMode: false } redirects)
        {
            if (redirects.StdinFilePath is { Length: > 0 } stdin && !File.Exists(stdin))
                errors.Add($"Could not find standard input redirection file: \"{stdin}\".");

            ValidateParentDirectory(redirects.StdoutFilePath, "standard output", errors);
            ValidateParentDirectory(redirects.StderrFilePath, "standard error", errors);
        }
    }

    private static void ValidateParentDirectory(string? filePath, string description, List<string> errors)
    {
        if (filePath is not { Length: > 0 })
            return;

        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (directory is { Length: > 0 } && !Directory.Exists(directory))
            errors.Add($"Could not find directory \"{directory}\" for the {description} redirection file: \"{filePath}\".");
    }

    private static bool IsX64CompatibleAssembly(string assemblyPath, out string error)
    {
        const string unsupportedArchitecture = "Unsupported target architecture. Only x64 and AnyCPU assemblies are supported.";

        try
        {
            using var fileStream = new FileStream(assemblyPath, FileMode.Open, FileAccess.Read);
            using var peReader = new PEReader(fileStream);
            var machine = peReader.PEHeaders.CoffHeader.Machine;

            switch (machine)
            {
                case Machine.Amd64:
                    error = string.Empty;
                    return true;
                case Machine.I386:
                {
                    // AnyCPU is compiled into I386
                    var corFlags = peReader.PEHeaders.CorHeader?.Flags ?? default;
                    var isAnyCpu = (corFlags & CorFlags.Requires32Bit) == 0;
                    error = isAnyCpu ? string.Empty : unsupportedArchitecture;
                    return isAnyCpu;
                }
                default:
                    error = unsupportedArchitecture;
                    return false;
            }
        }
        catch (Exception exception) when (exception is BadImageFormatException or IOException)
        {
            error = $"Target assembly \"{assemblyPath}\" is not a valid .NET assembly: {exception.Message}";
            return false;
        }
    }

    private static void ValidateRuntimeConfiguration(RuntimeConfigurationArgs configArgs, List<string> errors)
    {
        var profilerClsid = configArgs.Profiler.Clsid;

        if (!Guid.TryParse(profilerClsid, out var parsedClsid) || parsedClsid == Guid.Empty)
            errors.Add($"Invalid profiler CLSID: \"{profilerClsid}\".");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var windowsProfilerPath = configArgs.Profiler.PathWindowsX64;
            if (string.IsNullOrEmpty(windowsProfilerPath))
                errors.Add("No profiler path specified for Windows x64 platform.");
            else if (!File.Exists(windowsProfilerPath))
                errors.Add($"Could not find Windows x64 profiler library: \"{windowsProfilerPath}\".");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var linuxProfilerPath = configArgs.Profiler.PathLinuxX64;
            if (string.IsNullOrEmpty(linuxProfilerPath))
                errors.Add("No profiler path specified for Linux x64 platform.");
            else if (!File.Exists(linuxProfilerPath))
                errors.Add($"Could not find Linux x64 profiler library: \"{linuxProfilerPath}\".");
        }

        if (configArgs.Host is { } host)
        {
            var hostPath = host.Path;

            if (string.IsNullOrWhiteSpace(hostPath))
                errors.Add($"Invalid host path: \"{hostPath}\".");
            else if (IsRootedOrRelativePath(hostPath) && !File.Exists(hostPath))
                errors.Add($"Could not find host executable: \"{hostPath}\".");
        }
    }

    private static bool IsRootedOrRelativePath(string path)
        => path.Contains('/') || path.Contains(Path.DirectorySeparatorChar);

    private static void ValidateAnalysisConfiguration(AnalysisPluginConfigurationArgs configArgs, List<string> errors)
    {
        var pluginTypeName = configArgs.PluginFullTypeName;
        var pluginName = configArgs.PluginName;
        var pluginPath = configArgs.Path;

        if (string.IsNullOrWhiteSpace(pluginPath))
            errors.Add($"Invalid plugin path: \"{pluginPath}\".");
        else if (!File.Exists(pluginPath))
            errors.Add($"Could not find plugin assembly: \"{pluginPath}\".");

        if (string.IsNullOrWhiteSpace(pluginTypeName) && string.IsNullOrWhiteSpace(pluginName))
        {
            errors.Add(
                $"Either \"{nameof(configArgs.PluginFullTypeName)}\" or \"{nameof(configArgs.PluginName)}\" must be specified in the analysis configuration.");
        }

        if (configArgs.TemporaryFilesFolder is { Length: > 0 } temporaryFilesFolder &&
            !Directory.Exists(temporaryFilesFolder))
        {
            errors.Add($"Could not find temporary files folder: \"{temporaryFilesFolder}\".");
        }

        if (configArgs.RenderReport && configArgs.ReportsFolder is { Length: > 0 } reportsFolder)
        {
            try
            {
                Directory.CreateDirectory(reportsFolder);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                errors.Add($"Could not create reports folder \"{reportsFolder}\": {exception.Message}");
            }
        }
    }

    private static void ThrowIfAnyError(List<string> errors)
    {
        if (errors.Count == 0)
            return;

        if (errors.Count == 1)
            throw new ArgumentException(errors[0]);

        var details = string.Join(Environment.NewLine, errors.Select(error => $"  - {error}"));
        throw new ArgumentException($"Configuration is invalid:{Environment.NewLine}{details}");
    }
}
