// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>

#include "../lib/loguru/loguru.hpp"

#include "GarbageCollectionContext.h"

LibProfiler::GarbageCollectionContext::GarbageCollectionContext(const std::unordered_map<ObjectID, TrackedObjectId>& oldHeap, std::vector<BOOL>&& generationsCollected, std::vector<COR_PRF_GC_GENERATION_RANGE>&& bounds)	:
	_newHeapBuilder({ }), 
	_previousTrackedObjects({ }),
	_nextTrackedObjects({ }),
	_previousSortedHeap(std::vector<std::tuple<ObjectID, TrackedObjectId>>(oldHeap.cbegin(), oldHeap.cend())), 
	_generationsCollected(std::move(generationsCollected)),
	_bounds(std::move(bounds))
{
	for (auto&& record : oldHeap)
	{
		auto trackedObjectId = record.second;
		_previousTrackedObjects.insert(trackedObjectId);
	}

	std::sort(std::begin(_previousSortedHeap), std::end(_previousSortedHeap),
		[](std::tuple<ObjectID, TrackedObjectId> const& t1, std::tuple<ObjectID, TrackedObjectId> const& t2)
		{ 
			return std::get<0>(t1) < std::get<0>(t2); 
		});

	/* All generations that are not being collected automatically survived */
	for (auto&& bound : _bounds)
	{
		if (_generationsCollected[bound.generation])
		{
			// We need to resolve Surviving / Moving refernces notifications for this GC
			continue;
		}

		ProcessSurvivingReferences(tcb::span<ObjectID>(&bound.rangeStart, 1), tcb::span<SIZE_T>(&bound.rangeLength, 1));
	}
}

void LibProfiler::GarbageCollectionContext::ProcessSurvivingReferences(tcb::span<ObjectID> starts, tcb::span<SIZE_T> lengths)
{
	for (size_t index = 0; index < starts.size(); index++) 
	{
		auto start = starts[index];
		auto length = lengths[index];
		
		// Find element in segment that is closest to the segment start
		auto startIndex = BinarySearch(start);
		if (startIndex < 0)
			startIndex = ~startIndex;

		// Find element in segment that is closest to the segment end
		auto endIndex = BinarySearch(start + length - 1);
		if (endIndex < 0)
			endIndex = ~endIndex;

		for (size_t survivingIndex = startIndex; survivingIndex < endIndex; survivingIndex++)
		{
			// All objects that matched are surviving
			ObjectID currentObjectId;
			TrackedObjectId currentTrackedObjectId;
			std::tie(currentObjectId, currentTrackedObjectId) = _previousSortedHeap[survivingIndex];
			_newHeapBuilder.insert(std::make_pair(currentObjectId, currentTrackedObjectId));
			_nextTrackedObjects.insert(currentTrackedObjectId);
		}
	}
}

void LibProfiler::GarbageCollectionContext::ProcessMovingReferences(tcb::span<ObjectID> oldStarts, tcb::span<ObjectID> newStarts, tcb::span<SIZE_T> lengths)
{
	for (size_t index = 0; index < oldStarts.size(); index++)
	{
		auto const newStart = newStarts[index];
		auto const oldStart = oldStarts[index];
		auto const length = lengths[index];

		// Find element in segment that is closest to the segment start
		auto startIndex = BinarySearch(oldStart);
		if (startIndex < 0)
			startIndex = ~startIndex;

		// Find element in segment that is closest to the segment end
		auto endIndex = BinarySearch(oldStart + length - 1);
		if (endIndex < 0)
			endIndex = ~endIndex;

		for (size_t survivingIndex = startIndex; survivingIndex < endIndex; survivingIndex++)
		{
			// All objects that matched are surviving
			ObjectID currentObjectId;
			TrackedObjectId currentTrackedObjectId;
			std::tie(currentObjectId, currentTrackedObjectId) = _previousSortedHeap[survivingIndex];
			auto const newStartingIndex = newStart + (currentObjectId - oldStart);
			_newHeapBuilder.insert(std::make_pair(newStartingIndex, currentTrackedObjectId));
			_nextTrackedObjects.insert(currentTrackedObjectId);
		}
	}
}

INT LibProfiler::GarbageCollectionContext::BinarySearch(ObjectID objId)
{
	if (_previousSortedHeap.empty())
		return ~0;

	INT leftIndex = 0;
	INT rightIndex = _previousSortedHeap.size() - 1;
	while (leftIndex <= rightIndex)
	{
		auto middleIndex = (leftIndex + rightIndex) / 2;
		ObjectID middleValue;
		std::tie(middleValue, std::ignore) = _previousSortedHeap[middleIndex];

		if (middleValue == objId)
			return middleIndex;
		else if (middleValue < objId)
			leftIndex = middleIndex + 1;
		else if (middleValue > objId)
			rightIndex = middleIndex - 1;
	}

	return ~leftIndex;
}
