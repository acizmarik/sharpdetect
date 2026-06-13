// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "MethodInjectionDescriptor.h"

namespace Profiler
{
	struct TypeInjectionDescriptor
	{
		std::string typeFullName;
		std::vector<MethodInjectionDescriptor> methods;
	};

	void to_json(nlohmann::json& json, const TypeInjectionDescriptor& descriptor);
	void from_json(const nlohmann::json& json, TypeInjectionDescriptor& descriptor);
}