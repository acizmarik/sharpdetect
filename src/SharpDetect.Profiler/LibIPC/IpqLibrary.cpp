// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <stdexcept>

#include "../lib/loguru/loguru.hpp"

#include "IpqLibrary.h"

namespace
{
	template<class TFunction>
	TFunction ResolveSymbol(MODULE_HANDLE module, const std::string& name)
	{
		return reinterpret_cast<TFunction>(LibProfiler::PAL_LoadSymbolAddress(module, name));
	}
}

LibIPC::IpqLibrary::IpqLibrary(const std::string& libraryPath) :
	_moduleHandle(nullptr),
	_producerCreate(nullptr),
	_producerDestroy(nullptr),
	_producerEnqueue(nullptr),
	_registerProcess(nullptr),
	_consumerCreate(nullptr),
	_consumerDestroy(nullptr),
	_consumerDequeueTimeout(nullptr),
	_freeMemory(nullptr)
{
	_moduleHandle = LibProfiler::PAL_LoadLibrary(libraryPath);
	if (_moduleHandle == nullptr)
	{
		LOG_F(FATAL, "Could not load %s.", libraryPath.c_str());
		throw std::runtime_error("Error while loading communication library.");
	}

	_producerCreate = ResolveSymbol<ipq_producer_create>(_moduleHandle, "ipq_producer_create");
	_producerDestroy = ResolveSymbol<ipq_producer_destroy>(_moduleHandle, "ipq_producer_destroy");
	_producerEnqueue = ResolveSymbol<ipq_producer_enqueue>(_moduleHandle, "ipq_producer_enqueue");
	_registerProcess = ResolveSymbol<ipq_register_process>(_moduleHandle, "ipq_register_process");
	_consumerCreate = ResolveSymbol<ipq_consumer_create>(_moduleHandle, "ipq_consumer_create");
	_consumerDestroy = ResolveSymbol<ipq_consumer_destroy>(_moduleHandle, "ipq_consumer_destroy");
	_consumerDequeueTimeout = ResolveSymbol<ipq_consumer_dequeue_timeout>(_moduleHandle, "ipq_consumer_dequeue_timeout");
	_freeMemory = ResolveSymbol<ipq_free_memory>(_moduleHandle, "ipq_free_memory");

	if (_producerCreate == nullptr ||
		_producerDestroy == nullptr ||
		_producerEnqueue == nullptr ||
		_registerProcess == nullptr ||
		_consumerCreate == nullptr ||
		_consumerDestroy == nullptr ||
		_consumerDequeueTimeout == nullptr ||
		_freeMemory == nullptr)
	{
		LOG_F(FATAL, "Communication library does not contain expected symbols.");
		throw std::runtime_error("Incompatibility issue while loading communication library symbols.");
	}
}

PVOID LibIPC::IpqLibrary::CreateProducer(const std::string& name, const std::string& file,
	const std::string& semaphore, const INT size) const
{
	return _producerCreate(name.c_str(), file.c_str(), semaphore.c_str(), size);
}

void LibIPC::IpqLibrary::DestroyProducer(PVOID producer) const
{
	_producerDestroy(producer);
}

INT LibIPC::IpqLibrary::Enqueue(PVOID producer, BYTE* data, const INT size) const
{
	return _producerEnqueue(producer, data, size);
}

INT LibIPC::IpqLibrary::RegisterProcess(const std::string& name, const std::string& file,
	const INT size, const INT pid) const
{
	return _registerProcess(name.c_str(), file.c_str(), size, pid);
}

PVOID LibIPC::IpqLibrary::CreateConsumer(const std::string& name, const std::string& file,
	const std::string& semaphore, const INT size) const
{
	return _consumerCreate(name.c_str(), file.c_str(), semaphore.c_str(), size);
}

void LibIPC::IpqLibrary::DestroyConsumer(PVOID consumer) const
{
	_consumerDestroy(consumer);
}

INT LibIPC::IpqLibrary::DequeueTimeout(PVOID consumer, BYTE** data, INT* size, const INT timeoutMs) const
{
	return _consumerDequeueTimeout(consumer, data, size, timeoutMs);
}

void LibIPC::IpqLibrary::FreeMemory(BYTE* data) const
{
	_freeMemory(data);
}
