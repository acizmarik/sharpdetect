// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[NativeObject]
public unsafe interface ICorProfilerInfo3 : ICorProfilerInfo2
{
    HResult EnumJITedFunctions(
        out void* ppEnum);

    HResult RequestProfilerDetach(
        int dwExpectedCompletionMilliseconds);

    HResult SetFunctionIDMapper2(
        delegate* unmanaged[Stdcall]<FunctionId, IntPtr, bool*, IntPtr> pFunc,
        IntPtr clientData);

    HResult GetStringLayout2(
        out uint pStringLengthOffset,
        out uint pBufferOffset);

    HResult SetEnterLeaveFunctionHooks3(
        void* pFuncEnter3,
        void* pFuncLeave3,
        void* pFuncTailcall3);


    HResult SetEnterLeaveFunctionHooks3WithInfo(
        IntPtr pFuncEnter3WithInfo,
        IntPtr pFuncLeave3WithInfo,
        IntPtr pFuncTailcall3WithInfo);

    HResult GetFunctionEnter3Info(
        FunctionId functionId,
        COR_PRF_ELT_INFO eltInfo,
        out COR_PRF_FRAME_INFO pFrameInfo,
        ulong* pcbArgumentInfo,
        byte* pArgumentInfo);

    HResult GetFunctionLeave3Info(
        FunctionId functionId,
        COR_PRF_ELT_INFO eltInfo,
        out COR_PRF_FRAME_INFO pFrameInfo,
        out COR_PRF_FUNCTION_ARGUMENT_RANGE pRetvalRange);

    HResult GetFunctionTailcall3Info(
        FunctionId functionId,
        COR_PRF_ELT_INFO eltInfo,
        out COR_PRF_FRAME_INFO pFrameInfo);

    HResult EnumModules(
        out void* ppEnum);

    HResult GetRuntimeInformation(
        out ushort pClrInstanceId,
        out COR_PRF_RUNTIME_TYPE pRuntimeType,
        out ushort pMajorVersion,
        out ushort pMinorVersion,
        out ushort pBuildNumber,
        out ushort pQFEVersion,
        uint cchVersionString,
        out uint pcchVersionString,
        char* szVersionString);

    HResult GetThreadStaticAddress2(
        ClassId classId,
        MdFieldDef fieldToken,
        AppDomainId appDomainId,
        ThreadId threadId,
        out void* ppAddress);

    HResult GetAppDomainsContainingModule(
        ModuleId moduleId,
        uint cAppDomainIds,
        out uint pcAppDomainIds,
        AppDomainId* appDomainIds);

    HResult GetModuleInfo2(
        ModuleId moduleId,
        out byte* ppBaseLoadAddress,
        uint cchName,
        out uint pcchName,
        char* szName,
        out AssemblyId pAssemblyId,
        out int pdwModuleFlags);
}