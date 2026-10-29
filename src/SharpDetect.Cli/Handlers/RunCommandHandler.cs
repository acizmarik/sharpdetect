// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using CliFx;
using CliFx.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Worker;
using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Cli.Handlers;

internal sealed class RunCommandHandler : IDisposable
{
    private readonly RunCommandArgs _arguments;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _rawConfigurationJson;
    private bool _disposed;

    public RunCommandHandler(string configurationFilePath)
    {
        try
        {
            _rawConfigurationJson = LoadConfigurationJson(configurationFilePath);
            _arguments = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(_rawConfigurationJson);
            _serviceProvider = Initialize(_arguments);
        }
        catch (Exception ex) when (ex is not CommandException)
        {
            throw new CommandException(ExceptionMessages.Flatten(ex), (int)ExitCode.ConfigurationError, innerException: ex);
        }
    }

    public RunCommandHandler(RunCommandArgs arguments, string rawConfigurationJson)
    {
        try
        {
            _arguments = arguments;
            _rawConfigurationJson = rawConfigurationJson;
            _serviceProvider = Initialize(_arguments);
        }
        catch (Exception ex) when (ex is not CommandException)
        {
            throw new CommandException(ExceptionMessages.Flatten(ex), (int)ExitCode.ConfigurationError, innerException: ex);
        }
    }

    private IServiceProvider Initialize(RunCommandArgs arguments)
    {
        CommandArgumentsValidator.ValidateRunCommandArguments(arguments);
        var pluginType = LoadPluginInfo();
        var provider = new AnalysisServiceProviderBuilder(arguments)
            .WithTimeProvider(TimeProvider.System)
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(arguments.Analysis.LogLevel);
            })
            .WithPlugin(pluginType)
            .Build();

        // Instantiate the plugin eagerly - it parses its own configuration section and reports problems immediately
        provider.GetRequiredService<IPlugin>();
        return provider;
    }

    public async ValueTask ExecuteAsync(IConsole console, CancellationToken cancellationToken)
    {
        // Execute analysis
        var worker = _serviceProvider.GetRequiredService<IAnalysisWorker>();
        var outcome = await worker.ExecuteAsync(cancellationToken);
        
        // Create report summary
        var plugin = _serviceProvider.GetRequiredService<IPlugin>();
        var reportSummary = plugin.CreateDiagnostics();
        var analysisWarnings = outcome.Gaps.Select(gap => gap.Description).ToArray();
        
        // Persist report summary
        if (_arguments.Analysis.RenderReport)
        {
            var reportFullPath = await RenderAndWriteReport(plugin, reportSummary, analysisWarnings, cancellationToken);
            await console.Output.WriteLineAsync($"Report stored to file: {Path.GetFullPath(reportFullPath)}.");
        }

        var issuesFound = reportSummary.GetAllReports().Any();
        var exitCode = GetExitCode(outcome, issuesFound);
        if (exitCode != ExitCode.Success)
            throw new CommandException(GetExitMessage(outcome, issuesFound), (int)exitCode);
    }

    internal static ExitCode GetExitCode(AnalysisOutcome outcome, bool issuesFound)
    {
        if (!outcome.IsComplete)
            return ExitCode.AnalysisIncomplete;

        return issuesFound ? ExitCode.IssuesFound : ExitCode.Success;
    }

    private static string GetExitMessage(AnalysisOutcome outcome, bool issuesFound)
    {
        if (outcome.IsComplete)
            return "Analysis detected issue(s)";

        var gaps = string.Join(" ", outcome.Gaps.Select(gap => gap.Description));
        return issuesFound
            ? $"Analysis detected issue(s), but is incomplete: {gaps}"
            : $"Analysis is incomplete: {gaps}";
    }

    private Task<string> RenderAndWriteReport(
        IPlugin plugin,
        Summary reportSummary,
        IReadOnlyList<string> analysisWarnings,
        CancellationToken cancellationToken)
    {
        var reportDirectory = _arguments.Analysis.ReportsFolder;
        var reportFileName = _arguments.Analysis.ReportFileName;
        var writer = _serviceProvider.GetRequiredService<IReportSummaryWriter>();
        var renderer = _serviceProvider.GetRequiredService<IReportSummaryRenderer>();
        var context = new SummaryRenderingContext(
            reportSummary,
            plugin,
            plugin.ReportTemplates,
            _rawConfigurationJson,
            analysisWarnings);
        return writer.Write(reportFileName, reportDirectory, context, renderer, cancellationToken);
    }

    private static string LoadConfigurationJson(string configurationPath)
    {
        try
        {
            return File.ReadAllText(configurationPath);
        }
        catch (Exception exception)
        {
            throw new IOException(
                $"Could not load configuration file from path: \"{configurationPath}\": {exception.Message}",
                exception);
        }
    }

    private Type LoadPluginInfo()
    {
        var assemblyPath = _arguments.Analysis.Path;
        var pluginTypeName = _arguments.Analysis.PluginFullTypeName;
        var pluginName = _arguments.Analysis.PluginName;

        try
        {
            var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));

            if (!string.IsNullOrWhiteSpace(pluginTypeName))
            {
                return assembly.ManifestModule.GetType(pluginTypeName, ignoreCase: false, throwOnError: true)
                    ?? throw new TypeLoadException($"Could not find type: \"{pluginTypeName}\" in assembly \"{assembly.FullName}\".");
            }

            var match = assembly
                .GetTypes().Concat(assembly.GetForwardedTypes())
                .SingleOrDefault(type =>
                    type.GetCustomAttribute<PluginMetadataAttribute>()?.Name
                        .Equals(pluginName, StringComparison.OrdinalIgnoreCase) == true);

            return match
                ?? throw new TypeLoadException($"Could not find plugin named \"{pluginName}\" in assembly \"{assembly.FullName}\".");
        }
        catch (TypeLoadException)
        {
            // Already names the plugin and the assembly it was looked up in
            throw;
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Could not load plugin assembly \"{assemblyPath}\": {e.Message}", e);
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }
}
