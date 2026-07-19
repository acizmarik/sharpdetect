// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "cor.h"
#include "IpqLibrary.h"

namespace LibIPC
{
	class IpqConsumer
	{
	public:
		IpqConsumer(
			const IpqLibrary& library,
			const std::string& name,
			const std::string& file,
			const std::string& semaphore,
			INT size);
		~IpqConsumer();
		IpqConsumer(const IpqConsumer&) = delete;
		IpqConsumer& operator=(const IpqConsumer&) = delete;
		IpqConsumer(IpqConsumer&&) = delete;
		IpqConsumer& operator=(IpqConsumer&&) = delete;
		
		[[nodiscard]] bool TryDequeue(BYTE** data, INT* size, INT timeoutMs) const;
		void Free(BYTE* data) const;

	private:
		const IpqLibrary& _library;
		PVOID _handle;
	};
}
