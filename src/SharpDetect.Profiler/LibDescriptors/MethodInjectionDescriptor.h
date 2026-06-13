// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "../LibIPC/Messages.h"
#include "MethodSignatureDescriptor.h"

namespace Profiler
{
	struct MethodInjectionDescriptor
	{
		std::string name;
		LibIPC::RecordedEventType eventType;
		MethodSignatureDescriptor signature;
	};

	void to_json(nlohmann::json& json, const MethodInjectionDescriptor& descriptor);
	void from_json(const nlohmann::json& json, MethodInjectionDescriptor& descriptor);
}