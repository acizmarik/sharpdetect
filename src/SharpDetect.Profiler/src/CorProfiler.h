/*
 * Copyright (C) 2020, Andrej Čižmárik
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

 // Based on project microsoftarchive/clrprofiler/ILRewrite
 // Original source: https://github.com/microsoftarchive/clrprofiler/tree/master/ILRewrite
 // Copyright (c) .NET Foundation and contributors. All rights reserved.
 // Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef CORPROFILER_HEADER_GUARD
#define CORPROFILER_HEADER_GUARD

#include "Stdafx.h"
#include "profiler_pal.h"
#include "CorProfilerBase.h"
#include "wstring.h"
#include "Client.h"
#include "FunctionInfo.h"
#include "ModuleMetadata.h"
#include "InstrumentationContext.h"
#include "Logging.h"
#include "profiler_notifications.pb.h"
#include "profiler_requests.pb.h"
#include <atomic>
#include <unordered_map>
#include <mutex>
#include <memory>
#include <vector>
#include <stack>
#include <utility>
#include <tuple>

using namespace SharpDetect::Common::Messages;

class CorProfiler : public CorProfilerBase
{
public:
	CorProfiler();
	virtual ~CorProfiler();

	void EnterMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo) override;
	void LeaveMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo) override;
	void TailcallMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo) override;

	HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
	HRESULT STDMETHODCALLTYPE Shutdown() override;
	HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override;
	HRESULT STDMETHODCALLTYPE ClassLoadFinished(ClassID classId, HRESULT hrStatus) override;
	HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock) override;
	HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override;
	HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
	HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;
	HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
	HRESULT STDMETHODCALLTYPE RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason) override;
	HRESULT STDMETHODCALLTYPE RuntimeSuspendFinished() override;
	HRESULT STDMETHODCALLTYPE RuntimeThreadSuspended(ThreadID threadId) override;
	HRESULT STDMETHODCALLTYPE RuntimeThreadResumed(ThreadID threadId) override;
	HRESULT STDMETHODCALLTYPE MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
	HRESULT STDMETHODCALLTYPE SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;

	ModuleMetadata& GetModuleMetadata(ModuleID module);
	std::tuple<std::unique_ptr<COR_PRF_GC_GENERATION_RANGE[]>, ULONG> GetGenerationBounds();
	
	void GetArguments(const FunctionInfo& functionInfo, std::vector<BYTE*>& indirectAddrs, COR_PRF_FUNCTION_ARGUMENT_INFO* pArgInfos, BYTE* pArgValues, BYTE* pArgOffsets);
	void GetByRefArguments(const FunctionInfo& functionInfo, const std::vector<BYTE*>& indirectAddrs, BYTE* pArgValues, BYTE* pArgOffsets);

private:
	std::mutex metadataMutex;
	std::unordered_map<ModuleID, std::unique_ptr<ModuleMetadata>> metadata;
	std::unordered_map<FunctionID, mdMethodDef> injectedMethodWrappers;
	std::unique_ptr<InstrumentationContext> pInstrumentationContext;
};

#endif
