// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"
#include "../lib/json/single_include/nlohmann/json.hpp"

namespace Profiler
{
	struct MethodVersionDescriptor
	{
		INT32 fromMajorVersion;
		INT32 fromMinorVersion;
		INT32 fromBuildVersion;
		INT32 toMajorVersion;
		INT32 toMinorVersion;
		INT32 toBuildVersion;
	};

	void to_json(nlohmann::json& json, const MethodVersionDescriptor& descriptor);
	void from_json(const nlohmann::json& json, MethodVersionDescriptor& descriptor);
}
