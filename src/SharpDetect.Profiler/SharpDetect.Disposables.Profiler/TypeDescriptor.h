// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "cor.h"

namespace Profiler
{
	struct TypeDescriptor
	{
		std::string fullName;
	};

	void to_json(nlohmann::json& json, const TypeDescriptor& descriptor);
	void from_json(const nlohmann::json& json, TypeDescriptor& descriptor);
}