// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <set>

#include "doctest.h"

#include "ThreadIncarnations.h"

using LibProfiler::ThreadIncarnations;

namespace
{
	constexpr ThreadID ThreadA = 0x1000;
	constexpr ThreadID ThreadB = 0x2000;
}

TEST_CASE("ThreadIncarnations: a reused ThreadID gets a new incarnation")
{
	ThreadIncarnations incarnations;

	const auto first = incarnations.OnThreadCreated(ThreadA);
	CHECK(incarnations.GetOrAssign(ThreadA) == first);
	CHECK(incarnations.OnThreadDestroyed(ThreadA) == first);

	const auto second = incarnations.OnThreadCreated(ThreadA);
	CHECK(second != first);
	CHECK(incarnations.GetOrAssign(ThreadA) == second);
	CHECK(incarnations.OnThreadDestroyed(ThreadA) == second);
}

TEST_CASE("ThreadIncarnations: many threads reusing few ThreadIDs stay distinct")
{
	ThreadIncarnations incarnations;
	std::set<UINT64> seen;

	for (int i = 0; i < 300; ++i)
	{
		const ThreadID threadId = 0x1000 + (i % 3) * 0x100;
		const auto incarnation = incarnations.OnThreadCreated(threadId);
		CHECK(incarnation != ThreadIncarnations::NoIncarnation);
		seen.insert(incarnation);
		CHECK(incarnations.OnThreadDestroyed(threadId) == incarnation);
	}

	CHECK(seen.size() == 300);
	CHECK(incarnations.GetLiveThreadsCount() == 0);
}

TEST_CASE("ThreadIncarnations: concurrently live threads have distinct incarnations")
{
	ThreadIncarnations incarnations;

	const auto a = incarnations.OnThreadCreated(ThreadA);
	const auto b = incarnations.OnThreadCreated(ThreadB);
	CHECK(a != b);
	CHECK(incarnations.GetLiveThreadsCount() == 2);
}

TEST_CASE("ThreadIncarnations: an incarnation assigned before ThreadCreated is kept")
{
	ThreadIncarnations incarnations;

	const auto early = incarnations.GetOrAssign(ThreadA);
	CHECK(incarnations.OnThreadCreated(ThreadA) == early);
}

TEST_CASE("ThreadIncarnations: ThreadID 0 has no incarnation")
{
	ThreadIncarnations incarnations;

	CHECK(incarnations.OnThreadCreated(0) == ThreadIncarnations::NoIncarnation);
	CHECK(incarnations.GetOrAssign(0) == ThreadIncarnations::NoIncarnation);
	CHECK(incarnations.Resolve(0, ThreadA, 7) == ThreadIncarnations::NoIncarnation);
	CHECK(incarnations.OnThreadDestroyed(0) == ThreadIncarnations::NoIncarnation);
	CHECK(incarnations.GetLiveThreadsCount() == 0);
}

TEST_CASE("ThreadIncarnations: destroying an unknown thread does not reuse an incarnation")
{
	ThreadIncarnations incarnations;

	const auto a = incarnations.OnThreadCreated(ThreadA);
	const auto unknown = incarnations.OnThreadDestroyed(ThreadB);
	CHECK(unknown != ThreadIncarnations::NoIncarnation);
	CHECK(unknown != a);
	CHECK(incarnations.GetOrAssign(ThreadA) == a);
}

TEST_CASE("ThreadIncarnations: Resolve prefers the live mapping")
{
	ThreadIncarnations incarnations;

	const auto a = incarnations.OnThreadCreated(ThreadA);
	CHECK(incarnations.Resolve(ThreadA, 0, ThreadIncarnations::NoIncarnation) == a);
	CHECK(incarnations.Resolve(ThreadA, ThreadB, 42) == a);
}

TEST_CASE("ThreadIncarnations: a thread running after its own ThreadDestroyed keeps its incarnation")
{
	ThreadIncarnations incarnations;

	const auto a = incarnations.OnThreadCreated(ThreadA);
	CHECK(incarnations.OnThreadDestroyed(ThreadA) == a);

	CHECK(incarnations.Resolve(ThreadA, ThreadA, a) == a);
	CHECK(incarnations.GetLiveThreadsCount() == 0);
	CHECK(incarnations.OnThreadCreated(ThreadA) != a);
}

TEST_CASE("ThreadIncarnations: Resolve assigns an unreported thread once")
{
	ThreadIncarnations incarnations;

	const auto a = incarnations.Resolve(ThreadA, 0, ThreadIncarnations::NoIncarnation);
	CHECK(a != ThreadIncarnations::NoIncarnation);
	CHECK(incarnations.Resolve(ThreadA, 0, ThreadIncarnations::NoIncarnation) == a);
	CHECK(incarnations.OnThreadCreated(ThreadA) == a);
}

TEST_CASE("ThreadIncarnations: GetLiveThreadIds skips dead incarnations and keeps positions")
{
	ThreadIncarnations incarnations;

	const auto dead = incarnations.OnThreadCreated(ThreadA);
	CHECK(incarnations.OnThreadDestroyed(ThreadA) == dead);
	const auto b = incarnations.OnThreadCreated(ThreadB);
	const auto a = incarnations.OnThreadCreated(ThreadA);

	std::vector<ThreadID> threadIds;
	std::vector<size_t> positions;
	incarnations.GetLiveThreadIds({ dead, b, 12345, a }, threadIds, positions);

	CHECK(threadIds == std::vector<ThreadID>{ ThreadB, ThreadA });
	CHECK(positions == std::vector<size_t>{ 1, 3 });
}

TEST_CASE("ScatterToPositions: puts each value back at its position")
{
	const auto scattered = LibProfiler::ScatterToPositions(std::vector<int>{ 20, 40 }, { 1, 3 }, 4);

	CHECK(scattered == std::vector<int>{ 0, 20, 0, 40 });
}

TEST_CASE("ThreadIncarnations: maps live incarnations back to ThreadIDs")
{
	ThreadIncarnations incarnations;

	const auto a = incarnations.OnThreadCreated(ThreadA);
	REQUIRE(incarnations.TryGetThreadId(a).has_value());
	CHECK(*incarnations.TryGetThreadId(a) == ThreadA);
	CHECK_FALSE(incarnations.TryGetThreadId(ThreadIncarnations::NoIncarnation).has_value());

	CHECK(incarnations.OnThreadDestroyed(ThreadA) == a);
	CHECK_FALSE(incarnations.TryGetThreadId(a).has_value());

	const auto reused = incarnations.OnThreadCreated(ThreadA);
	CHECK_FALSE(incarnations.TryGetThreadId(a).has_value());
	CHECK(*incarnations.TryGetThreadId(reused) == ThreadA);
}
