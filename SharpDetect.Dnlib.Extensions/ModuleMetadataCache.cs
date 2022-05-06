using dnlib.DotNet;
using dnlib.DotNet.Writer;
using System.Collections.Concurrent;

namespace SharpDetect.Dnlib.Extensions
{
    public sealed class ModuleMetadataCache
    {
        private readonly ConcurrentDictionary<ModuleDef, Metadata> cache = new();
        private const uint DefaultDotNetResourceAlignment = 4;

        public Metadata GetMetadata(ModuleDef module)
        {
            if (!cache.ContainsKey(module))
            {
                lock (module)
                {
                    if (!cache.ContainsKey(module))
                        InitModuleMetadata(module);
                }
            }

            return cache[module];
        }

        private void InitModuleMetadata(ModuleDef module)
        {
            var constants = new UniqueChunkList<ByteArrayChunk>();
            var methodBodies = new MethodBodyChunks(false);
            var netResources = new NetResources(DefaultDotNetResourceAlignment);
            var options = new MetadataOptions() { Flags = MetadataFlags.PreserveAll };
            var moduleMetadata = Metadata.Create(module, constants, methodBodies, netResources, options);
            moduleMetadata.CreateTables();

            cache.TryAdd(module, moduleMetadata);
        }
    }
}
