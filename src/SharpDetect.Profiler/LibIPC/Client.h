// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <memory>
#include <string>

#include "../lib/msgpack-c/include/msgpack.hpp"
#include "cor.h"
#include "CommandDispatcher.h"
#include "EventDispatcher.h"
#include "IpqConsumer.h"
#include "IpqLibrary.h"
#include "IpqProducer.h"
#include "QueueEndpoint.h"

namespace LibIPC
{

	class Client
	{
	public:
		Client(
			const QueueEndpoint& commandQueue,
			const QueueEndpoint& eventQueue,
			const RegistrationEndpoint& registrationQueue);
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
			_events->Enqueue(buffer.data(), buffer.size());
		}

		template<class... Types>
		void SendPriority(msgpack::type::tuple<Types...>&& data)
		{
			thread_local msgpack::sbuffer buffer;
			buffer.clear();
			msgpack::pack(buffer, data);
			_events->EnqueuePriority(buffer.data(), buffer.size());
		}

		void SetCommandHandler(ICommandHandler* handler)
		{
		    _commands->SetCommandHandler(handler);
		}
		[[nodiscard]] bool IsCommandReceivingEnabled() const { return _commandReceivingEnabled; }

	private:
		bool _commandReceivingEnabled;
		std::unique_ptr<IpqLibrary> _library;
		std::unique_ptr<IpqProducer> _producer;
		std::unique_ptr<IpqConsumer> _consumer;
		std::unique_ptr<EventDispatcher> _events;
		std::unique_ptr<CommandDispatcher> _commands;
	};
}
