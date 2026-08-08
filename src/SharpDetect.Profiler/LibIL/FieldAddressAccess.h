// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <unordered_map>

#include "cor.h"

#include "../LibIPC/Messages.h"

namespace LibProfiler
{
	enum class FieldAddressAccessDirection : UINT8
	{
		Read,
		Write
	};

	struct FieldAddressAccessEffect
	{
		FieldAddressAccessDirection direction { FieldAddressAccessDirection::Read };
		LibIPC::FieldAccessKind accessKind { LibIPC::FieldAccessKind::Regular };
	};

	using FieldAddressAccessTokens = std::unordered_map<mdToken, FieldAddressAccessEffect>;
}
