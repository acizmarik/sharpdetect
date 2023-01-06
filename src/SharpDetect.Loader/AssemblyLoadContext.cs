using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Common.Services.Metadata;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Loader
{
    internal class AssemblyLoadContext : IMetadataResolversProvider
    {
        public IEnumerable<AssemblyDef> Assemblies { get { return assemblies.Values; } }
        public AssemblyResolver AssemblyResolver { get; }
        public Resolver MemberResolver { get; }
        private readonly ModuleCreationOptions moduleCreationOptions;
        private readonly ModuleContext moduleContext;
        private readonly ConcurrentDictionary<string, AssemblyDef> assemblies;
        private readonly ILogger<AssemblyLoadContext> logger;

        public AssemblyLoadContext(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<AssemblyLoadContext>();
            assemblies = new();
            moduleContext = ModuleDef.CreateModuleContext();
            moduleCreationOptions = new ModuleCreationOptions()
            {
                Context = moduleContext,
                TryToLoadPdbFromDisk = true
            };
            MemberResolver = (Resolver)moduleContext.Resolver;
            AssemblyResolver = (AssemblyResolver)moduleContext.AssemblyResolver;
            AssemblyResolver.UseGAC = false;
            AssemblyResolver.EnableFrameworkRedirect = false;
            AssemblyResolver.FindExactMatch = false;
        }

        public bool TryLoadFromAssemblyPath(string assemblyPath, [NotNullWhen(returnValue: true)] out AssemblyDef? assembly)
        {
            if (assemblies.TryGetValue(assemblyPath, out assembly))
                return true;

            try
            {
                if (!Path.IsPathFullyQualified(assemblyPath))
                {
                    logger.LogWarning("Could not load assembly {assembly}. It was probably emitted during runtime (this is not supported yet).", assemblyPath);
                    assembly = null;
                    return false;
                }

                assembly = AssemblyDef.Load(assemblyPath, moduleContext);
                AssemblyResolver.AddToCache(assembly);
                LogSuccessLoad(assemblyPath, assembly);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load assembly from path: {path}.", assemblyPath);
                assembly = null;
                return false;
            }
        }

        public bool TryLoadFromStream(Stream assemblyStream, string virtualPath, [NotNullWhen(returnValue: true)] out AssemblyDef? assembly)
        {
            if (assemblies.TryGetValue(virtualPath, out assembly))
                return true;

            try
            {
                assembly = AssemblyDef.Load(assemblyStream, moduleCreationOptions);
                AssemblyResolver.AddToCache(assembly);
                LogSuccessLoad(virtualPath, assembly);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not load assembly from stream.");
                assembly = null;
                return false;
            }
        }

        public void UnloadAll()
        {
            AssemblyResolver.Clear();
            logger.LogDebug("Unloaded all assemblies.");
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void LogSuccessLoad(string path, AssemblyDef assembly)
        {
            logger.LogDebug("Loaded assembly: {assembly} from path: {path}.", assembly.FullName, path);
        }
    }
}