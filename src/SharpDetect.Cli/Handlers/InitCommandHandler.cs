// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using CliFx;
using CliFx.Infrastructure;
using SharpDetect.Cli.Commands;
using SharpDetect.Cli.Output;
using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;

namespace SharpDetect.Cli.Handlers;

internal sealed class InitCommandHandler
{
    private const string EnableStackTraceCollectionOption = "EnableStackTraceCollection";
    private const string StackTraceCollectionMaxDepthOption = "StackTraceCollectionMaxDepth";
    private const string SkipInstrumentationForAssembliesOption = "SkipInstrumentationForAssemblies";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = null
    };

    private readonly string _outputFile;
    private readonly string _currentDirectory;
    private readonly AnalysisOptions _options;

    public InitCommandHandler(string outputFile, AnalysisOptions options, string? currentDirectory = null)
    {
        _currentDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        _outputFile = Path.GetFullPath(outputFile, _currentDirectory);
        _options = options;
    }

    public async ValueTask ExecuteAsync(IConsole console, CancellationToken cancellationToken)
    {
        var targetAssemblyPath = Path.GetFullPath(_options.TargetPath, _currentDirectory);

        if (File.Exists(_outputFile))
            throw new CommandException($"File '{_outputFile}' already exists.", (int)ExitCode.ConfigurationError);

        if (!File.Exists(targetAssemblyPath))
            throw new CommandException($"File '{targetAssemblyPath}' does not exist. Please specify a valid target application path.", (int)ExitCode.ConfigurationError);

        if (!IsValidDotNetAssembly(targetAssemblyPath))
            throw new CommandException($"File '{targetAssemblyPath}' is not a valid .NET assembly.", (int)ExitCode.ConfigurationError);

        var json = CreateTemplateConfigurationJson();
        await File.WriteAllTextAsync(_outputFile, json, cancellationToken);
        await console.Output.WriteLineAsync($"Configuration file created: {_outputFile}");
    }

    internal string CreateTemplateConfigurationJson()
    {
        var configurationDirectory = Path.GetDirectoryName(_outputFile)!;
        var templateArgs = BuildRunCommandArgs(_options with
        {
            TargetPath = ConfigurationPathResolver.MakeRelative(_options.TargetPath, _currentDirectory, configurationDirectory),
            Output = (_options.Output ?? OutputOptions.Default).MapReportPath(
                path => ConfigurationPathResolver.MakeRelative(path, _currentDirectory, configurationDirectory))
        });
        return JsonSerializer.Serialize(templateArgs, JsonSerializerOptions);
    }

    internal static RunCommandArgs BuildRunCommandArgs(AnalysisOptions options)
    {
        var target = options.IsTest
            ? new TargetConfigurationArgs(
                path: options.TargetPath,
                kind: TargetKind.TestAssembly,
                test: new TestTargetConfigurationArgs(
                    options.TestRunner ?? TestTargetConfigurationArgs.DefaultRunner,
                    options.TestFilter,
                    additionalRunnerArgs: options.TargetArguments))
            : new TargetConfigurationArgs(
                path: options.TargetPath,
                args: options.TargetArguments,
                redirectInputOutput: new RedirectInputOutputConfigurationArgs(singleConsoleMode: true));

        var pluginConfiguration = BuildPluginConfiguration(options);
        var pluginNameOrTypeFullName = options.PluginNameOrTypeFullName;
        var analysis = pluginNameOrTypeFullName.Contains('.')
            ? new AnalysisPluginConfigurationArgs(
                configuration: pluginConfiguration,
                pluginFullTypeName: pluginNameOrTypeFullName,
                renderReport: true)
            : new AnalysisPluginConfigurationArgs(
                configuration: pluginConfiguration,
                pluginName: pluginNameOrTypeFullName,
                renderReport: true);

        var arguments = new RunCommandArgs(Runtime: null, target, analysis);
        return (options.Output ?? OutputOptions.Default).ApplyTo(arguments);
    }

    private static Dictionary<string, object> BuildPluginConfiguration(AnalysisOptions options)
    {
        var instrumentationSkipList = options.InstrumentSystemLibraries
            ? []
            : options.IsTest
                ? WellKnownModules.SystemAndTestFrameworksModulePrefixes
                : WellKnownModules.SystemModulePrefixes;

        var configuration = new Dictionary<string, object>
        {
            [SkipInstrumentationForAssembliesOption] = instrumentationSkipList
        };

        if (options.CollectStackTraces)
        {
            configuration[EnableStackTraceCollectionOption] = true;
            if (options.StackTraceDepth is { } depth)
                configuration[StackTraceCollectionMaxDepthOption] = depth;
        }

        return configuration;
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
