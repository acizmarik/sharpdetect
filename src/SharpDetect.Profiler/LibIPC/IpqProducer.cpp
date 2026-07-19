// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <chrono>
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
}

LibIPC::IpqProducer::~IpqProducer()
{
	if (_handle != nullptr)
		_library.DestroyProducer(_handle);
}

void LibIPC::IpqProducer::Send(std::vector<char>& buffer)
{
	constexpr INT enqueueOk = 0;
	constexpr INT enqueueNotEnoughFreeMemory = 3;
	constexpr auto maxRetryDuration = std::chrono::seconds(5);

	const auto byteStream = reinterpret_cast<BYTE*>(buffer.data());
	const auto deadline = std::chrono::steady_clock::now() + maxRetryDuration;
	for (auto spinCount = 0; ; ++spinCount)
	{
		const INT result = _library.Enqueue(_handle, byteStream, static_cast<INT>(buffer.size()));
		if (result == enqueueOk)
			return;

		if (result != enqueueNotEnoughFreeMemory)
		{
			LOG_F(
				ERROR,
				"Dropping IPC message (%zu bytes) after non-recoverable enqueue error: %d.",
				buffer.size(),
				result);
			return;
		}

		// A full ring past deadline means we assume the consumer is gone/detached and will never drain it
		if (std::chrono::steady_clock::now() >= deadline)
		{
			LOG_F(
				ERROR,
				"Dropping IPC message (%zu bytes): consumer did not drain the queue within %lld seconds.",
				buffer.size(),
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
