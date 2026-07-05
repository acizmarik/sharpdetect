// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <unordered_map>

#include "cor.h"

#include "../LibIPC/Messages.h"

namespace LibProfiler
{
	struct InjectedMethodTokens
	{
		mdToken plain { mdTokenNil };
		mdToken withStackCapture { mdTokenNil };
	};

	using InjectedMethodsMap = std::unordered_map<LibIPC::RecordedEventType, InjectedMethodTokens>;
}
