// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using CliFx.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting;
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
        
        // Render and store report
        var timeProvider = _serviceProvider.GetRequiredService<TimeProvider>();
        var reportContent = GenerateReportSummary();
        var reportsFolder = _arguments.Analysis.ReportsFolder;
        var reportFileName = _arguments.Analysis.ReportFileName;
        var fullPath = await StoreReport(reportFileName, reportsFolder, reportContent, timeProvider, cancellationToken);
        await console.Output.WriteLineAsync($"Report stored to file: {Path.GetFullPath(fullPath)}.");
    }

    internal static Task<string> StoreReport(
        string content,
        TimeProvider timeProvider,
        CancellationToken ct)
    {
        return StoreReport(reportFileName: null, reportsFolder: null, content, timeProvider, ct);
    }
    
    private static async Task<string> StoreReport(
        string? reportFileName,
        string? reportsFolder,
        string content,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        // Use default report folder and file name if not specified
        reportsFolder ??= Directory.GetCurrentDirectory();
        reportFileName ??= $"SharpDetect_Report_{timeProvider.GetUtcNow().DateTime:yyyyMMdd_HHmmss}.html";
        
        // Ensure reports folder exists
        Directory.CreateDirectory(reportsFolder);
        
        var fullPath = Path.Combine(reportsFolder, reportFileName);
        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
        return fullPath;
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
    
    private string GenerateReportSummary()
    {
        var plugin = _serviceProvider.GetRequiredService<IPlugin>();
        var reportRenderer = _serviceProvider.GetRequiredService<IReportSummaryRenderer>();
        return reportRenderer.Render(plugin.CreateDiagnostics(), plugin, plugin.ReportTemplates);
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
