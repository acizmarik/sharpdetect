// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies;

internal sealed class PassthroughPluginHost : PluginHostBase
{
    public PassthroughPluginHost(
        IRecordedEventBindingsCompiler recordedEventBindingsCompiler,
        IPlugin plugin,
        ILogger<PassthroughPluginHost> logger)
        : base(recordedEventBindingsCompiler, plugin, logger)
    {
        
    }
}

