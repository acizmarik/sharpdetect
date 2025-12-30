// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodInjectionDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const MethodInjectionDescriptor& descriptor)
{
	json["name"] = descriptor.name;
	json["eventType"] = descriptor.eventType;
	json["signature"] = descriptor.signature;
}

void Profiler::from_json(const nlohmann::json& json, MethodInjectionDescriptor& descriptor)
{
	descriptor.name = json.at("name");
	descriptor.eventType = json.at("eventType");
	descriptor.signature = json.at("signature");
}