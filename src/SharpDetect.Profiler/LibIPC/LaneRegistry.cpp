// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>
#include <bit>

#include "../lib/loguru/loguru.hpp"

#include "LaneRegistry.h"

namespace
{
	struct LaneHandle
	{
		std::shared_ptr<LibIPC::EventLane> lane;
		UINT64 ownerId = 0;

		~LaneHandle()
		{
			if (lane != nullptr)
				lane->MarkClosed();
		}
	};

	thread_local LaneHandle t_laneHandle;
	std::atomic<UINT64> s_nextRegistryId { 1 };
}

LibIPC::LaneRegistry::LaneRegistry(const std::size_t eventQueueMaxBytes) :
	_laneCapacity(0),
	_registryId(s_nextRegistryId.fetch_add(1, std::memory_order_relaxed))
{
	constexpr std::size_t expectedProducerThreads = 8;
	constexpr std::size_t minimumLaneCapacity = 256 * 1024;
	_laneCapacity = std::bit_floor(std::max(eventQueueMaxBytes / expectedProducerThreads, minimumLaneCapacity));
	LOG_F(INFO, "Event buffer cap: %zu bytes (%zu bytes per producer lane).", eventQueueMaxBytes, _laneCapacity);
}

LibIPC::EventLane& LibIPC::LaneRegistry::GetOrCreate()
{
	auto& handle = t_laneHandle;
	if (handle.lane == nullptr || handle.ownerId != _registryId)
	{
		if (handle.lane != nullptr)
			handle.lane->MarkClosed();

		handle.lane = std::make_shared<EventLane>(_laneCapacity);
		handle.ownerId = _registryId;
		{
			std::lock_guard guard(_lanesMutex);
			_lanes.push_back(handle.lane);
		}
		_lanesVersion.fetch_add(1, std::memory_order_release);
	}

	return *handle.lane;
}

const std::vector<std::shared_ptr<LibIPC::EventLane>>& LibIPC::LaneRegistry::Snapshot()
{
	const auto version = _lanesVersion.load(std::memory_order_acquire);
	if (version != _snapshotVersion)
	{
		std::lock_guard guard(_lanesMutex);
		_snapshot = _lanes;
		_snapshotVersion = version;
	}
	return _snapshot;
}

bool LibIPC::LaneRegistry::SnapshotStale() const
{
	return _lanesVersion.load(std::memory_order_acquire) != _snapshotVersion;
}

void LibIPC::LaneRegistry::PruneClosed()
{
	auto anyRemovable = false;
	for (const auto& lane : _snapshot)
	{
		if (lane->IsClosed() && lane->IsEmpty())
		{
			anyRemovable = true;
			break;
		}
	}
	if (!anyRemovable)
		return;

	{
		std::lock_guard guard(_lanesMutex);
		std::erase_if(_lanes, [](const auto& lane) { return lane->IsClosed() && lane->IsEmpty(); });
	}
	_lanesVersion.fetch_add(1, std::memory_order_release);
}
