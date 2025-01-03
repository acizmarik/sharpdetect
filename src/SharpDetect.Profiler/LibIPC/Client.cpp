// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <chrono>
#include <filesystem>

#include "../lib/loguru/loguru.hpp"
#include "../LibProfiler/PAL.h"

#include "Client.h"

LibIPC::Client::Client(const std::string& ipqName, const std::string& mmfName, INT size) : 
	_ipqName(ipqName), 
	_mmfName(mmfName), 
	_queueSize(size), 
	_ipqModuleHandle(nullptr),
	_ffiProducer(nullptr),
	_ipqProducerCreateSymbolAddress(nullptr),
	_ipqProducerDestroySymbolAddress(nullptr),
	_ipqProducerEnqueueSymbolAddress(nullptr)
{
	auto const profilerLibraryPath = std::string(std::getenv("CORECLR_PROFILER_PATH"));
	auto const profilerLibraryDirectory = std::filesystem::path(profilerLibraryPath).parent_path();
	auto const profilerLibraryName = std::filesystem::path(_ipqLibraryName);
	auto const ipqPath = profilerLibraryDirectory / profilerLibraryName;

	_ipqModuleHandle = LibProfiler::PAL_LoadLibrary(ipqPath.generic_string());
	if (_ipqModuleHandle == nullptr)
	{
		LOG_F(FATAL, "Could not load %s.", ipqPath.generic_string().c_str());
		throw std::exception("Error while loading communication library.");
	}

	_ipqProducerCreateSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerCreateSymbolName);
	_ipqProducerDestroySymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerDestroySymbolName);
	_ipqProducerEnqueueSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerEnqueueSymbolName);
	if (_ipqProducerCreateSymbolAddress == nullptr ||
		_ipqProducerDestroySymbolAddress == nullptr ||
		_ipqProducerEnqueueSymbolAddress == nullptr)
	{
		LOG_F(FATAL, "Communication library does not contain expected symbols.");
		throw std::exception("Incompatibility issue while loading communication library symbols.");
	}
	
	_ffiProducer = reinterpret_cast<ipq_producer_create>(_ipqProducerCreateSymbolAddress)(_ipqName.c_str(), _mmfName.c_str(), _queueSize);
	if (_ffiProducer == nullptr)
	{
		LOG_F(FATAL, "Communication library could not create producer.");
		throw std::exception("Could not obtain write access to IPC queue.");
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