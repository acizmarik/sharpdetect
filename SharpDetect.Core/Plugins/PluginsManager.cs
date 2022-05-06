using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Services;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace SharpDetect.Core.Plugins
{
    internal class PluginsManager : IPluginsManager
    {
        private readonly IConfiguration configuration;
        private readonly ILogger<PluginsManager> logger;
        private readonly DirectoryInfo pluginsRootDirectory;
        private readonly Dictionary<string, Assembly> loadedAssemblies;
        private readonly Dictionary<PluginInfo, Func<IPlugin>> pluginActivators;

        public PluginsManager(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.pluginsRootDirectory = new(configuration.GetRequiredSection(Constants.Environment.SharpDetectPluginsRootFolder).Value);
            this.logger = loggerFactory.CreateLogger<PluginsManager>();
            this.loadedAssemblies = new();
            this.pluginActivators = new();

            // Ensure directory is valid
            ThrowHelpers.ThrowIf<ArgumentException>(pluginsRootDirectory.Exists);
        }

        public IEnumerable<PluginInfo> GetLoadedPluginInfos()
        {
            return pluginActivators.Keys;
        }

        public async Task<int> LoadPluginsAsync()
        {
            logger.LogDebug("[{class}] Looking for plugins in directory: {directory}", nameof(PluginsManager), pluginsRootDirectory);

            // Perform DFS from root directory through all sub-directories and search for .NET assemblies
            var directoryStack = new Stack<string>();
            var visitedDirectories = new HashSet<string>() { pluginsRootDirectory.FullName };
            directoryStack.Push(pluginsRootDirectory.FullName);

            await Task.Run(() =>
            {
                while (directoryStack.Count > 0)
                {
                    var currentDirectory = directoryStack.Pop();
                    foreach (var assemblyPath in Directory.GetFiles(currentDirectory)
                        .Where((f) => Path.GetExtension(f).Equals(".dll", StringComparison.InvariantCultureIgnoreCase)))
                    {
                        // Try to load all assemblies from the current directory
                        if (TryLoadAssembly(assemblyPath, out var cached, out var assembly) && !cached)
                        {
                            // Search for all plugins
                            foreach (var pluginType in FindPlugins(assembly))
                            {
                                // Ensure plugin can be created
                                if (Activator.CreateInstance(pluginType) is not IPlugin plugin)
                                    continue;

                                var pluginInfo = new PluginInfo(plugin.Name, plugin.Version, assemblyPath);
                                pluginActivators.Add(pluginInfo, () => (IPlugin)Activator.CreateInstance(pluginType)!);
                            }
                        }
                    }

                    // Check also all subdirectories
                    foreach (var subdirectory in Directory.GetDirectories(currentDirectory)
                        .Where((d) => !visitedDirectories.Contains(d)))
                    {
                        directoryStack.Push(subdirectory);
                        visitedDirectories.Add(subdirectory);
                    }
                }
            });

            logger.LogDebug("[{class}] Found {count} plugin.", nameof(PluginsManager), pluginActivators.Count);
            return pluginActivators.Count;
        }

        public bool TryConstructPlugins(IEnumerable<PluginInfo> description, [NotNullWhen(true)] out IList<IPlugin> plugins)
        {
            throw new NotImplementedException();
        }

        private bool TryLoadAssembly(string path, out bool cached, [NotNullWhen(returnValue: true)] out Assembly? assembly)
        {
            if (loadedAssemblies.TryGetValue(path, out assembly))
            {
                cached = true;
                return true;
            }

            cached = false;
            try
            {
                assembly = Assembly.LoadFrom(path);
                loadedAssemblies.Add(path, assembly);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "[{class}] Could not load assembly due to an error.", nameof(PluginsManager));
                assembly = null;
                return false;
            }
        }

        private static IEnumerable<Type> FindPlugins(Assembly assembly)
        {
            foreach (var plugin in assembly.DefinedTypes.Where(t => t.ImplementedInterfaces.Contains(typeof(IPlugin))))
                yield return plugin;
        }
    }
}
