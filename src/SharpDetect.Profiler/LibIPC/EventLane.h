// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <algorithm>
#include <atomic>
#include <cstddef>
#include <cstring>
#include <vector>

#include "cor.h"

namespace LibIPC
{
	// Single-producer single-consumer byte ring carrying [u32 payloadLength][u64 sequence][payload] records
	// Each thread has its own lane - all lanes are consumed by a single drain that maintains events ordering
	class EventLane
	{
	public:
		static constexpr std::size_t RecordHeaderSize = sizeof(UINT32) + sizeof(UINT64);

		explicit EventLane(const std::size_t capacity) : // capacity is a power of two
			_buffer(capacity),
			_capacity(capacity),
			_head(0),
			_tail(0),
			_closed(false)
		{
		}

		[[nodiscard]] std::size_t GetCapacity() const { return _capacity; }
		
		[[nodiscard]] bool HasSpaceFor(const std::size_t payloadSize) const
		{
			const auto needed = RecordHeaderSize + payloadSize;
			const auto used = _tail.load(std::memory_order_relaxed) - _head.load(std::memory_order_acquire);
			return _capacity - used >= needed;
		}

		void Write(const UINT64 sequence, const char* payload, const std::size_t payloadSize)
		{
			const auto tail = _tail.load(std::memory_order_relaxed);
			const auto length = static_cast<UINT32>(payloadSize);
			CopyIn(tail, &length, sizeof(length));
			CopyIn(tail + sizeof(length), &sequence, sizeof(sequence));
			CopyIn(tail + RecordHeaderSize, payload, payloadSize);
			_tail.store(tail + RecordHeaderSize + payloadSize, std::memory_order_release);
		}

		void MarkClosed()
		{
			_closed.store(true, std::memory_order_release);
		}

		[[nodiscard]] bool TryPeekSequence(UINT64& sequence) const
		{
			const auto head = _head.load(std::memory_order_relaxed);
			if (_tail.load(std::memory_order_acquire) == head)
				return false;

			CopyOut(head + sizeof(UINT32), &sequence, sizeof(sequence));
			return true;
		}

		void ConsumeInto(std::vector<char>& payload)
		{
			const auto head = _head.load(std::memory_order_relaxed);
			UINT32 length;
			CopyOut(head, &length, sizeof(length));
			payload.resize(length);
			CopyOut(head + RecordHeaderSize, payload.data(), length);
			_head.store(head + RecordHeaderSize + length, std::memory_order_release);
		}

		[[nodiscard]] bool IsEmpty() const
		{
			return _tail.load(std::memory_order_acquire) == _head.load(std::memory_order_acquire);
		}

		[[nodiscard]] bool IsClosed() const
		{
			return _closed.load(std::memory_order_acquire);
		}

	private:
		void CopyIn(const UINT64 position, const void* source, const std::size_t size)
		{
			const auto offset = static_cast<std::size_t>(position) & (_capacity - 1);
			const auto contiguous = std::min(size, _capacity - offset);
			std::memcpy(_buffer.data() + offset, source, contiguous);
			if (contiguous != size)
				std::memcpy(_buffer.data(), static_cast<const BYTE*>(source) + contiguous, size - contiguous);
		}

		void CopyOut(const UINT64 position, void* destination, const std::size_t size) const
		{
			const auto offset = static_cast<std::size_t>(position) & (_capacity - 1);
			const auto contiguous = std::min(size, _capacity - offset);
			std::memcpy(destination, _buffer.data() + offset, contiguous);
			if (contiguous != size)
				std::memcpy(static_cast<BYTE*>(destination) + contiguous, _buffer.data(), size - contiguous);
		}

		std::vector<BYTE> _buffer;
		std::size_t _capacity;
		std::atomic<UINT64> _head;
		std::atomic<UINT64> _tail;
		std::atomic<bool> _closed;
	};
}
