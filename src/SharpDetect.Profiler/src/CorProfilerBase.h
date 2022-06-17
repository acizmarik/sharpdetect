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


#ifndef PROFILERBASE_HEADER_GUARD
#define PROFILERBASE_HEADER_GUARD

#include "Stdafx.h"
#include "Logging.h"
#include "Client.h"
#include "FunctionInfo.h"
#include "ClassFactory.h"
#include <atomic>
#include <mutex>
#include <vector>
#include <unordered_map>
#include <memory>
#include <cstdlib>
#include <string>

class CorProfilerBase : public ICorProfilerCallback8
{
public:
	CorProfilerBase();
	virtual ~CorProfilerBase();

	virtual void EnterMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo) = 0;
	virtual void LeaveMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo) = 0;
	virtual void TailcallMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo) = 0;
	const FunctionInfo* TryGetHookData(FunctionID function);

	HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
	HRESULT STDMETHODCALLTYPE Shutdown() override;
	
	HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ClassLoadFinished(ClassID classId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader) override { return S_OK; };
	HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RuntimeSuspendFinished() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RuntimeSuspendAborted() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RuntimeResumeStarted() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RuntimeResumeFinished() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RuntimeThreadSuspended(ThreadID threadId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RuntimeThreadResumed(ThreadID threadId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override { return S_OK; }

	HRESULT STDMETHODCALLTYPE AppDomainCreationStarted(AppDomainID appDomainId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE AppDomainShutdownStarted(AppDomainID appDomainId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE AssemblyLoadStarted(AssemblyID assemblyId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE AssemblyUnloadStarted(AssemblyID assemblyId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ModuleLoadStarted(ModuleID moduleId) override { return S_OK; }	
	HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(ModuleID moduleId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ClassLoadStarted(ClassID classId) override { return S_OK; }	
	HRESULT STDMETHODCALLTYPE ClassUnloadStarted(ClassID classId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ClassUnloadFinished(ClassID classId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE FunctionUnloadStarted(FunctionID functionId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchStarted(FunctionID functionId, BOOL* pbUseCachedFunction) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE JITFunctionPitched(FunctionID functionId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingClientInvocationStarted() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingClientSendingMessage(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingClientReceivingReply(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingClientInvocationFinished() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingServerReceivingMessage(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingServerInvocationStarted() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingServerInvocationReturned() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RemotingServerSendingReply(GUID* pCookie, BOOL fIsAsync) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ObjectAllocated(ObjectID objectId, ClassID classId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RootReferences(ULONG cRootRefs, ObjectID rootRefIds[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionThrown(ObjectID thrownObjectId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionEnter(FunctionID functionId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionLeave() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionSearchFilterEnter(FunctionID functionId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionSearchFilterLeave() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionSearchCatcherFound(FunctionID functionId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionOSHandlerEnter(UINT_PTR __unused) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionOSHandlerLeave(UINT_PTR __unused) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter(FunctionID functionId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionLeave() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyEnter(FunctionID functionId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyLeave() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionCatcherLeave() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable, ULONG cSlots) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherFound() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherExecute() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[]) override { return S_OK; }	
	HRESULT STDMETHODCALLTYPE FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE HandleCreated(GCHandleID handleId, ObjectID initialObjectId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE HandleDestroyed(GCHandleID handleId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ProfilerAttachComplete() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded() override { return S_OK; }
	HRESULT STDMETHODCALLTYPE GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[]) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE GetAssemblyReferences(const WCHAR* wszAssemblyPath, ICorProfilerAssemblyReferenceProvider* pAsmRefProvider) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE ModuleInMemorySymbolsUpdated(ModuleID moduleId) override { return S_OK; }
	HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override { return S_OK; }

	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override
	{
		if (riid == __uuidof(ICorProfilerCallback8) ||
			riid == __uuidof(ICorProfilerCallback7) ||
			riid == __uuidof(ICorProfilerCallback6) ||
			riid == __uuidof(ICorProfilerCallback5) ||
			riid == __uuidof(ICorProfilerCallback4) ||
			riid == __uuidof(ICorProfilerCallback3) ||
			riid == __uuidof(ICorProfilerCallback2) ||
			riid == __uuidof(ICorProfilerCallback) ||
			riid == IID_IUnknown)
		{
			*ppvObject = this;
			this->AddRef();
			return S_OK;
		}

		*ppvObject = nullptr;
		return E_NOINTERFACE;
	}

	ULONG STDMETHODCALLTYPE AddRef(void) override
	{
		return std::atomic_fetch_add(&this->refCount, 1) + 1;
	}

	ULONG STDMETHODCALLTYPE Release(void) override
	{
		int count = std::atomic_fetch_sub(&this->refCount, 1) - 1;

		if (count <= 0)
		{
			delete this;
		}

		return count;
	}

	// Global is necessary for method enter/leave hooks	
	static CorProfilerBase* GetInstance() { return instance; }

protected:
	void PrepareLogging();
	void RegisterHook(FunctionID function, ModuleID module, mdTypeDef classToken, mdMethodDef functionToken,
		bool captureArguments, bool captureReturnValue, size_t totalArgumentsSize, size_t totalIndirectArgumentSize,
		std::vector<std::tuple<UINT16, UINT16, bool>>&& argumentInfos);

	std::unordered_map<FunctionID, std::unique_ptr<FunctionInfo>> injectedHooks;
	CComPtr<ICorProfilerInfo8> pCorProfilerInfo;
	std::unique_ptr<Client> pMessagingClient;
	el::Logger* pLogger;

private:
	static CorProfilerBase* instance;
	std::mutex hooksDataMutex;
	std::atomic<int> refCount;
};

#endif
