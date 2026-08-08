// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <tuple>

#include "MethodVersionDescriptor.h"

BOOL Profiler::IsApplicableToRuntimeVersion(
	const std::optional<MethodVersionDescriptor>& versionDescriptor,
	const INT32 versionMajor,
	const INT32 versionMinor,
	const INT32 versionBuild)
{
	if (!versionDescriptor.has_value())
		return TRUE;

	const auto& [
		fromMajorVersion,
		fromMinorVersion,
		fromBuildVersion,
		toMajorVersion,
		toMinorVersion,
		toBuildVersion] = versionDescriptor.value();

	const auto fromVersion = std::make_tuple(fromMajorVersion, fromMinorVersion, fromBuildVersion);
	const auto toVersion = std::make_tuple(toMajorVersion, toMinorVersion, toBuildVersion);
	const auto currentVersion = std::make_tuple(versionMajor, versionMinor, versionBuild);

	return currentVersion >= fromVersion && currentVersion <= toVersion;
}

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