// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using IntervalTree;
using Microsoft.Extensions.Logging;
using SharpDetect.Common.Exceptions;
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
        private bool[]? generationsCollected;
        private bool[]? compactingCollections;

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

        public void PrepareForGarbageCollection(bool[] generationsCollected)
        {
            this.generationsCollected = generationsCollected;
            this.compactingCollections = new bool[generationsCollected.Length];
        }

        public void TearDownGarbageCollection()
        {
            this.generationsCollected = null;
            this.compactingCollections = null;
        }

        public void StartGarbageCollection()
        {
            // Pre-mark all affected objects as dead
            // During SurvivingReferences or MovedReferences, we will correct this
            RuntimeContract.Assert(generationsCollected != null);
            RuntimeContract.Assert(compactingCollections != null);

            for (var genIndex = 0; genIndex < generationsCollected!.Length; genIndex++)
            {
                if (!generationsCollected![genIndex])
                {
                    // All generations not affected by GC are automatically surviving
                    // There is no action necessary to perform for this generation
                    continue;
                }

                var trackedObjects = objectsLookup[(GcGeneration)genIndex];
                foreach (var (_, trackedObject) in trackedObjects)
                    trackedObject.IsAlive = false;
            }
        }

        public void FinishGarbageCollection()
        {
            // Remove all dead objects
            RuntimeContract.Assert(generationsCollected != null);
            RuntimeContract.Assert(compactingCollections != null);

            for (var genIndex = 0; genIndex < generationsCollected.Length; genIndex++)
            {
                if (!generationsCollected[genIndex])
                {
                    // All generations not affected by GC are automatically surviving
                    // There is no action necessary to perform for this generation
                    continue;
                }

                var isCompacting = compactingCollections[genIndex];
                var collected = objectsLookup[(GcGeneration)genIndex];

                logger.LogDebug("GC {type} on {gen} with {total} tracked objects",
                    (isCompacting) ? "compacting" : "non-compacting", (GcGeneration)genIndex, collected.Count);

                if (!isCompacting)
                {
                    // GC was non-compacting
                    // We must just remove all dead objects
                    objectsLookup[(GcGeneration)genIndex] = new ConcurrentDictionary<UIntPtr, ShadowObject>(
                        collection: collected.Where(e => e.Value.IsAlive));
                }
                else
                {
                    // GC was compacting
                    // We must rebuild the object lookup
                    objectsLookup[(GcGeneration)genIndex] = new ConcurrentDictionary<UIntPtr, ShadowObject>(
                        collection: collected.Where(e => e.Value.IsAlive)
                            .Select(record => new KeyValuePair<UIntPtr, ShadowObject>(
                                key: record.Value.ShadowPointer,
                                value: record.Value)));
                }
            }

            // TODO: notify plugins about generation sizes
            // TODO: notify plugins about how many objects were collected
        }

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

        public void Collect(GcGeneration generation, UIntPtr[] survivingBlockStarts, uint[] lengths)
        {
            var toCollect = objectsLookup[generation];
            var intervalTree = new IntervalTree<UIntPtr, bool>();
            for (var i = 0; i < survivingBlockStarts.Length; i++)
                intervalTree.Add(survivingBlockStarts[i], new UIntPtr(survivingBlockStarts[i].ToUInt64() + lengths[i] - 1), true);

            // Mark all surviving objects as alive
            foreach (var (pointer, shadowObj) in toCollect)
            {
                if (intervalTree.QuerySingle(pointer))
                {
                    // This object is surviving
                    shadowObj.IsAlive = true;
                    continue;
                }
            }
        }

        public void Collect(GcGeneration generation, UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, uint[] lengths)
        {
            compactingCollections![(int)generation] = true;
            var toCollect = objectsLookup[generation];
            var intervalTree = new IntervalTree<UIntPtr, uint?>();
            for (var i = 0u; i < oldBlockStarts.Length; i++)
                intervalTree.Add(oldBlockStarts[i], new UIntPtr(oldBlockStarts[i].ToUInt64() + lengths[i] - 1), i);

            // Mark all surviving objects as alive and calculate their new locations
            foreach (var (pointer, shadowObj) in toCollect)
            {
                var blockIndex = intervalTree.QuerySingle(pointer);
                if (blockIndex != null)
                {
                    if (shadowObj.IsAlive)
                        continue;

                    // This object is surviving and might have been moved
                    var offset = pointer - oldBlockStarts[blockIndex.Value];
                    var newPointer = newBlockStarts[blockIndex.Value] + offset;
                    shadowObj.ShadowPointer = newPointer;
                    shadowObj.IsAlive = true;
                }
            }
        }

        public void PromoteSurvivors()
        {
            void PromoteImpl(GcGeneration from, GcGeneration to)
            {
                if (generationsCollected![(int)from])
                {
                    var generationFrom = objectsLookup![from];
                    var generationTo = objectsLookup![to];
                    foreach (var (ptr, obj) in generationFrom.Where(r => GetGeneration(r.Key) != from))
                    {
                        generationFrom.Remove(ptr, out _);
                        generationTo.TryAdd(ptr, obj);
                    }
                }
            }

            // Promotion is not relevant to other heap segments
            // There is nowhere to promote objects from LOH, pinned heap, nor the 2nd generation
            PromoteImpl(GcGeneration.COR_PRF_GC_GEN_1, GcGeneration.COR_PRF_GC_GEN_2);
            PromoteImpl(GcGeneration.COR_PRF_GC_GEN_0, GcGeneration.COR_PRF_GC_GEN_1);

            this.compactingCollections = null;
            this.generationsCollected = null;
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

        public int GetGenerationSize(GcGeneration generation)
            => objectsLookup[generation].Count;
    }
}
