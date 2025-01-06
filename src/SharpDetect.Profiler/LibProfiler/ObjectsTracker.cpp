// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <exception>

#include "../lib/loguru/loguru.hpp"

#include "GarbageCollectionContext.h"
#include "ObjectsTracker.h"

namespace LibProfiler
{
    void ObjectsTracker::ProcessGarbageCollectionStarted(std::vector<BOOL>&& collectedGenerations, std::vector<COR_PRF_GC_GENERATION_RANGE>&& bounds)
    {
        std::lock_guard guard(_allocationMutex);
        _gcContext = GarbageCollectionContext(_allocations, std::move(collectedGenerations), std::move(bounds));
    }

    void ObjectsTracker::ProcessGarbageCollectionFinished()
    {
        std::lock_guard guard(_allocationMutex);
        _allocations = _gcContext.value().GetHeap();
        _gcContext = { };
        LOG_F(INFO, "GC finished. Currently tracked objects count=%lld.", _allocations.size());
    }

    void ObjectsTracker::ProcessSurvivingReferences(std::span<ObjectID> starts, std::span<SIZE_T> lengths)
    {
        std::lock_guard guard(_allocationMutex);
        _gcContext.value().ProcessSurvivingReferences(starts, lengths);
    }

    void ObjectsTracker::ProcessMovingReferences(std::span<ObjectID> oldStarts, std::span<ObjectID> newStarts, std::span<SIZE_T> lengths)
    {
        std::lock_guard guard(_allocationMutex);
        _gcContext.value().ProcessMovingReferences(oldStarts, newStarts, lengths);
    }

    const TrackedObjectId ObjectsTracker::GetTrackedObject(ObjectID objectId)
    {
        std::lock_guard guard(_allocationMutex);
        auto const it = _allocations.find(objectId);
        if (it != _allocations.cend())
            return (*it).second;

        static TrackedObjectId lastAssignedTrackedObjectId = 1;
        auto const newTrackedObjectId = lastAssignedTrackedObjectId++;
        _allocations.emplace(objectId, newTrackedObjectId);
        return newTrackedObjectId;
    }

    const UINT ObjectsTracker::GetTrackedObjectsCount()
    {
        std::lock_guard guard(_allocationMutex);
        return _allocations.size();
    }
}