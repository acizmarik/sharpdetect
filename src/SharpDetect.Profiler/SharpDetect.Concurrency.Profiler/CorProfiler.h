// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <unordered_map>
#include <stack>
#include <utility>
#include <vector>
#include <mutex>

#include "cor.h"

#include "../LibIPC/Client.h"
#include "../LibIPC/Messages.h"
#include "../LibProfiler/AssemblyDef.h"
#include "../LibProfiler/CorProfilerBase.h"
#include "../LibProfiler/ModuleDef.h"
#include "../LibProfiler/ObjectsTracker.h"
#include "../LibProfiler/StackWalker.h"
#include "../LibProfiler/WString.h"

#include "Configuration.h"
#include "HashingUtils.h"
#include "MethodDescriptor.h"

namespace Profiler
{
	class CorProfiler : public LibProfiler::CorProfilerBase, public LibIPC::ICommandHandler
	{
	public:
		CorProfiler(Configuration configuration);
		virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
		
		virtual void OnCreateStackSnapshot(UINT64 commandId, UINT64 targetThreadId) override;
		virtual void OnCreateStackSnapshots(UINT64 commandId, const std::vector<UINT64>& targetThreadIds) override;

		virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
		virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;
		virtual HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock) override;
		virtual HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override;
		virtual HRESULT STDMETHODCALLTYPE MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
		virtual HRESULT STDMETHODCALLTYPE SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
		virtual HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override;
		virtual HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
		virtual HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
		
		HRESULT EnterMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT LeaveMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT TailcallMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		std::shared_ptr<MethodDescriptor> FindMethodDescriptor(FunctionID functionId);

	private:
		LibIPC::MetadataMsg CreateMetadataMsg();
		LibIPC::MetadataMsg CreateMetadataMsg(UINT64 commandId);
		HRESULT CaptureStackTrace(UINT64 commandId, ThreadID threadId);
		BOOL HasModuleDef(ModuleID moduleId);
		BOOL HasAssemblyDef(AssemblyID assemblyId);
		BOOL HasMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef);
		std::shared_ptr<LibProfiler::ModuleDef> GetModuleDef(ModuleID moduleId);
		std::shared_ptr<LibProfiler::AssemblyDef> GetAssemblyDef(AssemblyID assemblyID);
		std::shared_ptr<MethodDescriptor> GetMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef);
		HRESULT PatchMethodBody(LibProfiler::ModuleDef& moduleDef, mdTypeDef mdTypeDef, mdMethodDef mdMethodDef);

		HRESULT WrapAnalyzedExternMethods(LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportMethodWrappers(LibProfiler::AssemblyDef& assemblyDef, LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportMethodWrapper(LibProfiler::ModuleDef& moduleDef, const LibProfiler::AssemblyRef& assemblyRef, const MethodDescriptor& methodDescriptor);
		HRESULT ImportCustomRecordedEventTypes(LibProfiler::ModuleDef& moduleDef);

		HRESULT InitializeProfilingFeatures();

		HRESULT GetArguments(
			const MethodDescriptor& methodDescriptor,
			std::vector<UINT_PTR>& indirects,
			const COR_PRF_FUNCTION_ARGUMENT_INFO& argumentInfos,
			tcb::span<BYTE> argumentValues,
			tcb::span<BYTE> argumentOffsets);

		HRESULT GetByRefArguments(
			const MethodDescriptor& methodDescriptor,
			const std::vector<UINT_PTR>& indirects,
			tcb::span<BYTE> indirectValues,
			tcb::span<BYTE> indirectOffsets);

		HRESULT GetArgument(
			const CapturedArgumentDescriptor& argument,
			COR_PRF_FUNCTION_ARGUMENT_RANGE range,
			std::vector<UINT_PTR>& indirects,
			tcb::span<BYTE>& argValue,
			tcb::span<BYTE>& argOffset);

		std::atomic_bool _terminating;
		Configuration _configuration;
		LibIPC::Client _client;
		ModuleID _coreModule;

		std::unordered_map<AssemblyID, std::shared_ptr<LibProfiler::AssemblyDef>> _assemblies;
		std::unordered_map<ModuleID, std::shared_ptr<LibProfiler::ModuleDef>> _modules;
		std::mutex _assembliesAndModulesMutex;
		
		std::unordered_map<ModuleID, std::unordered_map<mdToken, mdToken>> _rewritings;
		std::mutex _rewritingsMutex;

		using MethodId = std::pair<ModuleID, mdMethodDef>;
		using MethodIdHasher = Profiler::pair_hash<ModuleID, mdMethodDef>;
		std::unordered_map<MethodId, BOOL, MethodIdHasher> _wrappers;
		std::mutex _wrappersMutex;

		LibProfiler::ObjectsTracker _objectsTracker;
		std::vector< std::shared_ptr<MethodDescriptor>> _methodDescriptors;
		std::unordered_map<MethodId, std::shared_ptr<MethodDescriptor>, MethodIdHasher> _methodDescriptorsLookup;
		std::mutex _methodDescriptorsMutex;

		using MethodInvocationId = std::tuple<ModuleID, mdMethodDef, USHORT>;
		using MethodInvocationIdHasher = Profiler::tuple_hash<ModuleID, mdMethodDef, USHORT>;
		std::unordered_map<MethodInvocationId, USHORT, MethodInvocationIdHasher> _customEventOnMethodEntryLookup;
		std::unordered_map<MethodInvocationId, USHORT, MethodInvocationIdHasher> _customEventOnMethodExitLookup;
		std::mutex _customEventLookupsMutex;

		using CustomEventsLookup = std::unordered_map<MethodInvocationId, USHORT, MethodInvocationIdHasher>;
		void AddCustomEventMapping(
			CustomEventsLookup& lookup,
			ModuleID moduleId,
			mdMethodDef methodDef,
			USHORT original,
			USHORT mapping);
		BOOL FindCustomEventMapping(
			const CustomEventsLookup& lookup,
			ModuleID moduleId,
			mdMethodDef methodDef,
			USHORT original,
			USHORT& mapping);
	};
}