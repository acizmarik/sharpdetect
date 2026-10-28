// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <unordered_map>

#include "cor.h"

namespace LibProfiler
{
	enum class FieldAccessIntrinsicKind : UINT8
	{
		VolatileRead,
		VolatileWrite,
		ReadModifyWrite,
		CompareExchange
	};

	// Instrumentation marks reserved for each intrinsic call site
	constexpr UINT64 IntrinsicReleaseMarkOffset = 0;
	constexpr UINT64 IntrinsicAcquireMarkOffset = 1;
	constexpr UINT64 IntrinsicFailedExchangeMarkOffset = IntrinsicAcquireMarkOffset;
	constexpr UINT64 IntrinsicSucceededExchangeMarkOffset = 2;
	constexpr UINT64 IntrinsicStoreCompletedMarkOffset = IntrinsicAcquireMarkOffset;
	constexpr UINT64 IntrinsicMarksCount = 3;

	using FieldAccessIntrinsicsMap = std::unordered_map<mdMemberRef, FieldAccessIntrinsicKind>;
}
