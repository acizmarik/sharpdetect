// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>
#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "cor.h"

#include "CapturedArgumentDescriptor.h"
#include "CapturedValueDescriptor.h"

namespace Profiler
{
	enum class FieldAddressAccessInterpretation : UINT8
	{
		AtomicReadModifyWrite = 1,
		VolatileRead = 2,
		VolatileWrite = 3
	};

	struct MethodRewritingDescriptor
	{
		BOOL injectHooks;
		BOOL injectManagedWrapper;
		std::vector<CapturedArgumentDescriptor> arguments;
		std::optional<CapturedValueDescriptor> returnValue;
		std::optional<USHORT> methodEnterInterpretation;
		std::optional<USHORT> methodExitInterpretation;
		BOOL emitExitEvent;
		BOOL captureStackTraceOnEnter;
		std::optional<FieldAddressAccessInterpretation> fieldAddressAccessInterpretation;
	};

    void to_json(nlohmann::json& json, const MethodRewritingDescriptor& descriptor);
	void from_json(const nlohmann::json& json, MethodRewritingDescriptor& descriptor);
}