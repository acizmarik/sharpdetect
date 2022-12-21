namespace SharpDetect.Profiler;

[NativeObject]
public unsafe interface ICorProfilerInfo2 : ICorProfilerInfo
{
    HResult DoStackSnapshot(
        ThreadId thread,
        delegate* unmanaged[Stdcall]<FunctionId, nint, COR_PRF_FRAME_INFO, uint, byte*, void*, HResult> callback,
        COR_PRF_SNAPSHOT_INFO infoFlags,
        void* clientData,
        byte* context,
        uint contextSize);

    HResult SetEnterLeaveFunctionHooks2(
        void* pFuncEnter,
        void* pFuncLeave,
        void* pFuncTailcall);

    HResult GetFunctionInfo2(
        FunctionId funcId,
        COR_PRF_FRAME_INFO frameInfo,
        out ClassId pClassId,
        out ModuleId pModuleId,
        out MdToken pToken,
        uint cTypeArgs,
        out uint pcTypeArgs,
        ClassId* typeArgs);

    HResult GetStringLayout(
        out uint pBufferLengthOffset,
        out uint pStringLengthOffset,
        out uint pBufferOffset);

    HResult GetClassLayout(
        ClassId classID,
        out COR_FIELD_OFFSET* rFieldOffset,
        uint cFieldOffset,
        out uint pcFieldOffset,
        out uint pulClassSize);

    HResult GetClassIDInfo2(
        ClassId classId,
        out ModuleId pModuleId,
        out MdTypeDef pTypeDefToken,
        out ClassId pParentClassId,
        uint cNumTypeArgs,
        out uint pcNumTypeArgs,
        out ClassId* typeArgs);

    HResult GetCodeInfo2(
        FunctionId functionID,
        uint cCodeInfos,
        out uint pcCodeInfos,
        out COR_PRF_CODE_INFO* codeInfos);

    HResult GetClassFromTokenAndTypeArgs(
        ModuleId moduleID,
        MdTypeDef typeDef,
        uint cTypeArgs,
        ClassId* typeArgs,
        out ClassId pClassID);

    HResult GetFunctionFromTokenAndTypeArgs(
        ModuleId moduleID,
        MdMethodDef funcDef,
        ClassId classId,
        uint cTypeArgs,
        ClassId* typeArgs,
        out FunctionId pFunctionID);

    HResult EnumModuleFrozenObjects(
        ModuleId moduleID,
        out void* ppEnum);

    HResult GetArrayObjectInfo(
        ObjectId objectId,
        uint cDimensions,
        out uint* pDimensionSizes,
        out int* pDimensionLowerBounds,
        out byte* ppData);

    HResult GetBoxClassLayout(
        ClassId classId,
        out uint pBufferOffset);

    HResult GetThreadAppDomain(
        ThreadId threadId,
        out AppDomainId pAppDomainId);

    HResult GetRVAStaticAddress(
        ClassId classId,
        MdFieldDef fieldToken,
        out void* ppAddress);

    HResult GetAppDomainStaticAddress(
        ClassId classId,
        MdFieldDef fieldToken,
        AppDomainId appDomainId,
        out void* ppAddress);

    HResult GetThreadStaticAddress(
        ClassId classId,
        MdFieldDef fieldToken,
        ThreadId threadId,
        out void* ppAddress);

    HResult GetContextStaticAddress(
        ClassId classId,
        MdFieldDef fieldToken,
        ContextId contextId,
        out void* ppAddress);

    HResult GetStaticFieldInfo(
        ClassId classId,
        MdFieldDef fieldToken,
        out COR_PRF_STATIC_TYPE pFieldInfo);

    HResult GetGenerationBounds(
        uint cObjectRanges,
        out uint pcObjectRanges,
        COR_PRF_GC_GENERATION_RANGE* ranges);

    HResult GetObjectGeneration(
        ObjectId objectId,
        out COR_PRF_GC_GENERATION_RANGE range);

    HResult GetNotifiedExceptionClauseInfo(
        out COR_PRF_EX_CLAUSE_INFO pinfo);
}