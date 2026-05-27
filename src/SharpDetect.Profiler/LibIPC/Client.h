// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <condition_variable>
#include <mutex>
#include <string>
#include <thread>
#include <queue>
#include <vector>

#include "../lib/msgpack-c/include/msgpack.hpp"
#include "cor.h"
#include "Messages.h"

#include "../LibProfiler/PAL.h"

namespace LibIPC
{
	class ICommandHandler
	{
	public:
		virtual ~ICommandHandler() = default;
		virtual void OnCreateStackSnapshot(UINT64 commandId, UINT64 targetThreadId) = 0;
		virtual void OnCreateStackSnapshots(UINT64 commandId, const std::vector<UINT64>& targetThreadIds) = 0;
	};

	class Client
	{
	public:
		Client(std::string commandQueueName, std::string commandQueueFile, UINT commandQueueSize,
			   std::string commandSemaphoreName,
			   std::string eventQueueName, std::string eventQueueFile, UINT eventQueueSize,
			   std::string eventSemaphoreName);
		Client(Client&& other) = delete;
		Client& operator=(Client&&) = delete;
		Client(Client& other) = delete;
		Client& operator=(const Client&) = delete;
		~Client();

		template<class... Types>
		void Send(msgpack::type::tuple<Types...>&& data)
		{
			thread_local msgpack::sbuffer buffer;
			buffer.clear();
			msgpack::pack(buffer, data);
			const auto bufferSize = buffer.size();
			std::vector<char> compact(buffer.data(), buffer.data() + bufferSize);
			{
				std::unique_lock<std::mutex> lock(_eventMutex);
				_eventQueueDrainedSignal.wait(lock, [this, bufferSize] {
					return _terminating
						|| _eventQueueBytes + bufferSize <= _eventQueueMaxBytes
						|| _eventQueue.empty();
				});
				_eventQueueBytes += bufferSize;
				_eventQueue.emplace(std::move(compact));
			}

			_eventQueueNonEmptySignal.notify_one();
		}

		template<class... Types>
		void SendPriority(msgpack::type::tuple<Types...>&& data)
		{
			thread_local msgpack::sbuffer buffer;
			buffer.clear();
			msgpack::pack(buffer, data);
			std::vector<char> compact(buffer.data(), buffer.data() + buffer.size());
			{
				std::lock_guard<std::mutex> guard(_eventMutex);
				_eventQueueBytes += compact.size();
				_eventQueue.emplace(std::move(compact));
			}

			_eventQueueNonEmptySignal.notify_one();
		}

		void SetCommandHandler(ICommandHandler* handler);
		[[nodiscard]] bool IsCommandReceivingEnabled() const { return _commandReceivingEnabled; }

	private:
		using ipq_producer_create = PVOID(*)(const char*, const char*, const char*, INT);
		using ipq_producer_destroy = void (*)(PVOID);
		using ipq_producer_enqueue = INT(*)(PVOID, BYTE*, INT);
		using ipq_consumer_create = PVOID(*)(const char*, const char*, const char*, INT);
		using ipq_consumer_destroy = void (*)(PVOID);
		using ipq_consumer_dequeue = INT(*)(PVOID, BYTE**, INT*);
		using ipq_consumer_dequeue_timeout = INT(*)(PVOID, BYTE**, INT*, INT);
		using ipq_free_memory = void (*)(BYTE*);

		void EventThreadLoop();
		void CommandThreadLoop();
		void SendDirect(ipq_producer_enqueue enqueueFn, std::vector<char>& buffer);
		void DrainQueues(ipq_producer_enqueue enqueueFn);

		static const std::string _ipqProducerCreateSymbolName;
		static const std::string _ipqProducerDestroySymbolName;
		static const std::string _ipqProducerEnqueueSymbolName;
		static const std::string _ipqConsumerCreateSymbolName;
		static const std::string _ipqConsumerDestroySymbolName;
		static const std::string _ipqConsumerDequeueSymbolName;
		static const std::string _ipqConsumerDequeueTimeoutSymbolName;
		static const std::string _ipqFreeMemorySymbolName;

		std::string _ipqName;
		std::string _mmfName;
		std::string _eventSemaphoreName;
		std::string _commandQueueName;
		std::string _commandMmfName;
		std::string _commandSemaphoreName;
		MODULE_HANDLE _ipqModuleHandle;
		PVOID _ffiProducer;
		PVOID _ffiConsumer;
		INT _eventQueueSize;
		INT _commandQueueSize;
		std::thread _eventThread;
		std::thread _commandThread;
		std::mutex _eventMutex;
		std::atomic_bool _terminating;
		std::condition_variable _eventQueueNonEmptySignal;
		std::condition_variable _eventQueueDrainedSignal;
		std::queue<std::vector<char>> _eventQueue;
		std::size_t _eventQueueBytes;
		std::size_t _eventQueueMaxBytes;
		bool _commandReceivingEnabled;
		ICommandHandler* _commandHandler;

		PVOID _ipqProducerCreateSymbolAddress;
		PVOID _ipqProducerDestroySymbolAddress;
		PVOID _ipqProducerEnqueueSymbolAddress;
		PVOID _ipqConsumerCreateSymbolAddress;
		PVOID _ipqConsumerDestroySymbolAddress;
		PVOID _ipqConsumerDequeueSymbolAddress;
		PVOID _ipqConsumerDequeueTimeoutSymbolAddress;
		PVOID _ipqFreeMemorySymbolAddress;
	};
}
