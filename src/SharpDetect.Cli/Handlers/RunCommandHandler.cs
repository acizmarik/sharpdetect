// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
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
    private bool _disposed;

    public RunCommandHandler(string configurationFilePath)
    {
        _arguments = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(configurationFilePath);
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

    public async ValueTask ExecuteAsync(IConsole console, CancellationToken cancellationToken)
    {
        // Execute analysis
        var worker = _serviceProvider.GetRequiredService<IAnalysisWorker>();
        await worker.ExecuteAsync(cancellationToken);
        
        // Create report summary
        var plugin = _serviceProvider.GetRequiredService<IPlugin>();
        var reportSummary = plugin.CreateDiagnostics();
        
        // Persist report summary
        var reportFullPath = await RenderAndWriteReport(plugin, reportSummary, cancellationToken);
        await console.Output.WriteLineAsync($"Report stored to file: {Path.GetFullPath(reportFullPath)}.");
    }

    private Task<string> RenderAndWriteReport(IPlugin plugin, Summary reportSummary, CancellationToken cancellationToken)
    {
        var reportDirectory = _arguments.Analysis.ReportsFolder;
        var reportFileName = _arguments.Analysis.ReportFileName;
        var writer = _serviceProvider.GetRequiredService<IReportSummaryWriter>();
        var renderer = _serviceProvider.GetRequiredService<IReportSummaryRenderer>();
        var context = new SummaryRenderingContext(reportSummary, plugin, plugin.ReportTemplates);
        return writer.Write(reportFileName, reportDirectory, context, renderer, cancellationToken);
    }

    private Type LoadPluginInfo()
    {
        var assemblyPath = _arguments.Analysis.Path;
        var pluginType = _arguments.Analysis.FullTypeName;

        try
        {
            var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
            return assembly.ManifestModule.GetType(pluginType, ignoreCase: false, throwOnError: true)
                ?? throw new TypeLoadException($"Could not find type: \"{pluginType}\" in assembly \"{assembly.FullName}\".");
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
