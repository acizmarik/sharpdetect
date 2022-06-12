using SharpDetect.Common.Interop;

namespace SharpDetect.Common.Services
{
    public interface IProfilingMessageHub: INotificationsHandler
    {
        event Action<EventInfo> Heartbeat;

        event Action<(Version? Version, EventInfo Info)> ProfilerInitialized;
        event Action<EventInfo> ProfilerDestroyed;

        event Action<(UIntPtr ModuleId, string Path, EventInfo Info)> ModuleLoaded;
        event Action<(TypeInfo TypeInfo, EventInfo Info)> TypeLoaded;
        event Action<(FunctionInfo FunctionInfo, EventInfo Info)> JITCompilationStarted;

        event Action<(UIntPtr ThreadId, EventInfo Info)> ThreadCreated;
        event Action<(UIntPtr ThreadId, EventInfo Info)> ThreadDestroyed;

        event Action<(COR_PRF_SUSPEND_REASON Reason, EventInfo Info)> RuntimeSuspendStarted;
        event Action<EventInfo> RuntimeSuspendFinished;
        event Action<(UIntPtr ThreadId, EventInfo Info)> RuntimeThreadSuspended;
        event Action<(UIntPtr ThreadId, EventInfo Info)> RuntimeThreadResumed;

        event Action<(bool[] GenerationsCollected, COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)> GarbageCollectionStarted;
        event Action<(COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)> GarbageCollectionFinished;
        event Action<(UIntPtr[] BlockStarts, UIntPtr[] Lengths, EventInfo Info)> SurvivingReferences;
        event Action<(UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, EventInfo Info)> MovedReferences;
    }
}