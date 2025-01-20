// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "../lib/optional/include/tl/optional.hpp"

#include "cor.h"

#include "CapturedArgumentDescriptor.h"
#include "CapturedValueDescriptor.h"
#include "RecordedEventType.h"

namespace Profiler
{
	struct MethodRewritingDescriptor
	{
		BOOL injectHooks;
		BOOL injectManagedWrapper;
		std::vector<CapturedArgumentDescriptor> arguments;
		tl::optional<CapturedValueDescriptor> returnValue;
		tl::optional<USHORT> methodEnterInterpretation;
		tl::optional<USHORT> methodExitInterpretation;
	};

    void to_json(nlohmann::json& json, const MethodRewritingDescriptor& descriptor);
	void from_json(const nlohmann::json& json, MethodRewritingDescriptor& descriptor);
}