// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"
#include "corprof.h"

#include "../LibIPC/Client.h"
#include "../LibIPC/Messages.h"
#include "../LibMetadata/AssemblyDef.h"
#include "../LibMetadata/AssemblyRef.h"
#include "../LibMetadata/ModuleDef.h"
#include "../LibDescriptors/Configuration.h"
#include "../LibDescriptors/MethodDescriptor.h"

#include "MetadataStore.h"
#include "MethodDescriptorRegistry.h"
#include "RewriteRegistry.h"

namespace Profiler
{
	class TypeInjector
	{
	public:
		TypeInjector(
			ICorProfilerInfo10*& corProfilerInfo,
			LibIPC::Client& client,
			const Configuration& configuration,
			const ModuleID& coreModule,
			MetadataStore& metadataStore,
			MethodDescriptorRegistry& methodDescriptorRegistry,
			RewriteRegistry& rewriteRegistry);

		HRESULT InjectTypesForProfilingFeatures(LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportInjectedTypes(const LibProfiler::AssemblyDef& assemblyDef, const LibProfiler::ModuleDef& moduleDef);
		HRESULT WrapAnalyzedExternMethods(LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportMethodWrappers(const LibProfiler::AssemblyDef& assemblyDef, const LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportCustomRecordedEventTypes(const LibProfiler::ModuleDef& moduleDef);

	private:
		HRESULT ImportMethodWrapper(const LibProfiler::ModuleDef& moduleDef, const LibProfiler::AssemblyRef& assemblyRef, const MethodDescriptor& methodDescriptor);
		[[nodiscard]] LibIPC::MetadataMsg CreateMetadataMsg() const;

		ICorProfilerInfo10*& _corProfilerInfo;
		LibIPC::Client& _client;
		const Configuration& _configuration;
		const ModuleID& _coreModule;
		MetadataStore& _metadataStore;
		MethodDescriptorRegistry& _methodDescriptorRegistry;
		RewriteRegistry& _rewriteRegistry;
	};
}
