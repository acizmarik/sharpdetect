// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>
#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "../lib/optional/include/tl/optional.hpp"

#include "cor.h"

namespace Profiler
{
	struct Configuration
	{
		UINT eventMask;
		std::string sharedMemoryName;
		tl::optional<std::string> sharedMemoryFile;
		UINT sharedMemorySize;
	};

	void to_json(nlohmann::json& json, const Configuration& descriptor);
	void from_json(const nlohmann::json& json, Configuration& descriptor);
}