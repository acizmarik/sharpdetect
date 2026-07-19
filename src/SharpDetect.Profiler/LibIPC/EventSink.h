// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <vector>

namespace LibIPC
{
	class IEventSink
	{
	public:
		virtual ~IEventSink() = default;
		virtual void Send(std::vector<char>& buffer) = 0;
	};
}
