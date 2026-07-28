// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstddef>
#include <cstdint>
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
		static constexpr std::size_t RecordHeaderSize = sizeof(std::int32_t);
		static constexpr std::size_t FlushThresholdBytes = 64 * 1024;
		static constexpr std::size_t BatchSlackBytes = 4 * 1024;

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
		void Flush() override;

	private:
		void SendMessage(char* data, std::size_t size);
		const IpqLibrary& _library;
		PVOID _handle;
		std::vector<char> _batch;
	};
}
