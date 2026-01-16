// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;
using SharpDetect.PluginHost.Services.Strategies;

namespace SharpDetect.PluginHost.Services;

internal sealed class PluginHostFactory
{
    private readonly IRecordedEventBindingsCompiler _recordedEventBindingsCompiler;
    private readonly IRecordedEventsDeliveryContext _recordedEventsDeliveryContext;
    private readonly ILoggerFactory _loggerFactory;

    public PluginHostFactory(
        IRecordedEventBindingsCompiler recordedEventBindingsCompiler,
        IRecordedEventsDeliveryContext recordedEventsDeliveryContext,
        ILoggerFactory loggerFactory)
    {
        _recordedEventBindingsCompiler = recordedEventBindingsCompiler;
        _recordedEventsDeliveryContext = recordedEventsDeliveryContext;
        _loggerFactory = loggerFactory;
    }

    public IPluginHost CreateHost(IPlugin plugin)
    {
        if (plugin is IExecutionOrderingPlugin)
        {
            return new ExecutionOrderingPluginHost(
                _recordedEventBindingsCompiler,
                _recordedEventsDeliveryContext,
                plugin,
                _loggerFactory.CreateLogger<ExecutionOrderingPluginHost>());
        }

        return new PassthroughPluginHost(
            _recordedEventBindingsCompiler,
            plugin,
            _loggerFactory.CreateLogger<PassthroughPluginHost>());
    }
}

