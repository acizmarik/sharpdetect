// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "OverflowBuffer.h"

void LibIPC::OverflowBuffer::Push(const UINT64 sequence, const char* payload, const std::size_t size)
{
	std::lock_guard guard(_mutex);
	_incoming.push_back(Record { sequence, std::vector<char>(payload, payload + size) });
	_incomingCount.fetch_add(1, std::memory_order_release);
}

bool LibIPC::OverflowBuffer::HasIncoming() const
{
	return _incomingCount.load(std::memory_order_acquire) > 0;
}

void LibIPC::OverflowBuffer::Splice()
{
	if (_incomingCount.load(std::memory_order_acquire) == 0)
		return;

	std::vector<Record> taken;
	{
		std::lock_guard guard(_mutex);
		taken.swap(_incoming);
		_incomingCount.store(0, std::memory_order_release);
	}

	for (auto& item : taken)
		_pending.emplace(item.sequence, std::move(item.payload));
}

bool LibIPC::OverflowBuffer::TryPeek(UINT64& sequence) const
{
	if (_pending.empty())
		return false;

	sequence = _pending.begin()->first;
	return true;
}

std::vector<char> LibIPC::OverflowBuffer::Pop()
{
	auto node = _pending.extract(_pending.begin());
	return std::move(node.mapped());
}
