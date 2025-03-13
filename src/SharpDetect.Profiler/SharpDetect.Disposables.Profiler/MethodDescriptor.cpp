// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const MethodDescriptor& descriptor)
{
	json["name"] = descriptor.name;
	json["declaringTypeFullName"] = descriptor.declaringTypeFullName;
}

void Profiler::from_json(const nlohmann::json& json, MethodDescriptor& descriptor)
{
	descriptor.name = json.at("name");
	descriptor.declaringTypeFullName = json.at("declaringTypeFullName");
}
