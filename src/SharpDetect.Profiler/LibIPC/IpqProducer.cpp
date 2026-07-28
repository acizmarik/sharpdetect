// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <chrono>
#include <limits>
#include <stdexcept>
#include <thread>

#include "../lib/loguru/loguru.hpp"

#include "IpqProducer.h"

LibIPC::IpqProducer::IpqProducer(
	const IpqLibrary& library,
	const std::string& name,
	const std::string& file,
	const std::string& semaphore,
	const INT size) :
	_library(library),
	_handle(library.CreateProducer(name, file, semaphore, size))
{
	if (_handle == nullptr)
	{
		LOG_F(FATAL, "Communication library could not create producer");
		throw std::runtime_error("Could not obtain write access to IPC event queue.");
	}

	_batch.reserve(FlushThresholdBytes + BatchSlackBytes);
}

LibIPC::IpqProducer::~IpqProducer()
{
	if (_handle != nullptr)
		_library.DestroyProducer(_handle);
}

void LibIPC::IpqProducer::Send(std::vector<char>& buffer)
{
	constexpr auto maxRecordSize = static_cast<std::size_t>(std::numeric_limits<std::int32_t>::max());
	const auto size = buffer.size();
	if (size > maxRecordSize)
	{
		LOG_F(ERROR, "Dropping IPC message (%zu bytes): record exceeds the maximum size.", size);
		return;
	}

	const auto sizeField = static_cast<std::int32_t>(size);
	const auto sizeFieldBytes = reinterpret_cast<const char*>(&sizeField);
	_batch.insert(_batch.end(), sizeFieldBytes, sizeFieldBytes + RecordHeaderSize);
	_batch.insert(_batch.end(), buffer.begin(), buffer.end());

	// An oversized record ends up in a batch of its own
	if (_batch.size() >= FlushThresholdBytes)
		Flush();
}

void LibIPC::IpqProducer::Flush()
{
	if (_batch.empty())
		return;

	SendMessage(_batch.data(), _batch.size());

	// An oversized record grows the batch far past the threshold
	if (_batch.capacity() > FlushThresholdBytes + BatchSlackBytes)
	{
		std::vector<char> replacement;
		replacement.reserve(FlushThresholdBytes + BatchSlackBytes);
		_batch.swap(replacement);
	}
	else
	{
		_batch.clear();
	}
}

void LibIPC::IpqProducer::SendMessage(char* data, const std::size_t size)
{
	constexpr INT enqueueOk = 0;
	constexpr INT enqueueNotEnoughFreeMemory = 3;
	constexpr auto maxRetryDuration = std::chrono::seconds(5);

	const auto byteStream = reinterpret_cast<BYTE*>(data);
	const auto deadline = std::chrono::steady_clock::now() + maxRetryDuration;
	for (auto spinCount = 0; ; ++spinCount)
	{
		const INT result = _library.Enqueue(_handle, byteStream, static_cast<INT>(size));
		if (result == enqueueOk)
			return;

		if (result != enqueueNotEnoughFreeMemory)
		{
			LOG_F(
				ERROR,
				"Dropping IPC message (%zu bytes) after non-recoverable enqueue error: %d.",
				size,
				result);
			return;
		}

		// A full ring past deadline means we assume the consumer is gone/detached and will never drain it
		if (std::chrono::steady_clock::now() >= deadline)
		{
			LOG_F(
				ERROR,
				"Dropping IPC message (%zu bytes): consumer did not drain the queue within %lld seconds.",
				size,
				static_cast<long long>(maxRetryDuration.count()));
			return;
		}

		// Backoff when repeatedly accessing queue leads to transient failures
		if (spinCount < 10)
			std::this_thread::yield();
		else if (spinCount < 20)
			std::this_thread::sleep_for(std::chrono::milliseconds(0));
		else
			std::this_thread::sleep_for(std::chrono::milliseconds(1));
	}
}
