// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <atomic>
#include <stdexcept>
#include <thread>
#include <vector>

#include "doctest.h"

#include "MethodBodyPatchStates.h"

using LibProfiler::MethodBodyPatchStates;

namespace
{
	constexpr std::size_t MethodDefsCount = 2;
	constexpr mdMethodDef Method = 0x06000001;
	constexpr mdMethodDef OtherMethod = 0x06000002;
	constexpr mdMethodDef AddedMethod = 0x06000003;
	constexpr mdMethodDef MethodWithHighestRid = 0x06FFFFFF;

	void Patch(MethodBodyPatchStates& states, const mdMethodDef methodDef)
	{
		auto claim = states.TryClaim(methodDef);
		REQUIRE(claim.has_value());
		claim->MarkPatched();
	}
}

TEST_CASE("MethodBodyPatchStates: a patched method is not claimed again")
{
	MethodBodyPatchStates states(MethodDefsCount);

	Patch(states, Method);

	CHECK_FALSE(states.TryClaim(Method).has_value());
}

TEST_CASE("MethodBodyPatchStates: a method whose patching failed is claimed again")
{
	MethodBodyPatchStates states(MethodDefsCount);

	CHECK(states.TryClaim(Method).has_value());

	CHECK(states.TryClaim(Method).has_value());
}

TEST_CASE("MethodBodyPatchStates: a claim abandoned by an exception can be claimed again")
{
	MethodBodyPatchStates states(MethodDefsCount);

	try
	{
		auto claim = states.TryClaim(Method);
		throw std::runtime_error("patching failed");
	}
	catch (const std::runtime_error&)
	{
	}

	CHECK(states.TryClaim(Method).has_value());
}

TEST_CASE("MethodBodyPatchStates: methods are claimed independently")
{
	MethodBodyPatchStates states(MethodDefsCount);

	auto claim = states.TryClaim(Method);
	auto otherClaim = states.TryClaim(OtherMethod);
	auto highestRidClaim = states.TryClaim(MethodWithHighestRid);
	REQUIRE(claim.has_value());
	REQUIRE(otherClaim.has_value());
	REQUIRE(highestRidClaim.has_value());
	claim->MarkPatched();
	otherClaim->MarkPatched();
	highestRidClaim->MarkPatched();
	claim.reset();
	otherClaim.reset();
	highestRidClaim.reset();

	CHECK_FALSE(states.TryClaim(Method).has_value());
	CHECK_FALSE(states.TryClaim(OtherMethod).has_value());
	CHECK_FALSE(states.TryClaim(MethodWithHighestRid).has_value());
}

TEST_CASE("MethodBodyPatchStates: a method added after the count was taken is patched once")
{
	MethodBodyPatchStates states(MethodDefsCount);

	Patch(states, AddedMethod);

	CHECK_FALSE(states.TryClaim(AddedMethod).has_value());
	CHECK(states.TryClaim(Method).has_value());
}

TEST_CASE("MethodBodyPatchStates: methods of a module whose methods could not be counted are patched once")
{
	MethodBodyPatchStates states(0);

	Patch(states, Method);
	Patch(states, OtherMethod);

	CHECK_FALSE(states.TryClaim(Method).has_value());
	CHECK_FALSE(states.TryClaim(OtherMethod).has_value());
}

TEST_CASE("MethodBodyPatchStates: concurrent claimers wait until the winner finishes patching")
{
	constexpr auto threadsCount = 8;
	MethodBodyPatchStates states(MethodDefsCount);
	std::atomic<int> winners { 0 };
	std::atomic<bool> isPatched { false };
	std::atomic<int> losersSeeingUnpatchedBody { 0 };

	std::vector<std::thread> threads;
	for (auto i = 0; i < threadsCount; i++)
	{
		threads.emplace_back([&]
		{
			if (auto claim = states.TryClaim(Method))
			{
				winners.fetch_add(1);
				std::this_thread::yield();
				isPatched.store(true, std::memory_order_relaxed);
				claim->MarkPatched();
			}
			else if (!isPatched.load(std::memory_order_relaxed))
			{
				losersSeeingUnpatchedBody.fetch_add(1);
			}
		});
	}

	for (auto& thread : threads)
		thread.join();

	CHECK(winners.load() == 1);
	CHECK(losersSeeingUnpatchedBody.load() == 0);
}

TEST_CASE("MethodBodyPatchStates: a waiting claimer takes over a claim abandoned without patching")
{
	MethodBodyPatchStates states(MethodDefsCount);
	auto claim = states.TryClaim(Method);
	REQUIRE(claim.has_value());

	std::atomic<bool> isTakenOver { false };
	std::thread waiter([&]
	{
		isTakenOver.store(states.TryClaim(Method).has_value());
	});
	claim.reset();
	waiter.join();

	CHECK(isTakenOver.load());
}
