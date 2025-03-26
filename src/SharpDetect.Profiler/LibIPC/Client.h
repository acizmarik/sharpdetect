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

#include "../LibProfiler/PAL.h"

namespace LibIPC
{
	class Client
	{
	public:
		Client(std::string mmName, std::string mmFile, UINT size);
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
				std::lock_guard<std::mutex> guard(_mutex);
				_queue.emplace(std::move(buffer));
			}

			_queueNonEmptySignal.notify_one();
		}

	private:
		void ThreadLoop();

		static const std::string _ipqProducerCreateSymbolName;
		static const std::string _ipqProducerDestroySymbolName;
		static const std::string _ipqProducerEnqueueSymbolName;
		typedef PVOID(*ipq_producer_create)(const char*, const char*, INT);
		typedef void (*ipq_producer_destroy)(PVOID);
		typedef INT(*ipq_producer_enqueue)(PVOID, BYTE*, INT);

		std::string _ipqName;
		std::string _mmfName;
		MODULE_HANDLE _ipqModuleHandle;
		PVOID _ffiProducer;
		INT _queueSize;
		std::thread _thread;
		std::mutex _mutex;
		std::atomic_bool _terminating;
		std::condition_variable _queueNonEmptySignal;
		std::queue<msgpack::sbuffer> _queue;

		PVOID _ipqProducerCreateSymbolAddress;
		PVOID _ipqProducerDestroySymbolAddress;
		PVOID _ipqProducerEnqueueSymbolAddress;
	};
}