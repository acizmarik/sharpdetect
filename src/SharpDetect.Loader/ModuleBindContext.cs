using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Services.Metadata;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Loader
{
    internal class ModuleBindContext : IModuleBindContext
    {
        public IMetadataResolversProvider MetadataResolversProvider { get { return assemblyLoadContext; } }

        private readonly AssemblyLoadContext assemblyLoadContext;
        private readonly ConcurrentDictionary<(int ProcessId, ModuleInfo ModuleInfo), ModuleDef> modules;
        private readonly ConcurrentDictionary<int, ModuleDef> coreLibraries;
        private readonly ILogger<ModuleBindContext> logger;
        private readonly object moduleLoadedSignalizer;
        private const string coreLibraryModuleName = "System.Private.CoreLib";

        public ModuleBindContext(AssemblyLoadContext assemblyLoadContext, ILoggerFactory loggerFactory)
        {
            this.assemblyLoadContext = assemblyLoadContext;
            this.moduleLoadedSignalizer = new object();
            this.logger = loggerFactory.CreateLogger<ModuleBindContext>();
            modules = new();
            coreLibraries = new();
        }

        public ModuleDef LoadModule(int processId, string path, ModuleInfo moduleInfo)
        {
            if (modules.TryGetValue((processId, moduleInfo), out var module))
                return module;

            if (!assemblyLoadContext.TryLoadFromAssemblyPath(path, out var assembly))
                throw new ArgumentException("Could not bind module to provided path: {path}.", nameof(path));

            if (assembly.Name == coreLibraryModuleName)
                coreLibraries.TryAdd(processId, assembly.ManifestModule);

            return AddModuleToCache(processId, assembly, moduleInfo);
        }

        public ModuleDef LoadModule(int processId, Stream stream, string virtualPath, ModuleInfo moduleInfo)
        {
            if (modules.TryGetValue((processId, moduleInfo), out var module))
                return module;

            if (!assemblyLoadContext.TryLoadFromStream(stream, virtualPath, out var assembly))
                throw new ArgumentException("Could not bind module to provided path: {path}.", nameof(virtualPath));

            return AddModuleToCache(processId, assembly, moduleInfo);
        }

        private ModuleDef AddModuleToCache(int processId, AssemblyDef assembly, ModuleInfo moduleInfo)
        {
            var module = assembly.ManifestModule;
            lock (moduleLoadedSignalizer)
            {
                modules.TryAdd((processId, moduleInfo), module);
                Monitor.PulseAll(moduleLoadedSignalizer);
            }
            LogSuccessBind(moduleInfo, module);
            return module;
        }

        public ModuleDef GetModule(int processId, ModuleInfo moduleInfo)
        {
            if (!TryGetModule(processId, moduleInfo, out var module))
                throw new ArgumentException($"Module for handle: {moduleInfo.Id} was not loaded.");
            return module;
        }

        public ModuleDef WaitForModuleLoaded(int processId, ModuleInfo moduleInfo)
        {
            if (!TryGetModule(processId, moduleInfo, out var moduleDef))
            {
                lock (moduleLoadedSignalizer)
                {
                    while (!TryGetModule(processId, moduleInfo, out moduleDef))
                        Monitor.Wait(moduleLoadedSignalizer);
                }
            }

            return moduleDef;
        }

        public ModuleDef GetCoreLibModule(int processId)
        {
            if (!TryGetCoreLibModule(processId, out var module))
                throw new ArgumentException($"Core library module for PID: {processId} was not loaded.");
            return module;
        }

        public bool TryGetModule(int processId, ModuleInfo moduleInfo, [NotNullWhen(true)] out ModuleDef? module)
            => modules.TryGetValue((processId, moduleInfo), out module);

        public bool TryGetCoreLibModule(int processId, [NotNullWhen(true)] out ModuleDef? module)
            => coreLibraries.TryGetValue(processId, out module);

        public void UnloadAll()
        {
            assemblyLoadContext.UnloadAll();
            modules.Clear();
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void LogSuccessBind(ModuleInfo moduleInfo, ModuleDef module)
        {
            logger.LogDebug("Bound module: {module} to handle: {id}.", module.FullName, moduleInfo.Id);
        }
    }
}
