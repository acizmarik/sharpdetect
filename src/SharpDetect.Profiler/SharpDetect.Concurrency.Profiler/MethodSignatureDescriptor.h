// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <vector>

#include "cor.h"
#include "ArgumentTypeDescriptor.h"
#include "../lib/json/single_include/nlohmann/json.hpp"

namespace Profiler
{
	struct MethodSignatureDescriptor
	{
		CorCallingConvention callingConvention;
		BYTE parametersCount;
		ArgumentTypeDescriptor returnType;
		std::vector<ArgumentTypeDescriptor> argumentTypeElements;
	};

    void to_json(nlohmann::json& json, const MethodSignatureDescriptor& descriptor);
	void from_json(const nlohmann::json& json, MethodSignatureDescriptor& descriptor);
}