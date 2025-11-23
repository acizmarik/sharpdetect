// Copyright 2025 Andrej Čižmárik and Contributors
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
	struct MethodRewritingDescriptor
	{
		BOOL injectHooks;
		BOOL injectManagedWrapper;
		std::vector<CapturedArgumentDescriptor> arguments;
		std::optional<CapturedValueDescriptor> returnValue;
		std::optional<USHORT> methodEnterInterpretation;
		std::optional<USHORT> methodExitInterpretation;
	};

    void to_json(nlohmann::json& json, const MethodRewritingDescriptor& descriptor);
	void from_json(const nlohmann::json& json, MethodRewritingDescriptor& descriptor);
}