using Microsoft.Extensions.Logging;
using GcGenerationRange = SharpDetect.Common.Interop.COR_PRF_GC_GENERATION_RANGE;

namespace SharpDetect.Core.Runtime.Memory
{
    internal class ShadowGC
    {
        public volatile int GarbageCollectionsCount = 0;
        private readonly ShadowMemory memory;

        public ShadowGC(ILoggerFactory loggerFactory)
        {
            memory = new(loggerFactory);
        }

        public void ProcessGarbageCollectionStarted(GcGenerationRange[] ranges, bool[] generationsCollected)
        {
            memory.Reconstruct(ranges);
            memory.StartGarbageCollection(generationsCollected);
        }

        public void ProcessGarbageCollectionFinished(GcGenerationRange[] ranges)
        {
            memory.FinishGarbageCollection();
            memory.PromoteSurvivors();
            memory.Reconstruct(ranges);
        }

        public void ProcessSurvivingReferences(UIntPtr[] survivingBlockStarts, UIntPtr[] lengths)
        {
            var generation = memory.GetGeneration(survivingBlockStarts[0]);
            memory.Collect(generation, survivingBlockStarts, lengths);
        }

        public void ProcessMovedReferences(UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, UIntPtr[] lengths)
        {
            var generation = memory.GetGeneration(oldBlockStarts[0]);
            memory.Collect(generation, oldBlockStarts, newBlockStarts, lengths);
        }

        public ShadowObject GetObject(UIntPtr pointer)
            => memory.GetOrTrack(pointer);
    }
}
