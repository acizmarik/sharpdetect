// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Runtime;
using GcGenerationRange = SharpDetect.Common.Interop.COR_PRF_GC_GENERATION_RANGE;

namespace SharpDetect.Core.Runtime.Memory
{
    internal class ShadowGC
    {
        public volatile int GarbageCollectionsCount = 0;
        private readonly ShadowMemory memory;
        private readonly ILogger logger;

        public ShadowGC(ILoggerFactory loggerFactory)
        {
            memory = new(loggerFactory);
            logger = loggerFactory.CreateLogger<ShadowGC>();
        }

        public void ProcessGarbageCollectionStarted(GcGenerationRange[] ranges, bool[] generationsCollected)
        {
            logger.LogDebug("GC started for generations {gens}", generationsCollected);
            memory.PrepareForGarbageCollection(generationsCollected);
            memory.Reconstruct(ranges);
            memory.StartGarbageCollection();
        }

        public void ProcessGarbageCollectionFinished(GcGenerationRange[] ranges)
        {
            memory.FinishGarbageCollection();
            memory.Reconstruct(ranges);
            memory.PromoteSurvivors();
            memory.TearDownGarbageCollection();

            var gen0Size = memory.GetGenerationSize(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0);
            var gen1Size = memory.GetGenerationSize(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_1);
            var gen2Size = memory.GetGenerationSize(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_2);
            var lohSize = memory.GetGenerationSize(COR_PRF_GC_GENERATION.COR_PRF_GC_LARGE_OBJECT_HEAP);
            var pohSize = memory.GetGenerationSize(COR_PRF_GC_GENERATION.COR_PRF_GC_PINNED_OBJECT_HEAP);

            logger.LogDebug("GC finished. Live & tracked objects: [G0={g0},G1={g1},G2={g2},LOH={loh},POH={poh}]", 
                gen0Size, gen1Size, gen2Size, lohSize, pohSize);
        }

        public void ProcessSurvivingReferences(UIntPtr[] survivingBlockStarts, uint[] lengths)
        {
            var generation = memory.GetGeneration(survivingBlockStarts[0]);
            memory.Collect(generation, survivingBlockStarts, lengths);
        }

        public void ProcessMovedReferences(UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, uint[] lengths)
        {
            var generation = memory.GetGeneration(oldBlockStarts[0]);
            memory.Collect(generation, oldBlockStarts, newBlockStarts, lengths);
        }

        public ShadowObject GetObject(UIntPtr pointer)
            => memory.GetOrTrack(pointer);

        public COR_PRF_GC_GENERATION GetObjectGeneration(IShadowObject obj)
            => memory.GetGeneration(obj.ShadowPointer);
    }
}
