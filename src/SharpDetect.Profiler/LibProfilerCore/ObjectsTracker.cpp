// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <exception>

#include "../lib/loguru/loguru.hpp"

#include "GarbageCollectionContext.h"
#include "ObjectsTracker.h"
#include "PAL.h"

namespace
{
    constexpr std::size_t TrackedObjectCacheSize = 256;

    struct TrackedObjectCacheEntry
    {
        ObjectID objectId;
        LibProfiler::TrackedObjectId trackedObjectId;
    };

    // Per-thread direct-mapped lookup cache. Valid only within a single GC epoch.
    struct TrackedObjectCache
    {
        UINT64 epoch;
        const void* owner;
        TrackedObjectCacheEntry entries[TrackedObjectCacheSize];
    };

    thread_local TrackedObjectCache trackedObjectCache { };
}

namespace LibProfiler
{
    void ObjectsTracker::ProcessGarbageCollectionStarted(std::vector<BOOL>&& collectedGenerations, std::vector<COR_PRF_GC_GENERATION_RANGE>&& bounds)
    {
        std::lock_guard guard(_allocationMutex);
        _gcContext = GarbageCollectionContext(_allocations, std::move(collectedGenerations), std::move(bounds));
    }

    GarbageCollectionContext ObjectsTracker::ProcessGarbageCollectionFinished()
    {
        std::lock_guard guard(_allocationMutex);
        const auto gcPreviousTrackedObjectsCount = _gcContext.value().GetPreviousTrackedObjects().size();
        const auto gcNextTrackedObjectsCount = _gcContext.value().GetNextTrackedObjects().size();
        LOG_F(INFO, "GC removed %" SIZE_FORMAT " tracked objects.", gcPreviousTrackedObjectsCount - gcNextTrackedObjectsCount);

        _allocations = _gcContext.value().GetHeap();
        _gcEpoch.fetch_add(1, std::memory_order_release);
        auto gcContextCopy = std::move(_gcContext.value());
        _gcContext.reset();
        LOG_F(INFO, "GC finished. Currently tracked objects count=%" SIZE_FORMAT ".", _allocations.size());
        return gcContextCopy;
    }

    void ObjectsTracker::ProcessSurvivingReferences(const std::span<ObjectID> starts, const std::span<SIZE_T> lengths)
    {
        std::lock_guard guard(_allocationMutex);
        _gcContext.value().ProcessSurvivingReferences(starts, lengths);
    }

    void ObjectsTracker::ProcessMovingReferences(
        const std::span<ObjectID> oldStarts,
        const std::span<ObjectID> newStarts,
        const std::span<SIZE_T> lengths)
    {
        std::lock_guard guard(_allocationMutex);
        _gcContext.value().ProcessMovingReferences(oldStarts, newStarts, lengths);
    }

    TrackedObjectId ObjectsTracker::GetTrackedObject(ObjectID objectId) {
        auto& cache = trackedObjectCache;
        const auto epoch = _gcEpoch.load(std::memory_order_acquire);
        if (cache.epoch != epoch || cache.owner != this)
        {
            cache = { };
            cache.epoch = epoch;
            cache.owner = this;
        }

        // ObjectID 0 must not match the zero-initialized empty slots
        auto& entry = cache.entries[(objectId >> 3) & (TrackedObjectCacheSize - 1)];
        if (objectId != 0 && entry.objectId == objectId)
            return entry.trackedObjectId;

        std::lock_guard<std::mutex> guard(_allocationMutex);
        auto const it = _allocations.find(objectId);
        if (it != _allocations.cend())
        {
            if (objectId != 0)
                entry = { objectId, it->second };
            return it->second;
        }

        static TrackedObjectId lastAssignedTrackedObjectId = 1;
        auto const newTrackedObjectId = lastAssignedTrackedObjectId++;
        _allocations.emplace(objectId, newTrackedObjectId);
        if (objectId != 0)
            entry = { objectId, newTrackedObjectId };
        return newTrackedObjectId;
    }

    UINT ObjectsTracker::GetTrackedObjectsCount() {
        std::lock_guard<std::mutex> guard(_allocationMutex);
        return _allocations.size();
    }
}