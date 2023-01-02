namespace SharpDetect.Profiler;

[NativeObject]
public unsafe interface ICorProfilerCallback2 : ICorProfilerCallback
{
    HResult ThreadNameChanged(
        ThreadId threadId,
        uint cchName,
        char* name);

    HResult GarbageCollectionStarted(
        int cGenerations,
        bool* generationCollected,
        COR_PRF_GC_REASON reason);

    HResult SurvivingReferences(
        uint cSurvivingObjectIDRanges,
        ObjectId* objectIDRangeStart,
        uint* cObjectIDRangeLength);

    HResult GarbageCollectionFinished();

    HResult FinalizeableObjectQueued(
        int finalizerFlags,
        ObjectId objectID);

    HResult RootReferences2(
        uint cRootRefs,
        ObjectId* rootRefIds,
        COR_PRF_GC_ROOT_KIND* rootKinds,
        COR_PRF_GC_ROOT_FLAGS* rootFlags,
        uint* rootIds);

    HResult HandleCreated(
        GCHandleId handleId,
        ObjectId initialObjectId);

    HResult HandleDestroyed(
        GCHandleId handleId);
}