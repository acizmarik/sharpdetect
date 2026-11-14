// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <unordered_map>
#include <vector>

#include "doctest.h"

#include "GarbageCollectionContext.h"

using LibProfiler::GarbageCollectionContext;
using LibProfiler::TrackedObjectId;

namespace
{
	constexpr ObjectID Gen0Start = 0x10000;
	constexpr ObjectID Gen2Start = 0x20000;
	constexpr UINT_PTR RangeLength = 0x1000;
	constexpr UINT_PTR RangeLengthReserved = 0x4000;

	std::vector<COR_PRF_GC_GENERATION_RANGE> CreateBounds()
	{
		return
		{
			{ COR_PRF_GC_GEN_0, Gen0Start, RangeLength, RangeLengthReserved },
			{ COR_PRF_GC_GEN_2, Gen2Start, RangeLength, RangeLengthReserved }
		};
	}

	GarbageCollectionContext CreateGen0Collection(const std::unordered_map<ObjectID, TrackedObjectId>& heap)
	{
		// Generations: 0, 1, 2, large object heap, pinned object heap
		return GarbageCollectionContext(heap, { TRUE, FALSE, FALSE, FALSE, FALSE }, CreateBounds());
	}
}

TEST_CASE("GarbageCollectionContext: an object outside every generation range keeps its id")
{
	constexpr ObjectID frozenObject = 0x90000;
	const std::unordered_map<ObjectID, TrackedObjectId> heap { { frozenObject, 7 } };

	const auto context = CreateGen0Collection(heap);

	CHECK(context.GetHeap().at(frozenObject) == 7);
	CHECK(context.GetNextTrackedObjects().contains(7));
}

TEST_CASE("GarbageCollectionContext: an unreported object of a collected generation is dropped")
{
	constexpr ObjectID deadObject = Gen0Start + 0x10;
	const std::unordered_map<ObjectID, TrackedObjectId> heap { { deadObject, 7 } };

	const auto context = CreateGen0Collection(heap);

	CHECK(context.GetHeap().empty());
	CHECK_FALSE(context.GetNextTrackedObjects().contains(7));
}

TEST_CASE("GarbageCollectionContext: an unreported object in the reserved part of a collected generation is dropped")
{
	constexpr ObjectID deadObject = Gen0Start + RangeLength + 0x10;
	const std::unordered_map<ObjectID, TrackedObjectId> heap { { deadObject, 7 } };

	const auto context = CreateGen0Collection(heap);

	CHECK(context.GetHeap().empty());
}

TEST_CASE("GarbageCollectionContext: an object of a generation that is not collected keeps its id")
{
	constexpr ObjectID oldObject = Gen2Start + 0x10;
	const std::unordered_map<ObjectID, TrackedObjectId> heap { { oldObject, 7 } };

	const auto context = CreateGen0Collection(heap);

	CHECK(context.GetHeap().at(oldObject) == 7);
}

TEST_CASE("GarbageCollectionContext: a moved object is re-keyed and a frozen one stays in place")
{
	constexpr ObjectID movedObject = Gen0Start + 0x10;
	constexpr ObjectID frozenObject = 0x90000;
	const std::unordered_map<ObjectID, TrackedObjectId> heap { { movedObject, 3 }, { frozenObject, 7 } };
	auto context = CreateGen0Collection(heap);

	ObjectID oldStart = Gen0Start;
	ObjectID newStart = Gen2Start + 0x100;
	SIZE_T length = 0x100;
	context.ProcessMovingReferences({ &oldStart, 1 }, { &newStart, 1 }, { &length, 1 });

	CHECK(context.GetHeap().size() == 2);
	CHECK(context.GetHeap().at(newStart + 0x10) == 3);
	CHECK(context.GetHeap().at(frozenObject) == 7);
}
