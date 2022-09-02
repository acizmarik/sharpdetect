using dnlib.DotNet;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Common.Services
{
    public interface IShadowExecutionObserver
    {
        event Action<(IShadowCLR Runtime, RawEventInfo Info)>? Heartbeat;
        event Action<(IShadowCLR Runtime, RawEventInfo Info)>? ProfilerInitialized;
        event Action<(IShadowCLR Runtime, Version? Version, RawEventInfo Info)>? ProfilerLoaded;
        event Action<(IShadowCLR Runtime, RawEventInfo Info)>? ProfilerDestroyed;
        event Action<(IShadowCLR Runtime, ModuleInfo Module, string Path, RawEventInfo Info)>? ModuleLoaded;
        event Action<(IShadowCLR Runtime, TypeInfo Type, RawEventInfo Info)>? TypeLoaded;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, RawEventInfo Info)>? JITCompilationStarted;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? ThreadCreated;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? ThreadDestroyed;
        event Action<(IShadowCLR Runtime, COR_PRF_SUSPEND_REASON Reason, RawEventInfo Info)>? RuntimeSuspendStarted;
        event Action<(IShadowCLR Runtime, RawEventInfo Info)>? RuntimeSuspendFinished;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? RuntimeThreadSuspended;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? RuntimeThreadResumed;
        event Action<(IShadowCLR Runtime, bool[] Generations, COR_PRF_GC_GENERATION_RANGE[] Bounds, RawEventInfo Info)>? GarbageCollectionStarted;
        event Action<(IShadowCLR Runtime, COR_PRF_GC_GENERATION_RANGE[] Bounds, RawEventInfo Info)>? GarbageCollectionFinished;
        event Action<(IShadowCLR Runtime, UIntPtr[] BlockStarts, UIntPtr[] Lengths, RawEventInfo Info)>? SurvivingReferences;
        event Action<(IShadowCLR Runtime, UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, RawEventInfo Info)>? MovedReferences;

        event Action<(IShadowCLR Runtime, TypeInfo Type, RawEventInfo Info)>? TypeInjected;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, RawEventInfo Info)>? MethodInjected;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, MDToken WrapperToken, RawEventInfo Info)>? MethodWrapperInjected;
        event Action<(IShadowCLR Runtime, TypeInfo Type, RawEventInfo Info)>? TypeReferenced;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, RawEventInfo Info)>? HelperMethodReferenced;
        event Action<(IShadowCLR Runtime, FunctionInfo Definition, FunctionInfo Reference, RawEventInfo Info)>? WrapperMethodReferenced;

        event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject? Instance, RawEventInfo Info)>? FieldAccessed;
        event Action<(IShadowCLR Runtime, IShadowObject Object, RawEventInfo Info)>? FieldInstanceAccessed;
        event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject Instance, int Index, RawEventInfo Info)>? ArrayElementAccessed;
        event Action<(IShadowCLR Runtime, IShadowObject Object, RawEventInfo Info)>? ArrayInstanceAccessed;
        event Action<(IShadowCLR Runtime, int Index, RawEventInfo Info)>? ArrayIndexAccessed;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IArgumentsList? Arguments, RawEventInfo Info)>? MethodCalled;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IValueOrObject? returnValue, IArgumentsList? ByRefArguments, RawEventInfo Info)>? MethodReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? LockAcquireAttempted;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, RawEventInfo Info)>? LockAcquireReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? LockReleaseCalled;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? LockReleaseReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? ObjectWaitAttempted;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, RawEventInfo Info)>? ObjectWaitReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, RawEventInfo Info)>? ObjectPulseCalled;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, RawEventInfo Info)>? ObjectPulseReturned;
    }
}
