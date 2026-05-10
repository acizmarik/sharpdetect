// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;
using SharpDetect.PluginHost.Services.Strategies;

namespace SharpDetect.PluginHost.Services;

internal sealed class PluginHostFactory
{
    private readonly IRecordedEventBindingsCompiler _recordedEventBindingsCompiler;
    private readonly ILoggerFactory _loggerFactory;

    public PluginHostFactory(
        IRecordedEventBindingsCompiler recordedEventBindingsCompiler,
        ILoggerFactory loggerFactory)
    {
        _recordedEventBindingsCompiler = recordedEventBindingsCompiler;
        _loggerFactory = loggerFactory;
    }

    public IPluginHost CreateHost(IPlugin plugin)
    {
        var passthrough = new PassthroughPluginHost(
            _recordedEventBindingsCompiler,
            plugin,
            _loggerFactory.CreateLogger<PassthroughPluginHost>());

        return new ReorderingPluginHost(
            passthrough,
            _loggerFactory.CreateLogger<ReorderingPluginHost>());
    }
}
