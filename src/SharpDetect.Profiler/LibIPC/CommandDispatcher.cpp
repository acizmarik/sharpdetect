// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "../lib/loguru/loguru.hpp"
#include "../lib/msgpack-c/include/msgpack.hpp"

#include "CommandDispatcher.h"
#include "Messages.h"

LibIPC::CommandDispatcher::CommandDispatcher(const IpqConsumer& consumer) :
	_consumer(consumer),
	_terminating(false),
	_commandHandler(nullptr)
{
}

void LibIPC::CommandDispatcher::Start()
{
	_thread = std::thread(&LibIPC::CommandDispatcher::CommandThreadLoop, this);
}

void LibIPC::CommandDispatcher::Stop()
{
	_terminating.store(true, std::memory_order_release);
	if (_thread.joinable())
		_thread.join();
}

void LibIPC::CommandDispatcher::CommandThreadLoop()
{
	LOG_F(INFO, "IPC command worker thread started.");

	while (!_terminating)
	{
		BYTE* dataPtr = nullptr;
		INT size = 0;

		if (!_consumer.TryDequeue(&dataPtr, &size, 1000))
			continue;

		try
		{
			msgpack::object_handle objectHandle = msgpack::unpack(reinterpret_cast<const char*>(dataPtr), size);
			const msgpack::object obj = objectHandle.get();

			// Extract command structure: [[pid, commandId], [discriminator, args]]
			if (obj.type != msgpack::type::ARRAY || obj.via.array.size != 2)
			{
				LOG_F(WARNING, "Invalid command message format.");
				_consumer.Free(dataPtr);
				continue;
			}

			// Extract metadata: [pid, commandId]
			const auto& metadataObj = obj.via.array.ptr[0];
			if (metadataObj.type != msgpack::type::ARRAY || metadataObj.via.array.size != 2)
			{
				LOG_F(WARNING, "Invalid command metadata format.");
				_consumer.Free(dataPtr);
				continue;
			}

			const UINT64 commandId = metadataObj.via.array.ptr[1].as<UINT64>();

			// Extract union: [discriminator, args]
			const auto& unionObj = obj.via.array.ptr[1];
			if (unionObj.type != msgpack::type::ARRAY || unionObj.via.array.size != 2)
			{
				LOG_F(WARNING, "Invalid command union format.");
				_consumer.Free(dataPtr);
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

		_consumer.Free(dataPtr);
	}
	LOG_F(INFO, "IPC command worker thread terminated.");
}
