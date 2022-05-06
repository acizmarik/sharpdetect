using SharpDetect.Common.Plugins;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.Services
{
    public interface IPluginsManager
    {
        Task<int> LoadPluginsAsync();
        IEnumerable<PluginInfo> GetLoadedPluginInfos();

        bool TryConstructPlugins(IEnumerable<PluginInfo> description, [NotNullWhen(returnValue: true)] out IList<IPlugin> plugins);
    }
}
