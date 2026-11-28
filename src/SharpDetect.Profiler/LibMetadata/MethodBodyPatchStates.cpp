// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <utility>

#include "MethodBodyPatchStates.h"

LibProfiler::MethodBodyPatchStates::Claim::Claim(Claim&& other) noexcept :
	_state(std::exchange(other._state, nullptr)),
	_isPatched(other._isPatched)
{
}

LibProfiler::MethodBodyPatchStates::Claim::~Claim()
{
	if (_state == nullptr)
		return;

	_state->store(_isPatched ? State::Patched : State::Unpatched, std::memory_order_release);
	_state->notify_all();
}

LibProfiler::MethodBodyPatchStates::MethodBodyPatchStates(const std::size_t methodDefsCount) :
	_methodDefsCount(methodDefsCount),
	_states(std::make_unique<std::atomic<State>[]>(methodDefsCount + 1))
{
}

std::optional<LibProfiler::MethodBodyPatchStates::Claim> LibProfiler::MethodBodyPatchStates::TryClaim(const mdMethodDef methodDef)
{
	auto& state = GetState(methodDef);
	while (true)
	{
		auto current = state.load(std::memory_order_acquire);
		if (current == State::Patched)
			return std::nullopt;

		if (current == State::Patching)
		{
			state.wait(State::Patching, std::memory_order_acquire);
			continue;
		}

		if (state.compare_exchange_weak(current, State::Patching, std::memory_order_acquire, std::memory_order_relaxed))
			return Claim(state);
	}
}

std::atomic<LibProfiler::MethodBodyPatchStates::State>& LibProfiler::MethodBodyPatchStates::GetState(const mdMethodDef methodDef)
{
	auto const rid = static_cast<std::size_t>(RidFromToken(methodDef));
	if (rid <= _methodDefsCount)
		return _states[rid];

	return GetAddedMethodState(rid);
}

std::atomic<LibProfiler::MethodBodyPatchStates::State>& LibProfiler::MethodBodyPatchStates::GetAddedMethodState(const std::size_t rid)
{
	std::call_once(_addedMethodStatesAllocation, [this] { _addedMethodStates = std::make_unique<AddedMethodStates>(); });

	auto& added = *_addedMethodStates;
	auto const chunkIndex = rid / ChunkSize;
	std::call_once(added.chunkAllocations[chunkIndex], [&added, chunkIndex] { added.chunks[chunkIndex] = std::make_unique<Chunk>(); });
	return (*added.chunks[chunkIndex])[rid % ChunkSize];
}
