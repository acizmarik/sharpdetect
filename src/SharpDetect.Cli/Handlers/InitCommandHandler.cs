// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Handlers;

internal sealed class InitCommandHandler(string outputFile, string pluginNameOrTypeFullName, string targetAssemblyPath)
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
            throw new CommandException($"File '{outputFile}' already exists.", (int)ExitCode.ConfigurationError);

        if (!File.Exists(targetAssemblyPath))
            throw new CommandException($"File '{targetAssemblyPath}' does not exist. Please specify a valid target application path.", (int)ExitCode.ConfigurationError);

        if (!IsValidDotNetAssembly(targetAssemblyPath))
            throw new CommandException($"File '{targetAssemblyPath}' is not a valid .NET assembly.", (int)ExitCode.ConfigurationError);

        var json = CreateTemplateConfigurationJson();
        await File.WriteAllTextAsync(outputFile, json, cancellationToken);
        await console.Output.WriteLineAsync($"Configuration file created: {Path.GetFullPath(outputFile)}");
    }

    internal string CreateTemplateConfigurationJson()
    {
        var templateArgs = CreateTemplateConfiguration();
        return JsonSerializer.Serialize(templateArgs, JsonSerializerOptions);
    }

    private RunCommandArgs CreateTemplateConfiguration()
    {
        var target = new TargetConfigurationArgs(
            path: targetAssemblyPath,
            redirectInputOutput: new RedirectInputOutputConfigurationArgs(singleConsoleMode: true));

        var analysis = pluginNameOrTypeFullName.Contains('.')
            ? new AnalysisPluginConfigurationArgs(pluginFullTypeName: pluginNameOrTypeFullName, renderReport: true)
            : new AnalysisPluginConfigurationArgs(pluginName: pluginNameOrTypeFullName, renderReport: true);

        return new RunCommandArgs(Runtime: null, target, analysis);
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

