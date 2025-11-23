// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <mutex>
#include <optional>
#include <unordered_map>
#include <vector>

#include "GarbageCollectionContext.h"
#include "TrackedObjectId.h"

namespace LibProfiler
{
	class ObjectsTracker
	{
	public:
		ObjectsTracker()
			: _gcContext(), _currentObjectId({ }), _allocations({ })
		{

		}

		void ProcessGarbageCollectionStarted(std::vector<BOOL>&& collectedGenerations, std::vector<COR_PRF_GC_GENERATION_RANGE>&& bounds);
		GarbageCollectionContext ProcessGarbageCollectionFinished();
		void ProcessSurvivingReferences(std::span<ObjectID> starts, std::span<SIZE_T> lengths);
		void ProcessMovingReferences(std::span<ObjectID> oldStarts, std::span<ObjectID> newStarts, std::span<SIZE_T> lengths);
		const TrackedObjectId GetTrackedObject(ObjectID objectId);
		const UINT GetTrackedObjectsCount();

	private:
		std::optional<GarbageCollectionContext> _gcContext;
		std::atomic<TrackedObjectId> _currentObjectId;
		std::unordered_map<ObjectID, TrackedObjectId> _allocations;
		std::mutex _allocationMutex;
	};
}