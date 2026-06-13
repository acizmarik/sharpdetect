// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "TypeInjectionDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const TypeInjectionDescriptor& descriptor)
{
	json["typeFullName"] = descriptor.typeFullName;
	json["methods"] = descriptor.methods;
}

void Profiler::from_json(const nlohmann::json& json, TypeInjectionDescriptor& descriptor)
{
	descriptor.typeFullName = json.at("typeFullName");
	descriptor.methods = json.at("methods").get<std::vector<MethodInjectionDescriptor>>();
}