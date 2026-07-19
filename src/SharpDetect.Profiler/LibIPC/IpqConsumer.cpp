// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <stdexcept>

#include "../lib/loguru/loguru.hpp"

#include "IpqConsumer.h"

LibIPC::IpqConsumer::IpqConsumer(
	const IpqLibrary& library,
	const std::string& name,
	const std::string& file,
	const std::string& semaphore,
	const INT size) :
	_library(library),
	_handle(library.CreateConsumer(name, file, semaphore, size))
{
	if (_handle == nullptr)
	{
		LOG_F(FATAL, "Communication library could not create consumer.");
		throw std::runtime_error("Could not obtain read access to IPC command queue.");
	}
}

LibIPC::IpqConsumer::~IpqConsumer()
{
	if (_handle != nullptr)
		_library.DestroyConsumer(_handle);
}

bool LibIPC::IpqConsumer::TryDequeue(BYTE** data, INT* size, const INT timeoutMs) const
{
	return _library.DequeueTimeout(_handle, data, size, timeoutMs) == 0;
}

void LibIPC::IpqConsumer::Free(BYTE* data) const
{
	_library.FreeMemory(data);
}
