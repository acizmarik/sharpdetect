// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <condition_variable>
#include <mutex>
#include <string>
#include <thread>
#include <queue>

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
			   std::string eventQueueName, std::string eventQueueFile, UINT eventQueueSize);
		Client(Client&& other) = delete;
		Client& operator=(Client&&) = delete;
		Client(Client& other) = delete;
		Client& operator=(const Client&) = delete;
		~Client();

		template<class... Types>
		void Send(msgpack::type::tuple<Types...>&& data)
		{
			msgpack::sbuffer buffer;
			msgpack::pack(buffer, data);
			{
				std::lock_guard<std::mutex> guard(_eventMutex);
				_eventQueue.emplace(std::move(buffer));
			}

			_eventQueueNonEmptySignal.notify_one();
		}

		void SetCommandHandler(ICommandHandler* handler);
		bool IsCommandReceivingEnabled() const { return _commandReceivingEnabled; }

	private:
		void EventThreadLoop();
		void CommandThreadLoop();

		static const std::string _ipqProducerCreateSymbolName;
		static const std::string _ipqProducerDestroySymbolName;
		static const std::string _ipqProducerEnqueueSymbolName;
		static const std::string _ipqConsumerCreateSymbolName;
		static const std::string _ipqConsumerDestroySymbolName;
		static const std::string _ipqConsumerDequeueSymbolName;
		static const std::string _ipqFreeMemorySymbolName;

		typedef PVOID(*ipq_producer_create)(const char*, const char*, INT);
		typedef void (*ipq_producer_destroy)(PVOID);
		typedef INT(*ipq_producer_enqueue)(PVOID, BYTE*, INT);
		typedef PVOID(*ipq_consumer_create)(const char*, const char*, INT);
		typedef void (*ipq_consumer_destroy)(PVOID);
		typedef INT(*ipq_consumer_dequeue)(PVOID, BYTE**, INT*);
		typedef void (*ipq_free_memory)(BYTE*);

		std::string _ipqName;
		std::string _mmfName;
		std::string _commandQueueName;
		std::string _commandMmfName;
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
		std::queue<msgpack::sbuffer> _eventQueue;
		bool _commandReceivingEnabled;
		ICommandHandler* _commandHandler;

		PVOID _ipqProducerCreateSymbolAddress;
		PVOID _ipqProducerDestroySymbolAddress;
		PVOID _ipqProducerEnqueueSymbolAddress;
		PVOID _ipqConsumerCreateSymbolAddress;
		PVOID _ipqConsumerDestroySymbolAddress;
		PVOID _ipqConsumerDequeueSymbolAddress;
		PVOID _ipqFreeMemorySymbolAddress;
	};
}