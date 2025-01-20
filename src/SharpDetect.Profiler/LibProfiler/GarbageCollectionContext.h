// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <algorithm>
#include <unordered_map>
#include <unordered_set>
#include <tuple>
#include <vector>

#include "cor.h"
#include "corprof.h"
#include "../lib/span/include/tcb/span.hpp"

#include "TrackedObjectId.h"

namespace LibProfiler
{
	class GarbageCollectionContext
	{
	public:
		GarbageCollectionContext(const std::unordered_map<ObjectID, TrackedObjectId>& oldHeap, std::vector<BOOL>&& generationsCollected, std::vector<COR_PRF_GC_GENERATION_RANGE>&& bounds);

		void ProcessSurvivingReferences(tcb::span<ObjectID> starts, tcb::span<SIZE_T> lengths);
		void ProcessMovingReferences(tcb::span<ObjectID> oldStarts, tcb::span<ObjectID> newStarts, tcb::span<SIZE_T> lengths);

		const std::unordered_map<ObjectID, TrackedObjectId>& GetHeap() const { return _newHeapBuilder; }
		const std::unordered_set<TrackedObjectId>& GetPreviousTrackedObjects() const { return _previousTrackedObjects; }
		const std::unordered_set<TrackedObjectId>& GetNextTrackedObjects() const { return _nextTrackedObjects; }

	private:

		INT BinarySearch(ObjectID objId);
		std::unordered_map<ObjectID, TrackedObjectId> _newHeapBuilder;
		std::unordered_set<TrackedObjectId> _previousTrackedObjects;
		std::unordered_set<TrackedObjectId> _nextTrackedObjects;
		std::vector<std::tuple<ObjectID, TrackedObjectId>> _previousSortedHeap;
		std::vector<COR_PRF_GC_GENERATION_RANGE> _bounds;
		std::vector<BOOL> _generationsCollected;
	};
}