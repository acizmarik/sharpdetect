// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodVersionDescriptor.h"

void Profiler::to_json(nlohmann::json &json, const MethodVersionDescriptor &descriptor)
{
	json["fromMajorVersion"] = descriptor.fromMajorVersion;
	json["fromMinorVersion"] = descriptor.fromMinorVersion;
	json["fromBuildVersion"] = descriptor.fromBuildVersion;
	json["toMajorVersion"] = descriptor.toMajorVersion;
	json["toMinorVersion"] = descriptor.toMinorVersion;
	json["toBuildVersion"] = descriptor.toBuildVersion;
}

void Profiler::from_json(const nlohmann::json &json, MethodVersionDescriptor &descriptor)
{
	descriptor.fromMajorVersion = json.at("fromMajorVersion");
	descriptor.fromMinorVersion = json.at("fromMinorVersion");
	descriptor.fromBuildVersion = json.at("fromBuildVersion");
	descriptor.toMajorVersion = json.at("toMajorVersion");
	descriptor.toMinorVersion = json.at("toMinorVersion");
	descriptor.toBuildVersion = json.at("toBuildVersion");
}