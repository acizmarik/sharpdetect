using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Plugins;
using SharpDetect.Common.Plugins.Metadata;
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
        private readonly Dictionary<string, List<PluginInfo>> loadedPluginInfos;
        private readonly Dictionary<Type, PluginInfo> pluginInfosLookup;
        private readonly Dictionary<PluginInfo, Func<IServiceProvider, IConfiguration, IPlugin>> pluginActivators;
        private volatile bool loaded;

        public PluginsManager(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            this.configuration = configuration;
            this.pluginsRootDirectory = new(configuration.GetRequiredSection(Constants.Configuration.PluginsRootFolder).Value);
            this.logger = loggerFactory.CreateLogger<PluginsManager>();
            this.loadedAssemblies = new();
            this.loadedPluginInfos = new();
            this.pluginInfosLookup = new();
            this.pluginActivators = new();

            // Ensure directory is valid
            Guard.True<ArgumentException>(pluginsRootDirectory.Exists);
        }

        public IEnumerable<PluginInfo> GetLoadedPluginInfos()
        {
            return pluginActivators.Keys;
        }

        public Task<int> LoadPluginsAsync(CancellationToken ct)
        {
            if (!loaded)
            {
                lock(this)
                {
                    if (!loaded)
                    {
                        // Ensure only one thread continues
                        return LoadPluginsImplAsync(ct);
                    }
                }
            }

            return Task.FromResult(loadedPluginInfos.Count);
        }

        public bool TryGetPluginInfo(IPlugin plugin, [NotNullWhen(true)] out PluginInfo? pluginInfo)
        {
            var result = pluginInfosLookup.TryGetValue(plugin.GetType(), out var info);
            pluginInfo = (result) ? info : null;
            return result;
        }

        private async Task<int> LoadPluginsImplAsync(CancellationToken ct)
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
                                // Ensure plugin is exported
                                if (!TryGetPluginMetadata(pluginType, out var name, out var version))
                                    continue;

                                var pluginInfo = new PluginInfo(name, version, assemblyPath);

                                // Add new record about available plugin
                                if (!loadedPluginInfos.ContainsKey(name))
                                    loadedPluginInfos.Add(name, new());
                                loadedPluginInfos[name].Add(pluginInfo);
                                pluginInfosLookup.Add(pluginType, pluginInfo);

                                // Add activator for the plugin
                                pluginActivators.Add(pluginInfo, (sp, conf) =>
                                {
                                    var plugin = (IPlugin)ActivatorUtilities.CreateInstance(sp, pluginType);
                                    var configurationEntry = $"{Constants.Configuration.PluginSettings}:{name}";
                                    var configurationItems = conf.GetSection(configurationEntry).AsEnumerable();
                                    var configurationBuilder = new ConfigurationBuilder();
                                    configurationBuilder.AddInMemoryCollection(configurationItems
                                        .Select(s => new KeyValuePair<string, string>(s.Key, s.Value))
                                        .Where(kv => kv.Value != null));
                                    plugin.Configure(configurationBuilder.Build());
                                    return plugin;
                                });
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
            }, ct);

            logger.LogDebug("[{class}] Found {count} plugin(s).", nameof(PluginsManager), pluginActivators.Count);
            loaded = true;
            return pluginActivators.Count;
        }

        public bool TryConstructPlugins(string[] pluginDescriptions, IConfiguration globalConfiguration, IServiceProvider provider, [NotNullWhen(true)] out IPlugin[] plugins)
        {
            var index = 0;
            plugins = new IPlugin[pluginDescriptions.Length];
            foreach (var pluginDescription in pluginDescriptions.Select(p => p.Trim()))
            {
                // Ensure plugin is available
                if (!loadedPluginInfos.TryGetValue(pluginDescription, out var pluginInfo))
                {
                    logger.LogError("[{class}] Could not find plugin {plugin}.", nameof(PluginsManager), pluginDescription);
                    return false;
                }

                // Ensure it can be created
                bool result;
                try
                {


                    var plugin = pluginActivators[pluginInfo.First()](provider, globalConfiguration);
                    plugins[index++] = plugin;
                    result = true;
                }
                catch
                {
                    logger.LogError("[{class}] Could not instantiate or initialize plugin {plugin}.", nameof(PluginsManager), pluginInfo);
                    result = false;
                }

                if (!result)
                    return false;
            }

            return true;
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

        private IEnumerable<Type> FindPlugins(Assembly assembly)
        {
            var result = new List<Type>();
            try
            {
                foreach (var plugin in assembly.DefinedTypes.Where(t => t.ImplementedInterfaces.Contains(typeof(IPlugin))))
                    result.Add(plugin);
            }
            catch (Exception e)
            {
                logger.LogError(e, "[{class}] Could not load types due to an error.", nameof(PluginsManager));
                return Enumerable.Empty<Type>();
            }

            return result;
        }

        private static bool TryGetPluginMetadata(Type plugin, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out Version? version)
        {
            name = null;
            version = default;
            var args = plugin.CustomAttributes.FirstOrDefault(a => a.AttributeType == typeof(PluginExportAttribute))?.ConstructorArguments;
            if (args == null)
                return false;

            name = (string)args[0].Value!;
            version = Version.Parse((string)args[1].Value!);
            return true;
        }
    }
}
