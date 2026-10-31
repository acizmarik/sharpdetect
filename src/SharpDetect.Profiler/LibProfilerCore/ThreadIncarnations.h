// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstddef>
#include <mutex>
#include <optional>
#include <unordered_map>
#include <utility>
#include <vector>

#include "cor.h"
#include "corprof.h"

namespace LibProfiler
{
	// The runtime reuses a ThreadID once it cleans up a dead thread; an incarnation is unique per thread lifetime
	class ThreadIncarnations
	{
	public:
		static constexpr UINT64 NoIncarnation = 0;

		[[nodiscard]] UINT64 OnThreadCreated(ThreadID threadId);
		[[nodiscard]] UINT64 OnThreadDestroyed(ThreadID threadId);
		[[nodiscard]] UINT64 GetOrAssign(ThreadID threadId);
		[[nodiscard]] UINT64 Resolve(ThreadID threadId, ThreadID cachedThreadId, UINT64 cachedIncarnation);
		[[nodiscard]] std::optional<ThreadID> TryGetThreadId(UINT64 incarnation) const;
		void GetLiveThreadIds(
			const std::vector<UINT64>& incarnations,
			std::vector<ThreadID>& threadIds,
			std::vector<size_t>& positions) const;
		[[nodiscard]] size_t GetLiveThreadsCount() const;

	private:
		UINT64 Assign(ThreadID threadId);
		UINT64 NewIncarnation();

		mutable std::mutex _mutex;
		UINT64 _nextIncarnation = 1;
		std::unordered_map<ThreadID, UINT64> _incarnations;
		std::unordered_map<UINT64, ThreadID> _threadIds;
	};

	template <typename T>
	[[nodiscard]] std::vector<T> ScatterToPositions(std::vector<T>&& values, const std::vector<size_t>& positions, const size_t count)
	{
		std::vector<T> result(count);
		for (size_t i = 0; i < values.size() && i < positions.size(); ++i)
			result[positions[i]] = std::move(values[i]);
		return result;
	}
}
