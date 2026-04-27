// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using CliFx.Exceptions;
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
            CommandArgumentsValidator.ValidateRunCommandArguments(_arguments);

            var pluginType = LoadPluginInfo();
            _serviceProvider = new AnalysisServiceProviderBuilder(_arguments)
                .WithTimeProvider(TimeProvider.System)
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(_arguments.Analysis.LogLevel);
                })
                .WithPlugin(pluginType)
                .Build();
        }
        catch (Exception ex) when (ex is not CommandException)
        {
            throw new CommandException(ex.Message, (int)ExitCode.ConfigurationError, innerException: ex);
        }
    }

    public async ValueTask ExecuteAsync(IConsole console, CancellationToken cancellationToken)
    {
        // Execute analysis
        var worker = _serviceProvider.GetRequiredService<IAnalysisWorker>();
        await worker.ExecuteAsync(cancellationToken);
        
        // Create report summary
        var plugin = _serviceProvider.GetRequiredService<IPlugin>();
        var reportSummary = plugin.CreateDiagnostics();
        
        // Persist report summary
        if (_arguments.Analysis.RenderReport)
        {
            var reportFullPath = await RenderAndWriteReport(plugin, reportSummary, cancellationToken);
            await console.Output.WriteLineAsync($"Report stored to file: {Path.GetFullPath(reportFullPath)}.");
        }

        if (reportSummary.GetAllReports().Any())
            throw new CommandException("Analysis detected issue(s)", (int)ExitCode.IssuesFound);
    }

    private Task<string> RenderAndWriteReport(IPlugin plugin, Summary reportSummary, CancellationToken cancellationToken)
    {
        var reportDirectory = _arguments.Analysis.ReportsFolder;
        var reportFileName = _arguments.Analysis.ReportFileName;
        var writer = _serviceProvider.GetRequiredService<IReportSummaryWriter>();
        var renderer = _serviceProvider.GetRequiredService<IReportSummaryRenderer>();
        var context = new SummaryRenderingContext(reportSummary, plugin, plugin.ReportTemplates, _rawConfigurationJson);
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
            throw new IOException($"Could not load configuration file from path: \"{configurationPath}\".", exception);
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
        catch (Exception e)
        {
            throw new ArgumentException("Error during loading plugin.", e);
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
