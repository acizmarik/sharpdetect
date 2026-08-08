// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <mutex>
#include <tuple>
#include <utility>

#include "MethodDescriptorRegistry.h"

void Profiler::MethodDescriptorRegistry::Import(
	const std::vector<MethodDescriptor>& configured,
	const INT32 versionMajor,
	const INT32 versionMinor,
	const INT32 versionBuild)
{
	for (auto&& item : configured)
	{
		if (IsApplicableToRuntimeVersion(item.versionDescriptor, versionMajor, versionMinor, versionBuild))
			_descriptors.emplace_back(std::make_shared<MethodDescriptor>(item));
	}
}

void Profiler::MethodDescriptorRegistry::AddLookup(
	const ModuleID moduleId,
	const mdToken methodDef,
	std::shared_ptr<MethodDescriptor> descriptor)
{
	auto guard = std::unique_lock(_mutex);
	_lookup.emplace(std::make_pair(moduleId, methodDef), std::move(descriptor));
}

BOOL Profiler::MethodDescriptorRegistry::Has(const ModuleID moduleId, const mdMethodDef methodDef)
{
	auto guard = std::shared_lock(_mutex);
	return _lookup.contains(std::make_pair(moduleId, methodDef));
}

std::shared_ptr<Profiler::MethodDescriptor> Profiler::MethodDescriptorRegistry::TryGet(const ModuleID moduleId, const mdMethodDef methodDef)
{
	auto guard = std::shared_lock(_mutex);
	const auto it = _lookup.find(std::make_pair(moduleId, methodDef));
	return (it != _lookup.cend()) ? it->second : nullptr;
}

std::shared_ptr<Profiler::MethodDescriptor> Profiler::MethodDescriptorRegistry::Get(const ModuleID moduleId, const mdMethodDef methodDef)
{
	auto guard = std::shared_lock(_mutex);
	return _lookup.find(std::make_pair(moduleId, methodDef))->second;
}
