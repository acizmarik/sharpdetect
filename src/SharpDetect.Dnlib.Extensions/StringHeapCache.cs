// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CommunityToolkit.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.IO;
using SharpDetect.Common.Services.Instrumentation;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace SharpDetect.Dnlib.Extensions
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
                        var moduleDefMd = module as ModuleDefMD;
                        Guard.IsNotNull(moduleDefMd);

                        // Update caches and ensure their integrity is intact
                        var stringsHeap = ParseStringsHeap(moduleDefMd);
                        if (!cache.TryAdd(module, stringsHeap))
                            throw new InvalidOperationException("Could not update cache");
                        if (!signallers.TryUpdate(module, (signaller, ModuleCacheState.Ready), (signaller, ModuleCacheState.Constructing)))
                            throw new InvalidOperationException("Could not update signallers about cache state");
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
                // Get string length
                var length = GetNextStringLength(ref reader);
                // Read string
                var str = reader.ReadString((int)length, System.Text.Encoding.Unicode);
                // Read null-terminator
                reader.ReadByte();
                // Create a new string record
                cacheBuilder.Add(str, new MDToken((Table)0x70, offset - reader.StartOffset));
            }

            return cacheBuilder.ToImmutableDictionary();
        }

        private static uint GetNextStringLength(ref DataReader reader)
        {
            return reader.ReadCompressedUInt32() & ~1u;
        }
    }
}
