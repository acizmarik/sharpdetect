namespace SharpDetect.Profiler;

[NativeObject]
public unsafe interface ICorProfilerInfo4 : ICorProfilerInfo3
{
    HResult EnumThreads(
        out IntPtr ppEnum);
    HResult InitializeCurrentThread();

    HResult RequestReJIT(
        uint cFunctions,
        ModuleId* moduleIds,
        MdMethodDef* methodIds);

    HResult RequestRevert(
        uint cFunctions,
        ModuleId* moduleIds,
        MdMethodDef* methodIds,
        HResult* status);

    HResult GetCodeInfo3(
        FunctionId functionID,
        ReJITID reJitId,
        uint cCodeInfos,
        out uint pcCodeInfos,
        COR_PRF_CODE_INFO* codeInfos);

    HResult GetFunctionFromIP2(
        nint ip,
        out FunctionId pFunctionId,
        out ReJITID pReJitId);

    HResult GetReJITIDs(
        FunctionId functionId,
        uint cReJitIds,
        out uint pcReJitIds,
        ReJITID* reJitIds);

    HResult GetILToNativeMapping2(
        FunctionId functionId,
        ReJITID reJitId,
        uint cMap,
        out uint pcMap,
        COR_DEBUG_IL_TO_NATIVE_MAP* map);

    HResult EnumJITedFunctions2(
        out IntPtr ppEnum);

    HResult GetObjectSize2(
        ObjectId objectId,
        out nint pcSize);
}
