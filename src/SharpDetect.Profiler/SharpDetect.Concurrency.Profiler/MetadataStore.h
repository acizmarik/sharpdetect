// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <memory>
#include <mutex>
#include <unordered_map>

#include "cor.h"
#include "corprof.h"

#include "../LibMetadata/AssemblyDef.h"
#include "../LibMetadata/ModuleDef.h"

namespace Profiler
{
	// Thread-safe cache of the CLR modules and assemblies the profiler has seen.
	class MetadataStore
	{
	public:
		void Add(
			ModuleID moduleId,
			std::shared_ptr<LibProfiler::ModuleDef> moduleDef,
			AssemblyID assemblyId,
			std::shared_ptr<LibProfiler::AssemblyDef> assemblyDef);

		[[nodiscard]] BOOL HasModuleDef(ModuleID moduleId);
		[[nodiscard]] BOOL HasAssemblyDef(AssemblyID assemblyId);
		[[nodiscard]] std::shared_ptr<LibProfiler::ModuleDef> GetModuleDef(ModuleID moduleId);
		[[nodiscard]] std::shared_ptr<LibProfiler::AssemblyDef> GetAssemblyDef(AssemblyID assemblyId);

	private:
		std::unordered_map<AssemblyID, std::shared_ptr<LibProfiler::AssemblyDef>> _assemblies;
		std::unordered_map<ModuleID, std::shared_ptr<LibProfiler::ModuleDef>> _modules;
		std::mutex _mutex;
	};
}
