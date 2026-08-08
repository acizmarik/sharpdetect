// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>
#include <string>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "MethodSignatureDescriptor.h"
#include "MethodVersionDescriptor.h"

namespace Profiler
{
	enum class FieldAccessIntrinsicInterpretation : UINT8
	{
		AtomicReadModifyWrite = 1,
		VolatileRead = 2,
		VolatileWrite = 3
	};

	struct FieldAccessIntrinsicDescriptor
	{
		std::string methodName;
		std::string declaringTypeFullName;
		std::optional<MethodVersionDescriptor> versionDescriptor;
		MethodSignatureDescriptor signatureDescriptor;
		FieldAccessIntrinsicInterpretation interpretation;
	};

	void to_json(nlohmann::json& json, const FieldAccessIntrinsicDescriptor& descriptor);
	void from_json(const nlohmann::json& json, FieldAccessIntrinsicDescriptor& descriptor);
}
