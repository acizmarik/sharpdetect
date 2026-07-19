// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstring>
#include <mutex>
#include <thread>
#include <vector>

#include "doctest.h"

#include "EventDispatcher.h"
#include "EventSink.h"

using LibIPC::EventDispatcher;

namespace
{
	class RecordingSink : public LibIPC::IEventSink
	{
	public:
		void Send(std::vector<char>& buffer) override
		{
			std::lock_guard guard(_mutex);
			_records.emplace_back(buffer.begin(), buffer.end());
		}

		std::vector<std::vector<char>> Records()
		{
			std::lock_guard guard(_mutex);
			return _records;
		}

	private:
		std::mutex _mutex;
		std::vector<std::vector<char>> _records;
	};

	std::vector<char> MakePayload(const std::int32_t a, const std::int32_t b, const std::size_t totalSize = 8)
	{
		std::vector<char> payload(totalSize, '.');
		std::memcpy(payload.data(), &a, sizeof(a));
		std::memcpy(payload.data() + sizeof(a), &b, sizeof(b));
		return payload;
	}

	std::int32_t FieldA(const std::vector<char>& payload)
	{
		std::int32_t value = 0;
		std::memcpy(&value, payload.data(), sizeof(value));
		return value;
	}

	std::int32_t FieldB(const std::vector<char>& payload)
	{
		std::int32_t value = 0;
		std::memcpy(&value, payload.data() + sizeof(std::int32_t), sizeof(value));
		return value;
	}
}

TEST_CASE("EventDispatcher emits single-thread events in FIFO order")
{
	constexpr std::int32_t count = 1000;
	RecordingSink sink;
	EventDispatcher dispatcher(sink, 8 * 1024 * 1024);
	dispatcher.Start();

	for (std::int32_t i = 0; i < count; ++i)
	{
		const auto payload = MakePayload(i, 0);
		dispatcher.Enqueue(payload.data(), payload.size());
	}
	dispatcher.Stop();

	const auto records = sink.Records();
	REQUIRE(records.size() == static_cast<std::size_t>(count));
	for (std::int32_t i = 0; i < count; ++i)
		CHECK(FieldA(records[i]) == i);
}

TEST_CASE("EventDispatcher delivers priority events")
{
	RecordingSink sink;
	EventDispatcher dispatcher(sink, 8 * 1024 * 1024);
	dispatcher.Start();

	for (std::int32_t i = 0; i < 100; ++i)
	{
		const auto payload = MakePayload(i, 0);
		dispatcher.EnqueuePriority(payload.data(), payload.size());
	}
	dispatcher.Stop();

	const auto records = sink.Records();
	REQUIRE(records.size() == 100);
	for (std::int32_t i = 0; i < 100; ++i)
		CHECK(FieldA(records[i]) == i);
}

TEST_CASE("EventDispatcher routes oversized records through overflow in sequence order")
{
	RecordingSink sink;
	EventDispatcher dispatcher(sink, 2 * 1024 * 1024);
	dispatcher.Start();

	const auto small0 = MakePayload(0, 0);
	const auto oversized = MakePayload(1, 0, 300 * 1024);
	const auto small2 = MakePayload(2, 0);
	dispatcher.Enqueue(small0.data(), small0.size());
	dispatcher.Enqueue(oversized.data(), oversized.size());
	dispatcher.Enqueue(small2.data(), small2.size());
	dispatcher.Stop();

	const auto records = sink.Records();
	REQUIRE(records.size() == 3);
	CHECK(FieldA(records[0]) == 0);
	CHECK(FieldA(records[1]) == 1);
	CHECK(FieldA(records[2]) == 2);
	CHECK(records[1].size() == 300 * 1024);
}

TEST_CASE("EventDispatcher loses nothing and preserves per-thread order under contention")
{
	constexpr std::int32_t threadCount = 4;
	constexpr std::int32_t perThread = 2000;

	RecordingSink sink;
	EventDispatcher dispatcher(sink, 8 * 1024 * 1024);
	dispatcher.Start();

	std::vector<std::thread> producers;
	for (std::int32_t t = 0; t < threadCount; ++t)
	{
		producers.emplace_back([&dispatcher, t]
		{
			for (std::int32_t i = 0; i < perThread; ++i)
			{
				const auto payload = MakePayload(t, i);
				dispatcher.Enqueue(payload.data(), payload.size());
			}
		});
	}
	for (auto& producer : producers)
		producer.join();
	dispatcher.Stop();

	const auto records = sink.Records();
	REQUIRE(records.size() == static_cast<std::size_t>(threadCount * perThread));

	std::vector<std::int32_t> nextExpected(threadCount, 0);
	for (const auto& record : records)
	{
		const auto thread = FieldA(record);
		const auto index = FieldB(record);
		REQUIRE(thread >= 0);
		REQUIRE(thread < threadCount);
		// Global drain must never reorder events emitted by the same producer thread.
		CHECK(index == nextExpected[thread]);
		++nextExpected[thread];
	}
	for (std::int32_t t = 0; t < threadCount; ++t)
		CHECK(nextExpected[t] == perThread);
}
