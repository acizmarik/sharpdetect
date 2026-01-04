// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliFx.Infrastructure;
using Microsoft.Extensions.Logging;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Handlers;

internal sealed class InitCommandHandler(string outputFile, string pluginType, string targetAssemblyPath)
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = null
    };

    public async ValueTask ExecuteAsync(IConsole console, CancellationToken cancellationToken)
    {
        if (File.Exists(outputFile))
        {
            await console.Error.WriteLineAsync($"File '{outputFile}' already exists.");
            return;
        }

        if (!File.Exists(targetAssemblyPath))
        {
            await console.Error.WriteLineAsync($"File '{targetAssemblyPath}' does not exist. Please specify a valid target application path.");
            return;
        }

        if (!IsValidDotNetAssembly(targetAssemblyPath))
        {
            await console.Error.WriteLineAsync($"File '{targetAssemblyPath}' is not a valid .NET assembly.");
            return;
        }

        var templateArgs = CreateTemplateConfiguration();
        var json = JsonSerializer.Serialize(templateArgs, JsonSerializerOptions);

        await File.WriteAllTextAsync(outputFile, json, cancellationToken);
        await console.Output.WriteLineAsync($"Configuration file created: {Path.GetFullPath(outputFile)}");
    }

    private RunCommandArgs CreateTemplateConfiguration()
    {
        // Defaults
        const string defaultPluginPath = "%SHARPDETECT_ROOT%/Plugins/SharpDetect.Plugins.dll";
        const string defaultProfilerClsid = "{b2c60596-b36d-460b-902a-3d91f5878529}";
        var profilerPathArgs = new ProfilerPathArgs(
            WindowsX64: "%SHARPDETECT_ROOT%/Profilers/win-x64/SharpDetect.Concurrency.Profiler.dll",
            LinuxX64: "%SHARPDETECT_ROOT%/Profilers/linux-x64/SharpDetect.Concurrency.Profiler.so");

        var target = new TargetConfigurationArgs(
            Path: targetAssemblyPath,
            Architecture: RuntimeInformation.ProcessArchitecture,
            Args: null,
            WorkingDirectory: null,
            AdditionalEnvironmentVariables: null,
            RedirectInputOutput: new RedirectInputOutputConfigurationArgs(
                SingleConsoleMode: true,
                StdinFilePath: null,
                StdoutFilePath: null,
                StderrFilePath: null
            )
        );

        var runtime = new RuntimeConfigurationArgs(
            Host: null,
            Profiler: new ProfilerConfigurationArgs(
                Clsid: defaultProfilerClsid,
                Path: profilerPathArgs,
                LogLevel: ProfilerLogLevel.Warning
            )
        );

        var analysis = new AnalysisPluginConfigurationArgs(
            Path: defaultPluginPath,
            FullTypeName: pluginType,
            Configuration: "",
            RenderReport: true,
            LogLevel: LogLevel.Warning,
            TemporaryFilesFolder: null,
            ReportsFolder: null
        );

        return new RunCommandArgs(runtime, target, analysis);
    }

    private static bool IsValidDotNetAssembly(string filePath)
    {
        try
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var peReader = new PEReader(fileStream);
            if (!peReader.HasMetadata)
                return false;
            
            var metadataReader = peReader.GetMetadataReader();
            return metadataReader.IsAssembly;
        }
        catch
        {
            return false;
        }
    }
}

