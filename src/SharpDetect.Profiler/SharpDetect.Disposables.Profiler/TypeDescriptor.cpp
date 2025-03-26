// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "TypeDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const TypeDescriptor& descriptor)
{
	json["fullName"] = descriptor.fullName;
}

void Profiler::from_json(const nlohmann::json& json, TypeDescriptor& descriptor)
{
	descriptor.fullName = json.at("fullName");
}
