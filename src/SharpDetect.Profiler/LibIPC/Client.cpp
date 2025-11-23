// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <chrono>
#include <utility>

#include "../lib/loguru/loguru.hpp"
#include "../LibProfiler/PAL.h"

#include "Client.h"

const std::string LibIPC::Client::_ipqProducerCreateSymbolName = "ipq_producer_create";
const std::string LibIPC::Client::_ipqProducerDestroySymbolName = "ipq_producer_destroy";
const std::string LibIPC::Client::_ipqProducerEnqueueSymbolName = "ipq_producer_enqueue";
const std::string LibIPC::Client::_ipqConsumerCreateSymbolName = "ipq_consumer_create";
const std::string LibIPC::Client::_ipqConsumerDestroySymbolName = "ipq_consumer_destroy";
const std::string LibIPC::Client::_ipqConsumerDequeueSymbolName = "ipq_consumer_dequeue";
const std::string LibIPC::Client::_ipqFreeMemorySymbolName = "ipq_free_memory";


LibIPC::Client::Client(std::string commandQueueName, std::string commandQueueFile, const UINT commandQueueSize,
					   std::string eventQueueName, std::string eventQueueFile, const UINT eventQueueSize) :
	_ipqName(std::move(eventQueueName)),
	_mmfName(std::move(eventQueueFile)),
	_commandQueueName(std::move(commandQueueName)),
	_commandMmfName(std::move(commandQueueFile)),
	_ipqModuleHandle(nullptr),
	_ffiProducer(nullptr),
	_ffiConsumer(nullptr),
	_eventQueueSize(eventQueueSize),
	_commandQueueSize(commandQueueSize),
	_terminating(false),
	_commandReceivingEnabled(true),
	_commandHandler(nullptr),
	_ipqProducerCreateSymbolAddress(nullptr),
	_ipqProducerDestroySymbolAddress(nullptr),
	_ipqProducerEnqueueSymbolAddress(nullptr),
	_ipqConsumerCreateSymbolAddress(nullptr),
	_ipqConsumerDestroySymbolAddress(nullptr),
	_ipqConsumerDequeueSymbolAddress(nullptr),
	_ipqFreeMemorySymbolAddress(nullptr)
{
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

	// Load all symbols
	_ipqProducerCreateSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerCreateSymbolName);
	_ipqProducerDestroySymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerDestroySymbolName);
	_ipqProducerEnqueueSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqProducerEnqueueSymbolName);
	_ipqConsumerCreateSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqConsumerCreateSymbolName);
	_ipqConsumerDestroySymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqConsumerDestroySymbolName);
	_ipqConsumerDequeueSymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqConsumerDequeueSymbolName);
	_ipqFreeMemorySymbolAddress = LibProfiler::PAL_LoadSymbolAddress(_ipqModuleHandle, _ipqFreeMemorySymbolName);

	if (_ipqProducerCreateSymbolAddress == nullptr ||
		_ipqProducerDestroySymbolAddress == nullptr ||
		_ipqProducerEnqueueSymbolAddress == nullptr ||
		_ipqConsumerCreateSymbolAddress == nullptr ||
		_ipqConsumerDestroySymbolAddress == nullptr ||
		_ipqConsumerDequeueSymbolAddress == nullptr ||
		_ipqFreeMemorySymbolAddress == nullptr)
	{
		LOG_F(FATAL, "Communication library does not contain expected symbols.");
		throw std::runtime_error("Incompatibility issue while loading communication library symbols.");
	}
	
	// Create producer for events
	LOG_F(INFO, "IPC event worker configuration: { name: %s, file: %s, size: %d }", _ipqName.c_str(), _mmfName.c_str(), _eventQueueSize);
	_ffiProducer = reinterpret_cast<ipq_producer_create>(_ipqProducerCreateSymbolAddress)(_ipqName.c_str(), _mmfName.c_str(), _eventQueueSize);
	if (_ffiProducer == nullptr)
	{
		LOG_F(FATAL, "Communication library could not create producer.");
		throw std::runtime_error("Could not obtain write access to IPC event queue.");
	}

	// Create consumer for commands
	LOG_F(INFO, "IPC command worker configuration: { name: %s, file: %s, size: %d }", _commandQueueName.c_str(), _commandMmfName.c_str(), _commandQueueSize);
	_ffiConsumer = reinterpret_cast<ipq_consumer_create>(_ipqConsumerCreateSymbolAddress)(_commandQueueName.c_str(), _commandMmfName.c_str(), _commandQueueSize);
	if (_ffiConsumer == nullptr)
	{
		LOG_F(FATAL, "Communication library could not create consumer.");
		throw std::runtime_error("Could not obtain read access to IPC command queue.");
	}

	LOG_F(INFO, "Communication library initialized with command receiving enabled.");
	_eventThread = std::thread(&LibIPC::Client::EventThreadLoop, this);
	_commandThread = std::thread(&LibIPC::Client::CommandThreadLoop, this);
}

