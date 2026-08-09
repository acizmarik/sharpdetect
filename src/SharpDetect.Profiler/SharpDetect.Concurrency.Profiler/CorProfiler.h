// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <deque>
#include <functional>
#include <mutex>
#include <unordered_map>
#include <optional>
#include <vector>

#include "cor.h"

#include "../LibIPC/Client.h"
#include "../LibIPC/Messages.h"
#include "../LibProfilerCore/CorProfilerBase.h"
#include "../LibMetadata/ModuleDef.h"
#include "../LibMetadata/TypeClassification.h"
#include "../LibProfilerCore/ObjectsTracker.h"
#include "../LibDescriptors/Configuration.h"
#include "../LibDescriptors/FieldAccessIntrinsicDescriptor.h"
#include "../LibDescriptors/MethodDescriptor.h"

#include "ArgumentCapture.h"
#include "MetadataStore.h"
#include "MethodDescriptorRegistry.h"
#include "RewriteRegistry.h"
#include "TypeInjector.h"

namespace Profiler
{
	enum class GenericCaptureState : UINT8 { Unresolved, Allow, Suppress };
	enum class EltCallbackKind : UINT8 { Enter, Leave };

	struct EltDecision
	{
		FunctionID functionId;
		ModuleID moduleId;
		mdMethodDef methodDef;
		const MethodDescriptor* descriptor;
		USHORT enterEventId;
		USHORT enterWithArgsEventId;
		USHORT exitEventId;
		USHORT exitWithArgsEventId;
		bool hasArguments;
		bool hasReturnValue;
		bool hasIndirects;
		bool pushesArgumentsFrame;
		bool emitExitEvent;
		bool captureStackTraceOnEnter;
		std::atomic<GenericCaptureState> genericCapture;
	};

	class CorProfiler final : public LibProfiler::CorProfilerBase, public LibIPC::ICommandHandler
	{
	public:
		explicit CorProfiler(const Configuration &configuration);
		HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
		HRESULT STDMETHODCALLTYPE Shutdown() override;
		
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
		HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter(FunctionID functionId) override;

		HRESULT EnterMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT LeaveMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT TailcallMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		[[nodiscard]] std::shared_ptr<MethodDescriptor> FindMethodDescriptor(FunctionID functionId);
		[[nodiscard]] EltDecision* GetEltDecision(FunctionID functionId, BOOL* pbHookFunction);

	private:
		HRESULT AbortAttach(const std::string& reason);
		[[nodiscard]] LibIPC::MetadataMsg CreateMetadataMsg() const;
		[[nodiscard]] LibIPC::MetadataMsg CreateMetadataMsg(UINT64 commandId) const;
		[[nodiscard]] UINT64 GetCurrentThreadIdCached() const;
		HRESULT CaptureStackTrace(UINT64 commandId, ThreadID threadId);
		void SendMethodEnter(UINT64 moduleId, UINT32 methodToken, USHORT interpretation);
		void SendMethodExit(UINT64 moduleId, UINT32 methodToken, USHORT interpretation);
		void SendMethodEnterWithArguments(
			UINT64 moduleId,
			UINT32 methodToken,
			USHORT interpretation,
			LibIPC::ByteSpanView argumentValues,
			LibIPC::ByteSpanView argumentInfos,
			std::optional<LibIPC::ByteSpanView> stackFrames);
		void SendMethodExitWithArguments(
			UINT64 moduleId,
			UINT32 methodToken,
			USHORT interpretation,
			LibIPC::ByteSpanView returnValue,
			LibIPC::ByteSpanView byRefArgumentValues,
			LibIPC::ByteSpanView byRefArgumentInfos);
		HRESULT PatchMethodBody(const LibProfiler::ModuleDef& moduleDef, mdTypeDef mdTypeDef, mdMethodDef mdMethodDef);
		[[nodiscard]] GenericCaptureState ClassifyGenericValueCapture(FunctionID functionId, COR_PRF_FRAME_INFO frameInfo, const MethodDescriptor& descriptor);
		[[nodiscard]] COR_PRF_FRAME_INFO GetFrameInfo(const EltDecision& decision, COR_PRF_ELT_INFO eltInfo, EltCallbackKind callback) const;
		[[nodiscard]] bool ShouldSuppressGenericCapture(EltDecision& decision, COR_PRF_ELT_INFO eltInfo, EltCallbackKind callback);

		HRESULT InitializeProfilingFeatures() const;

		std::atomic_bool _terminating;
		Configuration _configuration;
		LibIPC::Client _client;
		ModuleID _coreModule;
		UINT32 _pid;
		std::atomic<UINT64> _threadIdCacheEpoch;

		MetadataStore _metadataStore;
		LibProfiler::ObjectsTracker _objectsTracker;
		MethodDescriptorRegistry _methodDescriptorRegistry;
		std::vector<FieldAccessIntrinsicDescriptor> _fieldAccessIntrinsics;
		RewriteRegistry _rewriteRegistry;
		ArgumentCapture _argumentCapture;
		TypeInjector _typeInjector;

		std::deque<EltDecision> _eltDecisions;
		std::unordered_map<FunctionID, EltDecision*> _eltDecisionLookup;
		std::mutex _eltDecisionMutex;
	};
}
