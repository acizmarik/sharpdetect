// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE.coreclr.txt file in the project root for full license information.

#pragma once

#include <atomic>
#include "cor.h"
#include "corprof.h"

namespace LibProfiler
{
    class CorProfilerBase : public ICorProfilerCallback8
    {
    private:
        std::atomic<int> _refCount;

    protected:
        ICorProfilerInfo8* _corProfilerInfo;

    public:
        CorProfilerBase();
        virtual ~CorProfilerBase();
        virtual HRESULT STDMETHODCALLTYPE Initialize(IUnknown* pICorProfilerInfoUnk) override;
        virtual HRESULT STDMETHODCALLTYPE Shutdown() override;
        virtual HRESULT STDMETHODCALLTYPE AppDomainCreationStarted(AppDomainID appDomainId) override;
        virtual HRESULT STDMETHODCALLTYPE AppDomainCreationFinished(AppDomainID appDomainId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE AppDomainShutdownStarted(AppDomainID appDomainId) override;
        virtual HRESULT STDMETHODCALLTYPE AppDomainShutdownFinished(AppDomainID appDomainId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE AssemblyLoadStarted(AssemblyID assemblyId) override;
        virtual HRESULT STDMETHODCALLTYPE AssemblyLoadFinished(AssemblyID assemblyId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE AssemblyUnloadStarted(AssemblyID assemblyId) override;
        virtual HRESULT STDMETHODCALLTYPE AssemblyUnloadFinished(AssemblyID assemblyId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE ModuleLoadStarted(ModuleID moduleId) override;
        virtual HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE ModuleUnloadStarted(ModuleID moduleId) override;
        virtual HRESULT STDMETHODCALLTYPE ModuleUnloadFinished(ModuleID moduleId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE ModuleAttachedToAssembly(ModuleID moduleId, AssemblyID AssemblyId) override;
        virtual HRESULT STDMETHODCALLTYPE ClassLoadStarted(ClassID classId) override;
        virtual HRESULT STDMETHODCALLTYPE ClassLoadFinished(ClassID classId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE ClassUnloadStarted(ClassID classId) override;
        virtual HRESULT STDMETHODCALLTYPE ClassUnloadFinished(ClassID classId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE FunctionUnloadStarted(FunctionID functionId) override;
        virtual HRESULT STDMETHODCALLTYPE JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock) override;
        virtual HRESULT STDMETHODCALLTYPE JITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
        virtual HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchStarted(FunctionID functionId, BOOL* pbUseCachedFunction) override;
        virtual HRESULT STDMETHODCALLTYPE JITCachedFunctionSearchFinished(FunctionID functionId, COR_PRF_JIT_CACHE result) override;
        virtual HRESULT STDMETHODCALLTYPE JITFunctionPitched(FunctionID functionId) override;
        virtual HRESULT STDMETHODCALLTYPE JITInlining(FunctionID callerId, FunctionID calleeId, BOOL* pfShouldInline) override;
        virtual HRESULT STDMETHODCALLTYPE ThreadCreated(ThreadID threadId) override;
        virtual HRESULT STDMETHODCALLTYPE ThreadDestroyed(ThreadID threadId) override;
        virtual HRESULT STDMETHODCALLTYPE ThreadAssignedToOSThread(ThreadID managedThreadId, DWORD osThreadId) override;
        virtual HRESULT STDMETHODCALLTYPE RemotingClientInvocationStarted() override;
        virtual HRESULT STDMETHODCALLTYPE RemotingClientSendingMessage(GUID* pCookie, BOOL fIsAsync) override;
        virtual HRESULT STDMETHODCALLTYPE RemotingClientReceivingReply(GUID* pCookie, BOOL fIsAsync) override;
        virtual HRESULT STDMETHODCALLTYPE RemotingClientInvocationFinished() override;
        virtual HRESULT STDMETHODCALLTYPE RemotingServerReceivingMessage(GUID* pCookie, BOOL fIsAsync) override;
        virtual HRESULT STDMETHODCALLTYPE RemotingServerInvocationStarted() override;
        virtual HRESULT STDMETHODCALLTYPE RemotingServerInvocationReturned() override;
        virtual HRESULT STDMETHODCALLTYPE RemotingServerSendingReply(GUID* pCookie, BOOL fIsAsync) override;
        virtual HRESULT STDMETHODCALLTYPE UnmanagedToManagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
        virtual HRESULT STDMETHODCALLTYPE ManagedToUnmanagedTransition(FunctionID functionId, COR_PRF_TRANSITION_REASON reason) override;
        virtual HRESULT STDMETHODCALLTYPE RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason) override;
        virtual HRESULT STDMETHODCALLTYPE RuntimeSuspendFinished() override;
        virtual HRESULT STDMETHODCALLTYPE RuntimeSuspendAborted() override;
        virtual HRESULT STDMETHODCALLTYPE RuntimeResumeStarted() override;
        virtual HRESULT STDMETHODCALLTYPE RuntimeResumeFinished() override;
        virtual HRESULT STDMETHODCALLTYPE RuntimeThreadSuspended(ThreadID threadId) override;
        virtual HRESULT STDMETHODCALLTYPE RuntimeThreadResumed(ThreadID threadId) override;
        virtual HRESULT STDMETHODCALLTYPE MovedReferences(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], ULONG cObjectIDRangeLength[]) override;
        virtual HRESULT STDMETHODCALLTYPE ObjectAllocated(ObjectID objectId, ClassID classId) override;
        virtual HRESULT STDMETHODCALLTYPE ObjectsAllocatedByClass(ULONG cClassCount, ClassID classIds[], ULONG cObjects[]) override;
        virtual HRESULT STDMETHODCALLTYPE ObjectReferences(ObjectID objectId, ClassID classId, ULONG cObjectRefs, ObjectID objectRefIds[]) override;
        virtual HRESULT STDMETHODCALLTYPE RootReferences(ULONG cRootRefs, ObjectID rootRefIds[]) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionThrown(ObjectID thrownObjectId) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionEnter(FunctionID functionId) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFunctionLeave() override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFilterEnter(FunctionID functionId) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchFilterLeave() override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionSearchCatcherFound(FunctionID functionId) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionOSHandlerEnter(UINT_PTR __unused) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionOSHandlerLeave(UINT_PTR __unused) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionEnter(FunctionID functionId) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFunctionLeave() override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyEnter(FunctionID functionId) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionUnwindFinallyLeave() override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionCatcherEnter(FunctionID functionId, ObjectID objectId) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionCatcherLeave() override;
        virtual HRESULT STDMETHODCALLTYPE COMClassicVTableCreated(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable, ULONG cSlots) override;
        virtual HRESULT STDMETHODCALLTYPE COMClassicVTableDestroyed(ClassID wrappedClassId, REFGUID implementedIID, void* pVTable) override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherFound() override;
        virtual HRESULT STDMETHODCALLTYPE ExceptionCLRCatcherExecute() override;
        virtual HRESULT STDMETHODCALLTYPE ThreadNameChanged(ThreadID threadId, ULONG cchName, WCHAR name[]) override;
        virtual HRESULT STDMETHODCALLTYPE GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason) override;
        virtual HRESULT STDMETHODCALLTYPE SurvivingReferences(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], ULONG cObjectIDRangeLength[]) override;
        virtual HRESULT STDMETHODCALLTYPE GarbageCollectionFinished() override;
        virtual HRESULT STDMETHODCALLTYPE FinalizeableObjectQueued(DWORD finalizerFlags, ObjectID objectID) override;
        virtual HRESULT STDMETHODCALLTYPE RootReferences2(ULONG cRootRefs, ObjectID rootRefIds[], COR_PRF_GC_ROOT_KIND rootKinds[], COR_PRF_GC_ROOT_FLAGS rootFlags[], UINT_PTR rootIds[]) override;
        virtual HRESULT STDMETHODCALLTYPE HandleCreated(GCHandleID handleId, ObjectID initialObjectId) override;
        virtual HRESULT STDMETHODCALLTYPE HandleDestroyed(GCHandleID handleId) override;
        virtual HRESULT STDMETHODCALLTYPE InitializeForAttach(IUnknown* pCorProfilerInfoUnk, void* pvClientData, UINT cbClientData) override;
        virtual HRESULT STDMETHODCALLTYPE ProfilerAttachComplete() override;
        virtual HRESULT STDMETHODCALLTYPE ProfilerDetachSucceeded() override;
        virtual HRESULT STDMETHODCALLTYPE ReJITCompilationStarted(FunctionID functionId, ReJITID rejitId, BOOL fIsSafeToBlock) override;
        virtual HRESULT STDMETHODCALLTYPE GetReJITParameters(ModuleID moduleId, mdMethodDef methodId, ICorProfilerFunctionControl* pFunctionControl) override;
        virtual HRESULT STDMETHODCALLTYPE ReJITCompilationFinished(FunctionID functionId, ReJITID rejitId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;
        virtual HRESULT STDMETHODCALLTYPE ReJITError(ModuleID moduleId, mdMethodDef methodId, FunctionID functionId, HRESULT hrStatus) override;
        virtual HRESULT STDMETHODCALLTYPE MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
        virtual HRESULT STDMETHODCALLTYPE SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[]) override;
        virtual HRESULT STDMETHODCALLTYPE ConditionalWeakTableElementReferences(ULONG cRootRefs, ObjectID keyRefIds[], ObjectID valueRefIds[], GCHandleID rootIds[]) override;
        virtual HRESULT STDMETHODCALLTYPE GetAssemblyReferences(const WCHAR* wszAssemblyPath, ICorProfilerAssemblyReferenceProvider* pAsmRefProvider) override;
        virtual HRESULT STDMETHODCALLTYPE ModuleInMemorySymbolsUpdated(ModuleID moduleId) override;
        virtual HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock, LPCBYTE ilHeader, ULONG cbILHeader) override;
        virtual HRESULT STDMETHODCALLTYPE DynamicMethodJITCompilationFinished(FunctionID functionId, HRESULT hrStatus, BOOL fIsSafeToBlock) override;

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
            return std::atomic_fetch_add(&this->_refCount, 1) + 1;
        }

        ULONG STDMETHODCALLTYPE Release(void) override
        {
            int count = std::atomic_fetch_sub(&this->_refCount, 1) - 1;

            if (count <= 0)
            {
                delete this;
            }

            return count;
        }
    };
}