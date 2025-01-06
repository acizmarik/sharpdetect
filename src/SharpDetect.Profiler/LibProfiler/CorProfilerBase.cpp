// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstdlib>
#include <string>

#include "../lib/loguru/loguru.hpp"

#include "CorProfilerBase.h"

LibProfiler::CorProfilerBase::CorProfilerBase() : _refCount(0), _corProfilerInfo(nullptr)
{

}

LibProfiler::CorProfilerBase::~CorProfilerBase()
{
    LOG_F(INFO, "Terminating");
    if (this->_corProfilerInfo != nullptr)
    {
        this->_corProfilerInfo->Release();
        this->_corProfilerInfo = nullptr;
    }
}


HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    return pICorProfilerInfoUnk->QueryInterface(__uuidof(ICorProfilerInfo8), (void**)&this->_corProfilerInfo);
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::Shutdown()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AppDomainCreationStarted(AppDomainID appDomainId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AppDomainShutdownStarted(AppDomainID appDomainId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AssemblyLoadStarted(AssemblyID assemblyId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AssemblyUnloadStarted(AssemblyID assemblyId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ModuleLoadStarted(ModuleID moduleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ModuleUnloadStarted(ModuleID moduleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ClassLoadStarted(ClassID classId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ClassUnloadStarted(ClassID classId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ClassUnloadFinished(ClassID classId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::FunctionUnloadStarted(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::JITCachedFunctionSearchStarted(FunctionID functionId, BOOL* pbUseCachedFunction)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::JITFunctionPitched(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ThreadCreated(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ThreadDestroyed(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingClientInvocationStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingClientSendingMessage(GUID* pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingClientReceivingReply(GUID* pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingClientInvocationFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingServerReceivingMessage(GUID* pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingServerInvocationStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingServerInvocationReturned()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RemotingServerSendingReply(GUID* pCookie, BOOL fIsAsync)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RuntimeSuspendFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RuntimeSuspendAborted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RuntimeResumeStarted()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RuntimeResumeFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RuntimeThreadSuspended(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RuntimeThreadResumed(ThreadID threadId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ObjectAllocated(ObjectID objectId, ClassID classId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RootReferences(ULONG cRootRefs, ObjectID rootRefIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionThrown(ObjectID thrownObjectId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionSearchFunctionEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionSearchFunctionLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionSearchFilterEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionSearchFilterLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionSearchCatcherFound(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionOSHandlerEnter(UINT_PTR __unused)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionOSHandlerLeave(UINT_PTR __unused)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionUnwindFunctionEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionUnwindFunctionLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionUnwindFinallyEnter(FunctionID functionId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionUnwindFinallyLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionCatcherLeave()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable, ULONG cSlots)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionCLRCatcherFound()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ExceptionCLRCatcherExecute()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::GarbageCollectionFinished()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::HandleCreated(GCHandleID handleId, ObjectID initialObjectId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::HandleDestroyed(GCHandleID handleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ProfilerAttachComplete()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ProfilerDetachSucceeded()
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[])
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::GetAssemblyReferences(const WCHAR* wszAssemblyPath, ICorProfilerAssemblyReferenceProvider* pAsmRefProvider)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::ModuleInMemorySymbolsUpdated(ModuleID moduleId)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader)
{
    return S_OK;
}

HRESULT STDMETHODCALLTYPE LibProfiler::CorProfilerBase::DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock)
{
    return S_OK;
}
