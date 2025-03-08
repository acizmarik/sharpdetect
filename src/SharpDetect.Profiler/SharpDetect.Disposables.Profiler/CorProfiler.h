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
#include "../LibProfiler/WString.h"
#include "Configuration.h"

namespace Profiler
{
	class CorProfiler : public LibProfiler::CorProfilerBase
	{
	public:
		CorProfiler(Configuration configuration);
		virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
		virtual HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override;
		virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
		virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;
		virtual HRESULT STDMETHODCALLTYPE MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
		virtual HRESULT STDMETHODCALLTYPE SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;

		ICorProfilerInfo8& GetCorProfilerInfo();
		BOOL HasModuleDef(ModuleID moduleId);
		std::shared_ptr<LibProfiler::ModuleDef> GetModuleDef(ModuleID moduleId);
		HRESULT EnterMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT LeaveMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
		HRESULT TailcallMethod(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);

	private:
		std::atomic_bool _terminating;
		BOOL _collectFullStackTraces;
		Configuration _configuration;
		LibIPC::Client _client;
		LibProfiler::ObjectsTracker _objectsTracker;

		std::unordered_map<ModuleID, std::shared_ptr<LibProfiler::ModuleDef>> _modules;
		std::mutex _modulesMutex;

		LibIPC::MetadataMsg CreateMetadataMsg();
	};
}