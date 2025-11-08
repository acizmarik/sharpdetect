// Copyright 2025 Andrej Čižmárik and Contributors
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
            .ConfigureLogging(logging => logging.AddConsole())
            .WithPlugin(pluginType)
            .Build();
    }

    public async ValueTask ExecuteAsync(IConsole console, CancellationToken cancellationToken)
    {
        // Execute analysis
        var worker = _serviceProvider.GetRequiredService<IAnalysisWorker>();
        await worker.ExecuteAsync(cancellationToken);
        
        // Render and store report
        var report = GenerateReportSummary();
        var fileName = $"SharpDetect_Report_{TimeProvider.System.GetUtcNow().DateTime:O}.html";
        await StoreReportSummary(report, fileName, cancellationToken);
        await console.Output.WriteLineAsync($"Report stored to file: {Path.GetFullPath(fileName)}.");
    }

    private Type LoadPluginInfo()
    {
        var assemblyPath = _arguments.Analysis.Path;
        var pluginType = _arguments.Analysis.FullTypeName;

        try
        {
#pragma warning disable S3885 // "Assembly.Load" should be used because we are loading assemblies using paths
            var assembly = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
#pragma warning restore S3885
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
    
    private static Task StoreReportSummary(string reportSummary, string filename, CancellationToken cancellationToken)
    {
        return File.WriteAllTextAsync(filename, reportSummary, cancellationToken);
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
