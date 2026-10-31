// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "ThreadIncarnations.h"

using namespace LibProfiler;

UINT64 ThreadIncarnations::OnThreadCreated(const ThreadID threadId)
{
	return GetOrAssign(threadId);
}

UINT64 ThreadIncarnations::OnThreadDestroyed(const ThreadID threadId)
{
	if (threadId == 0)
		return NoIncarnation;

	std::lock_guard lock(_mutex);
	const auto it = _incarnations.find(threadId);
	if (it == _incarnations.end())
		return NewIncarnation();

	const auto incarnation = it->second;
	_threadIds.erase(incarnation);
	_incarnations.erase(it);
	return incarnation;
}

UINT64 ThreadIncarnations::GetOrAssign(const ThreadID threadId)
{
	if (threadId == 0)
		return NoIncarnation;

	std::lock_guard lock(_mutex);
	const auto it = _incarnations.find(threadId);
	return it != _incarnations.end() ? it->second : Assign(threadId);
}

UINT64 ThreadIncarnations::Resolve(const ThreadID threadId, const ThreadID cachedThreadId, const UINT64 cachedIncarnation)
{
	if (threadId == 0)
		return NoIncarnation;

	std::lock_guard lock(_mutex);
	const auto it = _incarnations.find(threadId);
	if (it != _incarnations.end())
		return it->second;

	const auto isRunningAfterOwnDestroy = threadId == cachedThreadId && cachedIncarnation != NoIncarnation;
	return isRunningAfterOwnDestroy ? cachedIncarnation : Assign(threadId);
}

std::optional<ThreadID> ThreadIncarnations::TryGetThreadId(const UINT64 incarnation) const
{
	std::lock_guard lock(_mutex);
	const auto it = _threadIds.find(incarnation);
	if (it == _threadIds.end())
		return std::nullopt;
	return it->second;
}

void ThreadIncarnations::GetLiveThreadIds(
	const std::vector<UINT64>& incarnations,
	std::vector<ThreadID>& threadIds,
	std::vector<size_t>& positions) const
{
	threadIds.clear();
	positions.clear();
	threadIds.reserve(incarnations.size());
	positions.reserve(incarnations.size());

	std::lock_guard lock(_mutex);
	for (size_t i = 0; i < incarnations.size(); ++i)
	{
		const auto it = _threadIds.find(incarnations[i]);
		if (it == _threadIds.end())
			continue;

		threadIds.push_back(it->second);
		positions.push_back(i);
	}
}

size_t ThreadIncarnations::GetLiveThreadsCount() const
{
	std::lock_guard lock(_mutex);
	return _incarnations.size();
}

UINT64 ThreadIncarnations::Assign(const ThreadID threadId)
{
	const auto incarnation = NewIncarnation();
	_incarnations.emplace(threadId, incarnation);
	_threadIds.emplace(incarnation, threadId);
	return incarnation;
}

UINT64 ThreadIncarnations::NewIncarnation()
{
	return _nextIncarnation++;
}
