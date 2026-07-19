// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>
#include <vector>

#include "cor.h"
#include "EventSink.h"
#include "IpqLibrary.h"

namespace LibIPC
{
	class IpqProducer : public IEventSink
	{
	public:
		IpqProducer(
			const IpqLibrary& library,
			const std::string& name,
			const std::string& file,
			const std::string& semaphore,
			INT size);
		~IpqProducer() override;
		IpqProducer(const IpqProducer&) = delete;
		IpqProducer& operator=(const IpqProducer&) = delete;
		IpqProducer(IpqProducer&&) = delete;
		IpqProducer& operator=(IpqProducer&&) = delete;

		void Send(std::vector<char>& buffer) override;

	private:
		const IpqLibrary& _library;
		PVOID _handle;
	};
}
