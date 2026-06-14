// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <memory>
#include <shared_mutex>
#include <unordered_map>
#include <vector>

#include "cor.h"
#include "corprof.h"

#include "../LibDescriptors/HashingUtils.h"
#include "../LibDescriptors/MethodDescriptor.h"

namespace Profiler
{
	class MethodDescriptorRegistry
	{
	public:
		void Import(
			const std::vector<MethodDescriptor>& configured,
			INT32 versionMajor,
			INT32 versionMinor,
			INT32 versionBuild);

		[[nodiscard]] const std::vector<std::shared_ptr<MethodDescriptor>>& Descriptors() const noexcept { return _descriptors; }

		void AddLookup(ModuleID moduleId, mdToken methodDef, std::shared_ptr<MethodDescriptor> descriptor);

		[[nodiscard]] BOOL Has(ModuleID moduleId, mdMethodDef methodDef);
		[[nodiscard]] std::shared_ptr<MethodDescriptor> TryGet(ModuleID moduleId, mdMethodDef methodDef);
		[[nodiscard]] std::shared_ptr<MethodDescriptor> Get(ModuleID moduleId, mdMethodDef methodDef);

	private:
		using Key = std::pair<ModuleID, mdMethodDef>;
		using KeyHasher = pair_hash<ModuleID, mdMethodDef>;

		std::vector<std::shared_ptr<MethodDescriptor>> _descriptors;
		std::unordered_map<Key, std::shared_ptr<MethodDescriptor>, KeyHasher> _lookup;
		std::shared_mutex _mutex;
	};
}
