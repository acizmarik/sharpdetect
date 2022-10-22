using IntervalTree;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using GcGeneration = SharpDetect.Common.Interop.COR_PRF_GC_GENERATION;
using GcGenerationRange = SharpDetect.Common.Interop.COR_PRF_GC_GENERATION_RANGE;

namespace SharpDetect.Core.Runtime.Memory
{
    internal class ShadowMemory
    {
        private readonly ILogger logger;
        private Dictionary<GcGeneration, ConcurrentDictionary<UIntPtr, ShadowObject>> objectsLookup;
        private IIntervalTree<UIntPtr, GcGeneration> memorySegments;

        public ShadowMemory(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<ShadowMemory>();
            var objectsLookupInternal = new Dictionary<GcGeneration, ConcurrentDictionary<UIntPtr, ShadowObject>>();
            foreach (var item in Enum.GetValues(typeof(GcGeneration)))
                objectsLookupInternal.Add((GcGeneration)item, new ConcurrentDictionary<UIntPtr, ShadowObject>());
            objectsLookup = objectsLookupInternal;

            memorySegments = new IntervalTree<UIntPtr, GcGeneration>();
        }

        public GcGeneration GetGeneration(UIntPtr pointer)
            => memorySegments.Query(pointer).FirstOrDefault();

        public void Reconstruct(GcGenerationRange[] ranges)
        {
            var newIntervalTree = new IntervalTree<UIntPtr, GcGeneration>();
            foreach (var range in ranges)
            {
                var start = range.rangeStart;
                var end = new UIntPtr(start.ToUInt64() + range.rangeLength.ToUInt64());
                newIntervalTree.Add(start, end, range.generation);
            }
            // Assign new memory segments lookup
            memorySegments = newIntervalTree;
        }

        public void Collect(GcGeneration generation, UIntPtr[] survivingBlockStarts, UIntPtr[] lengths)
        {
            logger.LogDebug("GC (non-compacting) for {generation}", generation);
            var toCollect = objectsLookup[generation];
            var intervalTree = new IntervalTree<UIntPtr, bool>();
            for (var i = 0; i < survivingBlockStarts.Length; i++)
                intervalTree.Add(survivingBlockStarts[i], new UIntPtr(survivingBlockStarts[i].ToUInt64() + lengths[i].ToUInt64() - 1), true);

            // Mark objects for removing
            var toRemove = new List<UIntPtr>();
            foreach (var (pointer, shadowObj) in toCollect)
            {
                if (intervalTree.QuerySingle(pointer))
                {
                    // This object is surviving
                    continue;
                }

                toRemove.Add(pointer);
                shadowObj.IsAlive = false;
            }

            // Remove objects
            foreach (var ptr in toRemove)
            {
                toCollect.Remove(ptr, out _);
            }
        }

        public void Collect(GcGeneration generation, UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, UIntPtr[] lengths)
        {
            logger.LogDebug("GC (compacting) for {generation}", generation);
            var toCollect = objectsLookup[generation];
            var intervalTree = new IntervalTree<UIntPtr, uint?>();
            for (var i = 0u; i < oldBlockStarts.Length; i++)
                intervalTree.Add(oldBlockStarts[i], new UIntPtr(oldBlockStarts[i].ToUInt64() + lengths[i].ToUInt64() - 1), i);

            // Mark objects for moving and deleting
            var toMove = new List<KeyValuePair<UIntPtr, ShadowObject>>();
            foreach (var (pointer, shadowObj) in toCollect)
            {
                var blockIndex = intervalTree.QuerySingle(pointer);
                if (blockIndex == null)
                {
                    // This object will be removed
                    shadowObj.IsAlive = false;
                    continue;
                }

                var offset = shadowObj.ShadowPointer.ToUInt64() - oldBlockStarts[blockIndex.Value].ToUInt64();
                var newPointer = new UIntPtr(newBlockStarts[blockIndex.Value].ToUInt64() + offset);
                toMove.Add(new KeyValuePair<UIntPtr, ShadowObject>(newPointer, shadowObj));
                shadowObj.ShadowPointer = newPointer;
            }

            // Create new collection by moving marked objects and dropping other
            var internalObjectLookup = objectsLookup;
            internalObjectLookup[generation] = new ConcurrentDictionary<UIntPtr, ShadowObject>(toMove);
        }

        public void PromoteSurvivors()
        {
            logger.LogDebug("GC promoting survived objects");
            var internalCollection = objectsLookup;
            var gen0 = internalCollection[GcGeneration.COR_PRF_GC_GEN_0];
            var gen1 = internalCollection[GcGeneration.COR_PRF_GC_GEN_1];
            var gen2 = internalCollection[GcGeneration.COR_PRF_GC_GEN_2];

            // Promotion is not relevant to other heap segments
            // There is nowhere to promote objects from LOH, pinned heap, nor the 2nd generation

            if (gen1.Count > 0)
            {
                // Promote survivors from generation 1 to generation 2
                var newGen2 = gen2.Concat(gen1);
                internalCollection[GcGeneration.COR_PRF_GC_GEN_2] = gen2 = new ConcurrentDictionary<UIntPtr, ShadowObject>(newGen2);
                internalCollection[GcGeneration.COR_PRF_GC_GEN_1] = gen1 = new ConcurrentDictionary<UIntPtr, ShadowObject>(/* empty */);
            }
            if (gen0.Count > 0)
            {
                // Promote survivors from generation 0 to generation 1
                if (gen1.Count == 0)
                {
                    // Reassign generations
                    internalCollection[GcGeneration.COR_PRF_GC_GEN_1] = gen1 = gen0;
                    internalCollection[GcGeneration.COR_PRF_GC_GEN_0] = gen0 = new ConcurrentDictionary<UIntPtr, ShadowObject>(/* empty */);
                }
                else
                {
                    // Merge generations
                    var newGen1 = gen1.Concat(gen0);
                    internalCollection[GcGeneration.COR_PRF_GC_GEN_1] = gen1 = new ConcurrentDictionary<UIntPtr, ShadowObject>(newGen1);
                    internalCollection[GcGeneration.COR_PRF_GC_GEN_0] = gen0 = new ConcurrentDictionary<UIntPtr, ShadowObject>(/* empty */);
                }
            }

            // TODO: notify plugins about generation sizes
            // TODO: notify plugins about how many objects were collected
        }

        public ShadowObject GetOrTrack(UIntPtr pointer)
        {
            var generation = objectsLookup[GetGeneration(pointer)];

            // Check if we are already tracking this object
            if (generation.TryGetValue(pointer, out var shadowObject))
                return shadowObject;

            // Create new object
            shadowObject = new ShadowObject() { ShadowPointer = pointer };

            // Try to assign 
            // Note: this is technically a race condition as other thread might assign it first
            // Solution: ensure that all threads use the same instance
            return generation.GetOrAdd(pointer, shadowObject);
        }
    }
}
