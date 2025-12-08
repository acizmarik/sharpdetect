// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Communication;
using SharpDetect.Core.Configuration;
using SharpDetect.Core.Plugins;
using SharpDetect.Loader;
using SharpDetect.Metadata;
using SharpDetect.PluginHost;
using SharpDetect.Reporting;
using SharpDetect.Serialization;
using SharpDetect.Worker.Commands.Run;
using SharpDetect.Worker.Services;

namespace SharpDetect.Worker.Configuration;

public sealed class AnalysisServiceProviderBuilder(RunCommandArgs arguments)
{
    private readonly ServiceCollection _services = new();
    private Action<ILoggingBuilder>? _configureLogger;
    private bool _pluginSet;
    private bool _timeProviderSet;

    public AnalysisServiceProviderBuilder ConfigureLogging(Action<ILoggingBuilder> configure)
    {
        _configureLogger = configure;
        return this;
    }
    
    public AnalysisServiceProviderBuilder WithTimeProvider(TimeProvider timeProvider)
    {
        _timeProviderSet = true;
        _services.AddSingleton(timeProvider);
        return this;
    }
    
    public AnalysisServiceProviderBuilder WithPlugin(Type pluginType)
    {
        _pluginSet = true;
        _services.AddSingleton(p => (IPlugin)ActivatorUtilities.CreateInstance(p, pluginType));
        _services.AddSingleton(pluginType, p => p.GetRequiredService<IPlugin>());
        return this;
    }
    
    public IServiceProvider Build()
    {
        _services.AddSingleton(arguments);
        _services.AddSingleton(new PathsConfiguration
        {
            TemporaryFilesFolder = arguments.Analysis.TemporaryFilesFolder,
            ReportsFolder = arguments.Analysis.ReportsFolder
        });
        _services.AddLogging(builder => _configureLogger?.Invoke(builder));
        _services.AddSharpDetectLoaderServices();
        _services.AddSharpDetectMetadataServices();
        _services.AddSharpDetectCommunicationServices();
        _services.AddSharpDetectSerializationServices();
        _services.AddSharpDetectReportingServices();
        _services.AddSharpDetectPluginHostServices();
        _services.AddSingleton<IAnalysisWorker, AnalysisWorker>();
        
        if (!_pluginSet)
            throw new InvalidOperationException($"Plugin type not set. Use {nameof(WithPlugin)}.");
        if (!_timeProviderSet)
            throw new InvalidOperationException($"TimeProvider not set. Use {nameof(WithTimeProvider)}.");
        
        return _services.BuildServiceProvider();
    }
}