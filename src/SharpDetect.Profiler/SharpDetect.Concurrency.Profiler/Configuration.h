// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>
#include <string>
#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "cor.h"

#include "MethodDescriptor.h"

namespace Profiler
{
	struct Configuration
	{
		UINT eventMask;

		std::string sharedMemoryName;
		std::optional<std::string> sharedMemoryFile;
		UINT sharedMemorySize;

		std::string commandQueueName;
		std::optional<std::string> commandQueueFile;
        UINT commandQueueSize;

		std::vector<MethodDescriptor> additionalData;
	};

	void to_json(nlohmann::json& json, const Configuration& descriptor);
	void from_json(const nlohmann::json& json, Configuration& descriptor);
}