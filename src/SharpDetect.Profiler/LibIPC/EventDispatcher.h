// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <cstddef>
#include <semaphore>
#include <thread>
#include <vector>

#include "cor.h"
#include "EventLane.h"
#include "EventSink.h"
#include "LaneRegistry.h"
#include "OverflowBuffer.h"

namespace LibIPC
{
	class EventDispatcher
	{
	public:
		EventDispatcher(IEventSink& sink, std::size_t eventQueueMaxBytes);
		~EventDispatcher() = default;
		EventDispatcher(const EventDispatcher&) = delete;
		EventDispatcher& operator=(const EventDispatcher&) = delete;
		EventDispatcher(EventDispatcher&&) = delete;
		EventDispatcher& operator=(EventDispatcher&&) = delete;

		void Start();
		void Stop();

		void Enqueue(const char* payload, std::size_t size);
		void EnqueuePriority(const char* payload, std::size_t size);

	private:
		void EnqueueOverflowEvent(const char* payload, std::size_t size);
		void WakeDrain();
		bool DrainAvailableEvents();
		void ParkDrain();
		bool AnyEventPending();
		void EventThreadLoop();

		IEventSink& _sink;
		std::thread _eventThread;
		std::atomic_bool _terminating;

		std::atomic<UINT64> _sequence = 0;
		LaneRegistry _lanes;
		OverflowBuffer _overflow;

		std::atomic<bool> _drainParked = false;
		std::counting_semaphore<> _drainSignal { 0 };

		UINT64 _nextSequenceToEmit = 0;
		std::vector<char> _drainScratch;
	};
}
