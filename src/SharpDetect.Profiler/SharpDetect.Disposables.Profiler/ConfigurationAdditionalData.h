// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "MethodDescriptor.h"
#include "TypeDescriptor.h"

namespace Profiler
{
	struct ConfigurationAdditionalData
	{
		std::vector<TypeDescriptor> typesToIgnore;
		std::vector<MethodDescriptor> methodsToInclude;
	};

	void to_json(nlohmann::json& json, const ConfigurationAdditionalData& descriptor);
	void from_json(const nlohmann::json& json, ConfigurationAdditionalData& descriptor);
}