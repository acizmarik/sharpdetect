// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliFx;
using CliFx.Infrastructure;
using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Handlers;

internal sealed class InitCommandHandler(
    string outputFile,
    string pluginNameOrTypeFullName,
    string targetAssemblyPath,
    bool instrumentSystemLibraries,
    bool isTest,
    TestRunner? testRunner,
    string? testFilter)
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
        var templateArgs = BuildRunCommandArgs(
            pluginNameOrTypeFullName,
            targetAssemblyPath,
            instrumentSystemLibraries,
            isTest,
            testRunner,
            testFilter);
        return JsonSerializer.Serialize(templateArgs, JsonSerializerOptions);
    }

    internal static RunCommandArgs BuildRunCommandArgs(
        string pluginNameOrTypeFullName,
        string targetAssemblyPath,
        bool instrumentSystemLibraries,
        bool isTest,
        TestRunner? testRunner,
        string? testFilter)
    {
        var target = isTest
            ? new TargetConfigurationArgs(
                path: targetAssemblyPath,
                kind: TargetKind.TestAssembly,
                test: new TestTargetConfigurationArgs(testRunner ?? TestTargetConfigurationArgs.DefaultRunner, testFilter))
            : new TargetConfigurationArgs(
                path: targetAssemblyPath,
                redirectInputOutput: new RedirectInputOutputConfigurationArgs(singleConsoleMode: true));

        var pluginConfiguration = BuildPluginConfiguration(isTest, instrumentSystemLibraries);
        var analysis = pluginNameOrTypeFullName.Contains('.')
            ? new AnalysisPluginConfigurationArgs(
                configuration: pluginConfiguration,
                pluginFullTypeName: pluginNameOrTypeFullName,
                renderReport: true)
            : new AnalysisPluginConfigurationArgs(
                configuration: pluginConfiguration,
                pluginName: pluginNameOrTypeFullName,
                renderReport: true);

        return new RunCommandArgs(Runtime: null, target, analysis);
    }

    private static object BuildPluginConfiguration(
        bool isTest,
        bool instrumentSystemLibraries)
    {
        var instrumentationSkipList = instrumentSystemLibraries
            ? []
            : isTest
                ? WellKnownModules.SystemAndTestFrameworksModulePrefixes
                : WellKnownModules.SystemModulePrefixes;

        return new { SkipInstrumentationForAssemblies = instrumentationSkipList };
    }

    internal static string SerializeRunCommandArgs(RunCommandArgs args)
        => JsonSerializer.Serialize(args, JsonSerializerOptions);

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
