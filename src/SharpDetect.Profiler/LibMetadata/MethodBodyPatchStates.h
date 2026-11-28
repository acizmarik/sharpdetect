// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <array>
#include <atomic>
#include <cstddef>
#include <cstdint>
#include <memory>
#include <mutex>
#include <optional>

#include "cor.h"

namespace LibProfiler
{
	class MethodBodyPatchStates
	{
		enum class State : std::uint8_t { Unpatched, Patching, Patched };

	public:
		class Claim
		{
		public:
			Claim(Claim&& other) noexcept;
			~Claim();

			Claim(const Claim&) = delete;
			Claim& operator=(const Claim&) = delete;
			Claim& operator=(Claim&&) = delete;

			void MarkPatched() { _isPatched = true; }

		private:
			friend class MethodBodyPatchStates;
			explicit Claim(std::atomic<State>& state) : _state(&state), _isPatched(false) { }

			std::atomic<State>* _state;
			bool _isPatched;
		};

		explicit MethodBodyPatchStates(std::size_t methodDefsCount);

		// Blocks while another thread holds the claim
		[[nodiscard]] std::optional<Claim> TryClaim(mdMethodDef methodDef);

	private:
		static constexpr std::size_t RidCount = std::size_t { 1 } << 24;
		static constexpr std::size_t ChunkSize = std::size_t { 1 } << 14;
		static constexpr std::size_t ChunksCount = RidCount / ChunkSize;
		using Chunk = std::array<std::atomic<State>, ChunkSize>;

		struct AddedMethodStates
		{
			std::array<std::once_flag, ChunksCount> chunkAllocations;
			std::array<std::unique_ptr<Chunk>, ChunksCount> chunks;
		};

		[[nodiscard]] std::atomic<State>& GetState(mdMethodDef methodDef);
		[[nodiscard]] std::atomic<State>& GetAddedMethodState(std::size_t rid);

		std::size_t _methodDefsCount;
		std::unique_ptr<std::atomic<State>[]> _states;
		std::once_flag _addedMethodStatesAllocation;
		std::unique_ptr<AddedMethodStates> _addedMethodStates;
	};
}
