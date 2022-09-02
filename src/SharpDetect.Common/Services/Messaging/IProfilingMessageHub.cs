using SharpDetect.Common.Interop;

namespace SharpDetect.Common.Services
{
    public interface IProfilingMessageHub: INotificationsHandler
    {
        event Action<RawEventInfo> Heartbeat;

        event Action<(Version? Version, RawEventInfo Info)> ProfilerInitialized;
        event Action<RawEventInfo> ProfilerDestroyed;

        event Action<(UIntPtr ModuleId, string Path, RawEventInfo Info)> ModuleLoaded;
        event Action<(TypeInfo TypeInfo, RawEventInfo Info)> TypeLoaded;
        event Action<(FunctionInfo FunctionInfo, RawEventInfo Info)> JITCompilationStarted;

        event Action<(UIntPtr ThreadId, RawEventInfo Info)> ThreadCreated;
        event Action<(UIntPtr ThreadId, RawEventInfo Info)> ThreadDestroyed;

        event Action<(COR_PRF_SUSPEND_REASON Reason, RawEventInfo Info)> RuntimeSuspendStarted;
        event Action<RawEventInfo> RuntimeSuspendFinished;
        event Action<(UIntPtr ThreadId, RawEventInfo Info)> RuntimeThreadSuspended;
        event Action<(UIntPtr ThreadId, RawEventInfo Info)> RuntimeThreadResumed;

        event Action<(bool[] GenerationsCollected, COR_PRF_GC_GENERATION_RANGE[] Bounds, RawEventInfo Info)> GarbageCollectionStarted;
        event Action<(COR_PRF_GC_GENERATION_RANGE[] Bounds, RawEventInfo Info)> GarbageCollectionFinished;
        event Action<(UIntPtr[] BlockStarts, UIntPtr[] Lengths, RawEventInfo Info)> SurvivingReferences;
        event Action<(UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, RawEventInfo Info)> MovedReferences;
    }
}