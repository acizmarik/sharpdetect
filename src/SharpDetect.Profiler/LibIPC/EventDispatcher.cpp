// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <chrono>
#include <limits>
#include <thread>

#include "../lib/loguru/loguru.hpp"

#include "EventDispatcher.h"

LibIPC::EventDispatcher::EventDispatcher(IEventSink& sink, const std::size_t eventQueueMaxBytes) :
	_sink(sink),
	_terminating(false),
	_lanes(eventQueueMaxBytes)
{
}

void LibIPC::EventDispatcher::Start()
{
	_eventThread = std::thread(&LibIPC::EventDispatcher::EventThreadLoop, this);
}

void LibIPC::EventDispatcher::Stop()
{
	_terminating.store(true, std::memory_order_release);
	_drainSignal.release();
	if (_eventThread.joinable())
		_eventThread.join();
	
	DrainAvailableEvents();
}

void LibIPC::EventDispatcher::Enqueue(const char* payload, const std::size_t size)
{
	auto& lane = _lanes.GetOrCreate();
	if (EventLane::RecordHeaderSize + size > lane.GetCapacity())
	{
		EnqueueOverflowEvent(payload, size);
		return;
	}

	// Backpressure: stall the producing thread until the drain frees lane space
	for (auto spinCount = 0; !lane.HasSpaceFor(size); ++spinCount)
	{
		if (_terminating.load(std::memory_order_relaxed))
		{
			EnqueueOverflowEvent(payload, size);
			return;
		}

		WakeDrain();
		if (spinCount < 10)
			std::this_thread::yield();
		else if (spinCount < 20)
			std::this_thread::sleep_for(std::chrono::milliseconds(0));
		else
			std::this_thread::sleep_for(std::chrono::milliseconds(1));
	}
	
	const auto sequence = _sequence.fetch_add(1, std::memory_order_relaxed);
	lane.Write(sequence, payload, size);
	WakeDrain();
}

void LibIPC::EventDispatcher::EnqueuePriority(const char* payload, const std::size_t size)
{
	// GC callbacks must not wait for drain progress
	auto& lane = _lanes.GetOrCreate();
	if (EventLane::RecordHeaderSize + size <= lane.GetCapacity() && lane.HasSpaceFor(size))
	{
		const auto sequence = _sequence.fetch_add(1, std::memory_order_relaxed);
		lane.Write(sequence, payload, size);
		WakeDrain();
	}
	else
	{
		EnqueueOverflowEvent(payload, size);
	}
}

void LibIPC::EventDispatcher::EnqueueOverflowEvent(const char* payload, const std::size_t size)
{
	const auto sequence = _sequence.fetch_add(1, std::memory_order_relaxed);
	_overflow.Push(sequence, payload, size);
	WakeDrain();
}

void LibIPC::EventDispatcher::WakeDrain()
{
	std::atomic_thread_fence(std::memory_order_seq_cst);
	if (_drainParked.load(std::memory_order_relaxed))
		_drainSignal.release();
}

bool LibIPC::EventDispatcher::DrainAvailableEvents()
{
	constexpr auto terminatingGapDeadline = std::chrono::seconds(2);

	auto progress = false;
	auto gapStart = std::chrono::steady_clock::time_point { };
	for (auto gapSpinCount = 0; ; )
	{
		const auto& lanes = _lanes.Snapshot();
		_overflow.Splice();

		// Pick the record with the lowest global sequence across all lanes and the overflow
		auto minSequence = std::numeric_limits<UINT64>::max();
		EventLane* sourceLane = nullptr;
		for (const auto& lane : lanes)
		{
			UINT64 sequence;
			if (lane->TryPeekSequence(sequence) && sequence < minSequence)
			{
				minSequence = sequence;
				sourceLane = lane.get();
			}
		}
		auto fromOverflow = false;
		if (UINT64 overflowSequence; _overflow.TryPeek(overflowSequence) && overflowSequence < minSequence)
		{
			minSequence = overflowSequence;
			fromOverflow = true;
		}

		if (minSequence == std::numeric_limits<UINT64>::max())
		{
			_lanes.PruneClosed();
			return progress;
		}

		if (minSequence > _nextSequenceToEmit)
		{
			// A producer claimed the next sequence but has not published it yet
			if (gapStart == std::chrono::steady_clock::time_point { })
				gapStart = std::chrono::steady_clock::now();

			if (_terminating.load(std::memory_order_relaxed) &&
				std::chrono::steady_clock::now() - gapStart >= terminatingGapDeadline)
			{
				LOG_F(ERROR, "Skipping %llu unpublished event sequence(s) during shutdown.",
					static_cast<unsigned long long>(minSequence - _nextSequenceToEmit));
				_nextSequenceToEmit = minSequence;
				continue;
			}
			
			if (++gapSpinCount >= 2000)
				std::this_thread::sleep_for(std::chrono::microseconds(100));
			else if (gapSpinCount >= 500)
				std::this_thread::yield();
			continue;
		}

		gapSpinCount = 0;
		gapStart = { };
		if (fromOverflow)
		{
			auto payload = _overflow.Pop();
			_sink.Send(payload);
		}
		else
		{
			sourceLane->ConsumeInto(_drainScratch);
			_sink.Send(_drainScratch);
		}

		if (minSequence >= _nextSequenceToEmit)
			_nextSequenceToEmit = minSequence + 1;
		progress = true;
		
		if (!fromOverflow)
		{
			UINT64 sequence;
			while (sourceLane->TryPeekSequence(sequence) && sequence == _nextSequenceToEmit)
			{
				sourceLane->ConsumeInto(_drainScratch);
				_sink.Send(_drainScratch);
				++_nextSequenceToEmit;
			}
		}
	}
}

bool LibIPC::EventDispatcher::AnyEventPending()
{
	if (_overflow.HasIncoming())
		return true;
	if (_lanes.SnapshotStale())
		return true;

	auto& snapshot = _lanes.Snapshot();
	return std::ranges::any_of(
		snapshot.cbegin(),
		snapshot.cend(),
		[](const auto& item)  { return !item->IsEmpty(); });
}

void LibIPC::EventDispatcher::ParkDrain()
{
	while (_drainSignal.try_acquire())
	{
		// Discard permits accumulated while the drain was busy
	}

	_drainParked.store(true, std::memory_order_relaxed);
	std::atomic_thread_fence(std::memory_order_seq_cst);
	if (AnyEventPending() || _terminating.load(std::memory_order_relaxed))
	{
		_drainParked.store(false, std::memory_order_relaxed);
		return;
	}

	_drainSignal.try_acquire_for(std::chrono::milliseconds(200));
	_drainParked.store(false, std::memory_order_relaxed);
}

void LibIPC::EventDispatcher::EventThreadLoop()
{
	LOG_F(INFO, "IPC event worker thread started.");
	while (true)
	{
		const auto progress = DrainAvailableEvents();

		if (_terminating.load(std::memory_order_acquire))
			break;

		if (!progress)
			ParkDrain();
	}

	LOG_F(INFO, "IPC event worker thread terminated.");
}
