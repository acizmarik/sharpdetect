using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services
{
    public interface IPluginsManager
    {
        Task<int> LoadPluginsAsync(CancellationToken ct);
        IEnumerable<PluginInfo> GetLoadedPluginInfos();

        bool TryConstructPlugins(string[] pluginDescriptions, [NotNullWhen(returnValue: true)] out IPlugin[] plugins);
    }
}
