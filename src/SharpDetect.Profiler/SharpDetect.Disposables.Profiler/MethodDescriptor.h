// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "cor.h"

namespace Profiler
{
	struct MethodDescriptor
	{
		std::string name;
		std::string declaringTypeFullName;
	};

	void to_json(nlohmann::json& json, const MethodDescriptor& descriptor);
	void from_json(const nlohmann::json& json, MethodDescriptor& descriptor);
}