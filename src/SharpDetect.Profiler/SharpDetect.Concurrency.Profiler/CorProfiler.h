// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <unordered_map>
#include <stack>
#include <utility>
#include <vector>
#include <mutex>
#include <shared_mutex>

#include "cor.h"

#include "../LibIPC/Client.h"
#include "../LibIPC/Messages.h"
#include "../LibMetadata/AssemblyDef.h"
#include "../LibProfilerCore/CorProfilerBase.h"
#include "../LibMetadata/ModuleDef.h"
#include "../LibProfilerCore/ObjectsTracker.h"
#include "../LibDescriptors/Configuration.h"
#include "../LibDescriptors/HashingUtils.h"
#include "../LibDescriptors/MethodDescriptor.h"
#include "../LibDescriptors/TypeInjectionDescriptor.h"

#include "ArgumentCapture.h"
#include "MetadataStore.h"
#include "MethodDescriptorRegistry.h"
#include "RewriteRegistry.h"

namespace Profiler
{
	class CorProfiler final : public LibProfiler::CorProfilerBase, public LibIPC::ICommandHandler
	{
	public:
		explicit CorProfiler(const Configuration &configuration);
		HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
		
		void OnCreateStackSnapshot(UINT64 commandId, UINT64 targetThreadId) override;
		void OnCreateStackSnapshots(UINT64 commandId, const std::vector<UINT64>& targetThreadIds) override;

		HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
		HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;
		HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock) override;
		HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override;
		HRESULT STDMETHODCALLTYPE MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
		HRESULT STDMETHODCALLTYPE SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
		HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override;
		HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
		HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
		
		HRESULT EnterMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT LeaveMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT TailcallMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		[[nodiscard]] std::shared_ptr<MethodDescriptor> FindMethodDescriptor(FunctionID functionId);

	private:
		HRESULT AbortAttach(const std::string& reason);
		[[nodiscard]] LibIPC::MetadataMsg CreateMetadataMsg() const;
		[[nodiscard]] LibIPC::MetadataMsg CreateMetadataMsg(UINT64 commandId) const;
		HRESULT CaptureStackTrace(UINT64 commandId, ThreadID threadId);
		HRESULT PatchMethodBody(const LibProfiler::ModuleDef& moduleDef, mdTypeDef mdTypeDef, mdMethodDef mdMethodDef);

		HRESULT WrapAnalyzedExternMethods(LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportMethodWrappers(const LibProfiler::AssemblyDef& assemblyDef, const LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportMethodWrapper(const LibProfiler::ModuleDef& moduleDef, const LibProfiler::AssemblyRef& assemblyRef, const MethodDescriptor& methodDescriptor);
		HRESULT ImportCustomRecordedEventTypes(const LibProfiler::ModuleDef& moduleDef);

		HRESULT InjectTypesForProfilingFeatures(LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportInjectedTypes(const LibProfiler::AssemblyDef& assemblyDef, const LibProfiler::ModuleDef& moduleDef);

		HRESULT InitializeProfilingFeatures() const;

		std::atomic_bool _terminating;
		Configuration _configuration;
		LibIPC::Client _client;
		ModuleID _coreModule;

		MetadataStore _metadataStore;
		LibProfiler::ObjectsTracker _objectsTracker;
		MethodDescriptorRegistry _methodDescriptorRegistry;
		RewriteRegistry _rewriteRegistry;
		ArgumentCapture _argumentCapture;
	};
}