void LibIPC::Client::EventThreadLoop()
{
	LOG_F(INFO, "IPC event worker thread started.");
	const auto enqueueFn = reinterpret_cast<ipq_producer_enqueue>(_ipqProducerEnqueueSymbolAddress);
	while (!_terminating)
	{
		auto lock = std::unique_lock<std::mutex>(_eventMutex);
		if (_eventQueueNonEmptySignal.wait_for(lock, std::chrono::seconds(2)) == std::cv_status::timeout || _eventQueue.empty())
			continue;

		while (!_eventQueue.empty())
		{
			auto&& item = std::move(_eventQueue.front());

			auto byteStream = reinterpret_cast<BYTE*>(item.data());
			INT result = 0;
			do
			{
				if (result != 0)
					std::this_thread::yield();

				result = enqueueFn(_ffiProducer, byteStream, item.size());
			}
			while (result != 0);

			_eventQueue.pop();
		}
	}
	LOG_F(INFO, "IPC event worker thread terminated.");
}

void LibIPC::Client::CommandThreadLoop()
{
	LOG_F(INFO, "IPC command worker thread started.");
	const auto dequeueFn = reinterpret_cast<ipq_consumer_dequeue>(_ipqConsumerDequeueSymbolAddress);
	const auto freeMemoryFn = reinterpret_cast<ipq_free_memory>(_ipqFreeMemorySymbolAddress);

	while (!_terminating)
	{
		BYTE* dataPtr = nullptr;
		INT size = 0;

		auto result = dequeueFn(_ffiConsumer, &dataPtr, &size);
		if (result != 0)
		{
			std::this_thread::yield();
			continue;
		}

		try
		{
			msgpack::object_handle objectHandle = msgpack::unpack(reinterpret_cast<const char*>(dataPtr), size);
			const msgpack::object obj = objectHandle.get();

			// Extract command structure: [[pid, commandId], [discriminator, args]]
			if (obj.type != msgpack::type::ARRAY || obj.via.array.size != 2)
			{
				LOG_F(WARNING, "Invalid command message format.");
				freeMemoryFn(dataPtr);
				continue;
			}

			// Extract metadata: [pid, commandId]
			const auto& metadataObj = obj.via.array.ptr[0];
			if (metadataObj.type != msgpack::type::ARRAY || metadataObj.via.array.size != 2)
			{
				LOG_F(WARNING, "Invalid command metadata format.");
				freeMemoryFn(dataPtr);
				continue;
			}
			
			const UINT64 commandId = metadataObj.via.array.ptr[1].as<UINT64>();
			
			// Extract union: [discriminator, args]
			const auto& unionObj = obj.via.array.ptr[1];
			if (unionObj.type != msgpack::type::ARRAY || unionObj.via.array.size != 2)
			{
				LOG_F(WARNING, "Invalid command union format.");
				freeMemoryFn(dataPtr);
				continue;
			}

			INT32 discriminator = unionObj.via.array.ptr[0].as<INT32>();
			const auto& argsObj = unionObj.via.array.ptr[1];

			// Handle based on command type
			if (_commandHandler != nullptr)
			{
				switch (static_cast<ProfilerCommandType>(discriminator))
				{
				case ProfilerCommandType::CreateStackSnapshot:
				{
					if (argsObj.type == msgpack::type::ARRAY && argsObj.via.array.size == 1)
					{
						const UINT64 targetThreadId = argsObj.via.array.ptr[0].as<UINT64>();
						_commandHandler->OnCreateStackSnapshot(commandId, targetThreadId);
					}
					else
					{
						LOG_F(WARNING, "Invalid CreateStackSnapshot arguments.");
					}
					break;
				}
				case ProfilerCommandType::CreateStackSnapshots:
				{
					if (argsObj.type == msgpack::type::ARRAY && argsObj.via.array.size == 1)
					{
						auto threadIds = argsObj.via.array.ptr[0].as<std::vector<UINT64>>();
						_commandHandler->OnCreateStackSnapshots(commandId, threadIds);
					}
					else
					{
						LOG_F(WARNING, "Invalid CreateStackSnapshots arguments.");
					}
					break;
				}
				default:
					LOG_F(WARNING, "Unknown command type. Discriminator: %d", discriminator);
					break;
				}
			}
		}
		catch (const std::exception& ex)
		{
			LOG_F(ERROR, "Error processing command: %s", ex.what());
		}

		freeMemoryFn(dataPtr);
	}
	LOG_F(INFO, "IPC command worker thread terminated.");
}

void LibIPC::Client::SetCommandHandler(ICommandHandler* handler)
{
	_commandHandler = handler;
}

LibIPC::Client::~Client()
{
	if (_ffiProducer == nullptr)
		return;

	_terminating = true;
	if (_eventThread.joinable())
		_eventThread.join();

	if (_commandReceivingEnabled && _commandThread.joinable())
		_commandThread.join();

	reinterpret_cast<ipq_producer_destroy>(_ipqProducerDestroySymbolAddress)(_ffiProducer);
	_ffiProducer = nullptr;

	if (_commandReceivingEnabled && _ffiConsumer != nullptr)
	{
		reinterpret_cast<ipq_consumer_destroy>(_ipqConsumerDestroySymbolAddress)(_ffiConsumer);
		_ffiConsumer = nullptr;
	}
}