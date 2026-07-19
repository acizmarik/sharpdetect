// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <cstddef>
#include <map>
#include <mutex>
#include <vector>

#include "cor.h"

namespace LibIPC
{
	class OverflowBuffer
	{
	public:
		void Push(UINT64 sequence, const char* payload, std::size_t size);
		[[nodiscard]] bool HasIncoming() const;
		void Splice();
		[[nodiscard]] bool TryPeek(UINT64& sequence) const;
		std::vector<char> Pop();

	private:
		struct Record
		{
			UINT64 sequence;
			std::vector<char> payload;
		};

		std::vector<Record> _incoming;
		std::mutex _mutex;
		std::atomic<std::size_t> _incomingCount = 0;

		std::map<UINT64, std::vector<char>> _pending;
	};
}
