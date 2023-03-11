// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace SharpDetect.Profiler;

[NativeObject]
public unsafe interface ICorProfilerInfo : IUnknown
{
    HResult GetClassFromObject(
        ObjectId ObjectId,
        out ClassId pClassId);

    HResult GetClassFromToken(
        ModuleId ModuleId,
        MdTypeDef typeDef,
        out ClassId pClassId);

    HResult GetCodeInfo(
        FunctionId FunctionId,
        out byte* pStart,
        out uint pcSize);

    HResult GetEventMask(
        out int pdwEvents);

    HResult GetFunctionFromIP(
        nint ip,
        out FunctionId pFunctionId);

    HResult GetFunctionFromToken(
        ModuleId ModuleId,
        MdToken token,
        out FunctionId pFunctionId);

    HResult GetHandleFromThread(
        ThreadId ThreadId,
        out nint phThread);

    HResult GetObjectSize(
        ObjectId ObjectId,
        out uint pcSize);

    HResult IsArrayClass(
        ClassId ClassId,
        out CorElementType pBaseElemType,
        out ClassId pBaseClassId,
        out uint pcRank);

    HResult GetThreadInfo(
        ThreadId ThreadId,
        out int pdwWin32ThreadId);

    HResult GetCurrentThreadId(
        out ThreadId pThreadId);

    HResult GetClassIdInfo(
        ClassId ClassId,
        out ModuleId pModuleId,
        out MdTypeDef pTypeDefToken);

    HResult GetFunctionInfo(
        FunctionId FunctionId,
        out ClassId pClassId,
        out ModuleId pModuleId,
        out MdToken pToken);

    HResult SetEventMask(
        COR_PRF_MONITOR dwEvents);

    HResult SetEnterLeaveFunctionHooks(
        void* pFuncEnter,
        void* pFuncLeave,
        void* pFuncTailcall);

    HResult SetFunctionIdMapper(
        void* pFunc);

    HResult GetTokenAndMetaDataFromFunction(
        FunctionId FunctionId,
        in Guid riid,
        out IntPtr ppImport,
        out MdToken pToken);

    HResult GetModuleInfo(
        ModuleId ModuleId,
        out nint ppBaseLoadAddress,
        uint cchName,
        out uint pcchName,
        char* szName,
        out AssemblyId pAssemblyId);

    HResult GetModuleMetaData(
        ModuleId ModuleId,
        CorOpenFlags dwOpenFlags,
        Guid riid,
        out IntPtr ppOut);

    HResult GetILFunctionBody(
        ModuleId ModuleId,
        MdMethodDef methodId,
        out byte* ppMethodHeader,
        out uint pcbMethodSize);

    HResult GetILFunctionBodyAllocator(
        ModuleId ModuleId,
        out IntPtr ppMalloc);

    HResult SetILFunctionBody(
        ModuleId ModuleId,
        MdMethodDef methodid,
        IntPtr pbNewILMethodHeader);

    HResult GetAppDomainInfo(
        AppDomainId appDomainId,
        uint cchName,
        out uint pcchName,
        out char* szName,
        out ProcessId pProcessId);

    HResult GetAssemblyInfo(
        AssemblyId assemblyId,
        uint cchName,
        out uint pcchName,
        char* szName,
        out AppDomainId pAppDomainId,
        out ModuleId pModuleId);

    HResult SetFunctionReJIT(
        FunctionId functionId);

    HResult ForceGC();

    HResult SetILInstrumentedCodeMap(
        FunctionId FunctionId,
        bool fStartJit,
        uint cILMapEntries,
        CorILMap* rgILMapEntries);

    HResult GetInprocInspectionInterface(
        out void* ppicd);

    HResult GetInprocInspectionIThisThread(
        out void* ppicd);

    HResult GetThreadContext(
        ThreadId ThreadId,
        out ContextId pContextId);

    HResult BeginInprocDebugging(
        bool fThisThreadOnly,
        out int pdwProfilerContext);

    HResult EndInprocDebugging(
        int dwProfilerContext);

    HResult GetILToNativeMapping(
        FunctionId FunctionId,
        uint cMap,
        out uint pcMap,
        CorDebugILToNativeMap* map);
}