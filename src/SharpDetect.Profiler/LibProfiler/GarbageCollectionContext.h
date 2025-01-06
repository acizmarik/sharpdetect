﻿// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <algorithm>
#include <unordered_map>
#include <span>
#include <tuple>
#include <vector>

#include "cor.h"
#include "corprof.h"

#include "TrackedObjectId.h"

namespace LibProfiler
{
	class GarbageCollectionContext
	{
	public:
		GarbageCollectionContext(const std::unordered_map<ObjectID, TrackedObjectId>& oldHeap, std::vector<BOOL>&& generationsCollected, std::vector<COR_PRF_GC_GENERATION_RANGE>&& bounds);

		void ProcessSurvivingReferences(std::span<ObjectID> starts, std::span<SIZE_T> lengths);
		void ProcessMovingReferences(std::span<ObjectID> oldStarts, std::span<ObjectID> newStarts, std::span<SIZE_T> lengths);

		const std::unordered_map<ObjectID, TrackedObjectId>& GetHeap() const { return _newHeapBuilder; }

	private:

		INT BinarySearch(ObjectID objId);
		std::unordered_map<ObjectID, TrackedObjectId> _newHeapBuilder;
		std::vector<std::tuple<ObjectID, TrackedObjectId>> _previousSortedHeap;
		std::vector<COR_PRF_GC_GENERATION_RANGE> _bounds;
		std::vector<BOOL> _generationsCollected;
	};
}