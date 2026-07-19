// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <string>
#include <vector>

#include "doctest.h"

#include "EventLane.h"

using LibIPC::EventLane;

namespace
{
	std::string Consume(EventLane& lane)
	{
		std::vector<char> out;
		lane.ConsumeInto(out);
		return std::string(out.begin(), out.end());
	}
}

TEST_CASE("EventLane header size is u32 length + u64 sequence")
{
	CHECK(EventLane::RecordHeaderSize == sizeof(UINT32) + sizeof(UINT64));
}

TEST_CASE("EventLane starts empty and open")
{
	EventLane lane(1024);
	CHECK(lane.IsEmpty());
	CHECK_FALSE(lane.IsClosed());

	UINT64 sequence = 0;
	CHECK_FALSE(lane.TryPeekSequence(sequence));
}

TEST_CASE("EventLane round-trips a single record")
{
	EventLane lane(1024);
	const std::string payload = "hello world";
	REQUIRE(lane.HasSpaceFor(payload.size()));

	lane.Write(42, payload.data(), payload.size());
	CHECK_FALSE(lane.IsEmpty());

	UINT64 sequence = 0;
	REQUIRE(lane.TryPeekSequence(sequence));
	CHECK(sequence == 42);
	// Peeking does not consume
	CHECK_FALSE(lane.IsEmpty());

	CHECK(Consume(lane) == payload);
	CHECK(lane.IsEmpty());
}

TEST_CASE("EventLane preserves FIFO order across several records")
{
	EventLane lane(4096);
	for (UINT64 i = 0; i < 50; ++i)
	{
		const auto payload = "record-" + std::to_string(i);
		REQUIRE(lane.HasSpaceFor(payload.size()));
		lane.Write(i, payload.data(), payload.size());
	}

	for (UINT64 i = 0; i < 50; ++i)
	{
		UINT64 sequence = 0;
		REQUIRE(lane.TryPeekSequence(sequence));
		CHECK(sequence == i);
		CHECK(Consume(lane) == "record-" + std::to_string(i));
	}
	CHECK(lane.IsEmpty());
}

TEST_CASE("EventLane wraps records around the buffer edge")
{
	// Capacity 64, record = 12-byte header + 10-byte payload = 22 bytes
	EventLane lane(64);
	const std::string payload(10, 'x');
	for (UINT64 i = 0; i < 100; ++i)
	{
		REQUIRE(lane.HasSpaceFor(payload.size()));
		lane.Write(i, payload.data(), payload.size());

		UINT64 sequence = 0;
		REQUIRE(lane.TryPeekSequence(sequence));
		CHECK(sequence == i);
		CHECK(Consume(lane) == payload);
	}
	CHECK(lane.IsEmpty());
}

TEST_CASE("EventLane keeps distinct payloads intact when a record straddles the edge")
{
	EventLane lane(64);
	const std::string primer(6, 'p');
	for (int i = 0; i < 3; ++i)
	{
		lane.Write(0, primer.data(), primer.size());
		Consume(lane);
	}

	const std::string a(15, 'a');
	const std::string b(9, 'b');
	REQUIRE(lane.HasSpaceFor(a.size()));
	lane.Write(1, a.data(), a.size());
	// Consume the first before writing the second so the ring never overflows.
	CHECK(Consume(lane) == a);
	REQUIRE(lane.HasSpaceFor(b.size()));
	lane.Write(2, b.data(), b.size());
	CHECK(Consume(lane) == b);
}

TEST_CASE("EventLane HasSpaceFor accounts for the record header")
{
	EventLane lane(64);
	const std::string big(40, 'a');
	REQUIRE(lane.HasSpaceFor(big.size()));
	lane.Write(1, big.data(), big.size());

	// 12 bytes remain: not enough for another header, let alone a payload.
	CHECK_FALSE(lane.HasSpaceFor(big.size()));
	CHECK_FALSE(lane.HasSpaceFor(1));

	Consume(lane);
	CHECK(lane.HasSpaceFor(big.size()));
}

TEST_CASE("EventLane MarkClosed is observable but does not affect draining")
{
	EventLane lane(64);
	lane.Write(7, "ab", 2);
	CHECK_FALSE(lane.IsClosed());

	lane.MarkClosed();
	CHECK(lane.IsClosed());
	CHECK_FALSE(lane.IsEmpty()); // buffered record still drains

	CHECK(Consume(lane) == "ab");
	CHECK(lane.IsEmpty());
	CHECK(lane.IsClosed());
}
