using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Services.Instrumentation;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace SharpDetect.Dnlib.Extensions.Assembler
{
    public class StringHeapCache : IStringHeapCache
    {
        enum ModuleCacheState { Constructing, Ready }

        private readonly ConcurrentDictionary<ModuleDef, ImmutableDictionary<string, MDToken>> cache;
        private readonly ConcurrentDictionary<ModuleDef, (object Signaller, ModuleCacheState State)> signallers;
        private const int defaultCacheCapacity = 20;

        public StringHeapCache()
        {
            this.cache = new ConcurrentDictionary<ModuleDef, ImmutableDictionary<string, MDToken>>(concurrencyLevel: Environment.ProcessorCount, capacity: defaultCacheCapacity);
            this.signallers = new ConcurrentDictionary<ModuleDef, (object Signaller, ModuleCacheState State)>(concurrencyLevel: Environment.ProcessorCount, capacity: defaultCacheCapacity);
        }

        public MDToken GetStringOffset(ModuleDef module, string str)
        {
            EnsureCacheConstructedConstructCache(module);
            return cache[module][str];
        }

        internal ImmutableDictionary<string, MDToken> GetAllOffsets(ModuleDef module)
        {
            EnsureCacheConstructedConstructCache(module);
            return cache[module];
        }

        private void EnsureCacheConstructedConstructCache(ModuleDef module)
        {
            var (signaller, state) = signallers.GetOrAdd(module, (_) => (new object(), ModuleCacheState.Constructing));
            if (state == ModuleCacheState.Constructing)
            {
                lock (signaller)
                {
                    if (signallers.TryGetValue(module, out var newRecord) && newRecord.State == ModuleCacheState.Constructing)
                    {
                        // We are the ones that should construct the cache
                        var moduleDefMd = Guard.NotNull<ModuleDefMD, InvalidOperationException>(module as ModuleDefMD);
                        var stringsHeap = ParseStringsHeap(moduleDefMd);
                        // Update caches and ensure their integrity is intact
                        Guard.True<InvalidOperationException>(cache.TryAdd(module, stringsHeap));
                        Guard.True<InvalidOperationException>(signallers.TryUpdate(module, (signaller, ModuleCacheState.Ready), (signaller, ModuleCacheState.Constructing)));
                    }
                }
            }
        }

        private static ImmutableDictionary<string, MDToken> ParseStringsHeap(ModuleDefMD moduleDefMd)
        {
            var cacheBuilder = ImmutableDictionary.CreateBuilder<string, MDToken>();
            var reader = moduleDefMd.Metadata.USStream.CreateReader();

            // #US heap does not need to be present
            if (moduleDefMd.USStream.StreamLength == 0)
                return cacheBuilder.ToImmutableDictionary();

            // Heap starts with a 0x00
            reader.CurrentOffset++;

            // Iterate through whole #US heap
            while (reader.CanRead(sizeof(uint)))
            {
                var offset = reader.CurrentOffset;
                var length = GetNextStringLength(ref reader);
                var str = reader.ReadString((int)length, System.Text.Encoding.Unicode);
                if (str != string.Empty)
                    cacheBuilder.Add(str, new MDToken((Table)0x70, offset - reader.StartOffset));
                //reader.CurrentOffset++;
            }

            return cacheBuilder.ToImmutableDictionary();
        }

        private static uint GetNextStringLength(ref DataReader reader)
        {
            var size = reader.ReadCompressedUInt32();
            //if (size < 127)
            //{
            //    // Compressed uint to 1 byte
            //    reader.CurrentOffset++;
            //}
            //else if (size < 0x3FFF)
            //{
            //    // Compressed uint to 2 bytes
            //    reader.CurrentOffset += 2;
            //}
            //else
            //{
            //    // Compressed uint to 4 bytes
            //    reader.CurrentOffset += 4;
            //}

            return size & ~1u;
        }
    }
}
