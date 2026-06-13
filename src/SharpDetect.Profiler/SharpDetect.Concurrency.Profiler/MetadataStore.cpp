// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <utility>

#include "MetadataStore.h"

void Profiler::MetadataStore::Add(
	const ModuleID moduleId,
	std::shared_ptr<LibProfiler::ModuleDef> moduleDef,
	const AssemblyID assemblyId,
	std::shared_ptr<LibProfiler::AssemblyDef> assemblyDef)
{
	auto guard = std::unique_lock(_mutex);
	// FIXME: modules and assemblies are not always 1:1 mapped (assembly can contain multiple modules)
	_modules.emplace(moduleId, std::move(moduleDef));
	_assemblies.emplace(assemblyId, std::move(assemblyDef));
}

std::shared_ptr<LibProfiler::ModuleDef> Profiler::MetadataStore::GetModuleDef(const ModuleID moduleId)
{
	auto guard = std::unique_lock(_mutex);
	return _modules.find(moduleId)->second;
}

std::shared_ptr<LibProfiler::AssemblyDef> Profiler::MetadataStore::GetAssemblyDef(const AssemblyID assemblyId)
{
	auto guard = std::unique_lock(_mutex);
	return _assemblies.find(assemblyId)->second;
}

BOOL Profiler::MetadataStore::HasModuleDef(const ModuleID moduleId)
{
	auto guard = std::unique_lock(_mutex);
	return _modules.contains(moduleId);
}

BOOL Profiler::MetadataStore::HasAssemblyDef(const AssemblyID assemblyId)
{
	auto guard = std::unique_lock(_mutex);
	return _assemblies.contains(assemblyId);
}
