// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <unordered_map>

#include "cor.h"

#include "../LibIPC/Messages.h"

namespace LibProfiler
{
	enum class FieldAccessDirection : UINT8
	{
		Read,
		Write
	};

	struct FieldAccessIntrinsicEffect
	{
		FieldAccessDirection direction { FieldAccessDirection::Read };
		LibIPC::FieldAccessKind accessKind { LibIPC::FieldAccessKind::Regular };
	};

	using FieldAccessIntrinsicsMap = std::unordered_map<mdMemberRef, FieldAccessIntrinsicEffect>;
}
