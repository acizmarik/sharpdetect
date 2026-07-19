// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <thread>
#include <vector>

#include "cor.h"
#include "IpqConsumer.h"

namespace LibIPC
{
	class ICommandHandler
	{
	public:
		virtual ~ICommandHandler() = default;
		virtual void OnCreateStackSnapshot(UINT64 commandId, UINT64 targetThreadId) = 0;
		virtual void OnCreateStackSnapshots(UINT64 commandId, const std::vector<UINT64>& targetThreadIds) = 0;
	};

	class CommandDispatcher
	{
	public:
		explicit CommandDispatcher(const IpqConsumer& consumer);
		~CommandDispatcher() = default;
		CommandDispatcher(const CommandDispatcher&) = delete;
		CommandDispatcher& operator=(const CommandDispatcher&) = delete;
		CommandDispatcher(CommandDispatcher&&) = delete;
		CommandDispatcher& operator=(CommandDispatcher&&) = delete;

		void SetCommandHandler(ICommandHandler* handler) { _commandHandler = handler; }

		void Start();
		void Stop();

	private:
		void CommandThreadLoop();

		const IpqConsumer& _consumer;
		std::thread _thread;
		std::atomic_bool _terminating;
		ICommandHandler* _commandHandler;
	};
}
