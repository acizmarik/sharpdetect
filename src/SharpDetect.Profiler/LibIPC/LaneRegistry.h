// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <cstddef>
#include <memory>
#include <mutex>
#include <vector>

#include "cor.h"
#include "EventLane.h"

namespace LibIPC
{
	class LaneRegistry
	{
	public:
		explicit LaneRegistry(std::size_t eventQueueMaxBytes);

		EventLane& GetOrCreate();

		const std::vector<std::shared_ptr<EventLane>>& Snapshot();
		[[nodiscard]] bool SnapshotStale() const;
		void PruneClosed();

	private:
		std::size_t _laneCapacity;
		UINT64 _registryId;

		std::vector<std::shared_ptr<EventLane>> _lanes;
		std::mutex _lanesMutex;
		std::atomic<UINT64> _lanesVersion = 0;

		std::vector<std::shared_ptr<EventLane>> _snapshot;
		UINT64 _snapshotVersion = 0;
	};
}
