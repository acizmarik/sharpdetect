// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <chrono>

#include "../lib/loguru/loguru.hpp"
#include "../LibProfiler/PAL.h"

#include "Client.h"

const std::string LibIPC::Client::_ipqProducerCreateSymbolName = "ipq_producer_create";
const std::string LibIPC::Client::_ipqProducerDestroySymbolName = "ipq_producer_destroy";
const std::string LibIPC::Client::_ipqProducerEnqueueSymbolName = "ipq_producer_enqueue";

LibIPC::Client::Client() : 
	_ipqName({}),
	_mmfName({}),
	_queueSize(20971520 /* 20MB */),
	_ipqModuleHandle(nullptr),
	_ffiProducer(nullptr),
	_ipqProducerCreateSymbolAddress(nullptr),
	_ipqProducerDestroySymbolAddress(nullptr),
	_ipqProducerEnqueueSymbolAddress(nullptr)
{
	auto const sharedMemoryNameStringPointer = std::getenv("SharpDetect_SHAREDMEMORY_NAME");
	if (sharedMemoryNameStringPointer == nullptr)
	{
		LOG_F(FATAL, "Could not obtain memory map name.");
		throw std::runtime_error("Error while configuring memory mapped file.");
	}
	_ipqName = std::string(sharedMemoryNameStringPointer);

	auto const sharedMemoryFileStringPointer = std::getenv("SharpDetect_SHAREDMEMORY_FILE");
	_mmfName = (sharedMemoryFileStringPointer != nullptr) ? std::string(sharedMemoryFileStringPointer) : std::string();

	auto const ipqPathStringPointer = std::getenv("SharpDetect_IPQ_PATH");
	if (ipqPathStringPointer == nullptr)
	{
		LOG_F(FATAL, "Could not obtain path to IPQ library.");
		throw std::runtime_error("Error while configuring memory mapped file.");
	}
	auto const ipqPath = std::string(ipqPathStringPointer);

	_ipqModuleHandle = LibProfiler::PAL_LoadLibrary(ipqPath);
	if (_ipqModuleHandle == nullptr)
	{
		LOG_F(FATAL, "Could not load %s.", ipqPath.c_str());
		throw std::runtime_error("Error while loading communication library.");
	}

	_ipqProducerCreateSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerCreateSymbolName);
	_ipqProducerDestroySymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerDestroySymbolName);
	_ipqProducerEnqueueSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerEnqueueSymbolName);
	if (_ipqProducerCreateSymbolAddress == nullptr ||
		_ipqProducerDestroySymbolAddress == nullptr ||
		_ipqProducerEnqueueSymbolAddress == nullptr)
	{
		LOG_F(FATAL, "Communication library does not contain expected symbols.");
		throw std::runtime_error("Incompatibility issue while loading communication library symbols.");
	}
	
	_ffiProducer = reinterpret_cast<ipq_producer_create>(_ipqProducerCreateSymbolAddress)(_ipqName.c_str(), _mmfName.c_str(), _queueSize);
	if (_ffiProducer == nullptr)
	{
		LOG_F(FATAL, "Communication library could not create producer.");
		throw std::runtime_error("Could not obtain write access to IPC queue.");
	}

	LOG_F(INFO, "Communication library initialized.");
	_thread = std::thread(&LibIPC::Client::ThreadLoop, this);
}

void LibIPC::Client::ThreadLoop()
{
	LOG_F(INFO, "IPC worker thread started.");
	auto enqueueFn = reinterpret_cast<ipq_producer_enqueue>(_ipqProducerEnqueueSymbolAddress);
	while (!_terminating)
	{
		auto lock = std::unique_lock<std::mutex>(_mutex);
		if (_queueNonEmptySignal.wait_for(lock, std::chrono::seconds(2)) == std::cv_status::timeout || _queue.empty())
			continue;

		while (!_queue.empty())
		{
			auto&& item = std::move(_queue.front());

			auto byteStream = reinterpret_cast<BYTE*>(item.data());
			INT result = 0;
			do
			{
				if (result != 0)
					std::this_thread::yield();

				result = enqueueFn(_ffiProducer, byteStream, item.size());
			}
			while (result != 0);

			_queue.pop();
		}
	}
	LOG_F(INFO, "IPC worker thread terminated.");
}

LibIPC::Client::~Client()
{
	if (_ffiProducer == nullptr)
		return;

	_terminating = true;
	_thread.join();

	reinterpret_cast<ipq_producer_destroy>(_ipqProducerDestroySymbolAddress)(_ffiProducer);
	_ffiProducer = nullptr;
}