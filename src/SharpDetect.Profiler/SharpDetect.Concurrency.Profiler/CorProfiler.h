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

#include "Configuration.h"
#include "HashingUtils.h"
#include "MethodDescriptor.h"

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
		[[nodiscard]] LibIPC::MetadataMsg CreateMetadataMsg() const;
		[[nodiscard]] LibIPC::MetadataMsg CreateMetadataMsg(UINT64 commandId) const;
		HRESULT CaptureStackTrace(UINT64 commandId, ThreadID threadId);
		[[nodiscard]] BOOL HasModuleDef(ModuleID moduleId);
		[[nodiscard]] BOOL HasAssemblyDef(AssemblyID assemblyId);
		[[nodiscard]] BOOL HasMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef);
		[[nodiscard]] std::shared_ptr<LibProfiler::ModuleDef> GetModuleDef(ModuleID moduleId);
		[[nodiscard]] std::shared_ptr<LibProfiler::AssemblyDef> GetAssemblyDef(AssemblyID assemblyID);
		[[nodiscard]] std::shared_ptr<MethodDescriptor> GetMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef);
		HRESULT PatchMethodBody(const LibProfiler::ModuleDef& moduleDef, mdTypeDef mdTypeDef, mdMethodDef mdMethodDef);

		HRESULT WrapAnalyzedExternMethods(LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportMethodWrappers(const LibProfiler::AssemblyDef& assemblyDef, const LibProfiler::ModuleDef& moduleDef);
		HRESULT ImportMethodWrapper(const LibProfiler::ModuleDef& moduleDef, const LibProfiler::AssemblyRef& assemblyRef, const MethodDescriptor& methodDescriptor);
		HRESULT ImportCustomRecordedEventTypes(const LibProfiler::ModuleDef& moduleDef);

		HRESULT InitializeProfilingFeatures() const;
		HRESULT ImportMethodDescriptors(INT32 versionMajor, INT32 versionMinor, INT32 versionBuild);

		HRESULT GetArguments(
			const MethodDescriptor& methodDescriptor,
			std::vector<UINT_PTR>& indirects,
			const COR_PRF_FUNCTION_ARGUMENT_INFO& argumentInfos,
			std::span<BYTE> argumentValues,
			std::span<BYTE> argumentOffsets);

		static HRESULT GetByRefArguments(
			const MethodDescriptor& methodDescriptor,
			const std::vector<UINT_PTR>& indirects,
			std::span<BYTE> indirectValues,
			std::span<BYTE> indirectOffsets);

		HRESULT GetArgument(
			const CapturedArgumentDescriptor& argument,
			COR_PRF_FUNCTION_ARGUMENT_RANGE range,
			std::vector<UINT_PTR>& indirects,
			std::span<BYTE>& argValue,
			std::span<BYTE>& argOffset);

		HRESULT GetReturnValue(
			const CapturedValueDescriptor& value,
			COR_PRF_FUNCTION_ARGUMENT_RANGE range,
			const std::span<BYTE>& returnValue);

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
		using MethodIdHasher = pair_hash<ModuleID, mdMethodDef>;
		std::unordered_map<MethodId, BOOL, MethodIdHasher> _wrappers;
		std::mutex _wrappersMutex;

		LibProfiler::ObjectsTracker _objectsTracker;
		std::vector< std::shared_ptr<MethodDescriptor>> _methodDescriptors;
		std::unordered_map<MethodId, std::shared_ptr<MethodDescriptor>, MethodIdHasher> _methodDescriptorsLookup;
		std::mutex _methodDescriptorsMutex;

		using MethodInvocationId = std::tuple<ModuleID, mdMethodDef, USHORT>;
		using MethodInvocationIdHasher = tuple_hash<ModuleID, mdMethodDef, USHORT>;
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