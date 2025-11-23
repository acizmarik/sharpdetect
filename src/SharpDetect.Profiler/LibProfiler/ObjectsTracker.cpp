// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <exception>

#include "../lib/loguru/loguru.hpp"

#include "GarbageCollectionContext.h"
#include "ObjectsTracker.h"
#include "PAL.h"

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
        std::lock_guard<std::mutex> guard(_allocationMutex);
        auto const it = _allocations.find(objectId);
        if (it != _allocations.cend())
            return it->second;

        static TrackedObjectId lastAssignedTrackedObjectId = 1;
        auto const newTrackedObjectId = lastAssignedTrackedObjectId++;
        _allocations.emplace(objectId, newTrackedObjectId);
        return newTrackedObjectId;
    }

    UINT ObjectsTracker::GetTrackedObjectsCount() {
        std::lock_guard<std::mutex> guard(_allocationMutex);
        return _allocations.size();
    }
}