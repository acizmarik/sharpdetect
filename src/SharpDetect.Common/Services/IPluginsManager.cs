// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services
{
    public interface IPluginsManager
    {
        Task<int> LoadPluginsAsync(CancellationToken ct);
        IEnumerable<PluginInfo> GetLoadedPluginInfos();

        bool TryConstructPlugins(string[] pluginDescriptions, IConfiguration globalConfiguration, IServiceProvider provider, [NotNullWhen(returnValue: true)] out IPlugin[] plugins);
        bool TryGetPluginInfo(IPlugin plugin, [NotNullWhen(returnValue: true)] out PluginInfo? pluginInfo);
    }
}
