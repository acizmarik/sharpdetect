using dnlib.DotNet;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;

namespace SharpDetect.Common.Services
{
    public interface IShadowExecutionObserver
    {
        event Action<(IShadowCLR Runtime, EventInfo Info)>? Heartbeat;
        event Action<(IShadowCLR Runtime, EventInfo Info)>? ProfilerInitialized;
        event Action<(IShadowCLR Runtime, Version? Version, EventInfo Info)>? ProfilerLoaded;
        event Action<(IShadowCLR Runtime, EventInfo Info)>? ProfilerDestroyed;
        event Action<(IShadowCLR Runtime, ModuleInfo Module, string Path, EventInfo Info)>? ModuleLoaded;
        event Action<(IShadowCLR Runtime, TypeInfo Type, EventInfo Info)>? TypeLoaded;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, EventInfo Info)>? JITCompilationStarted;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? ThreadCreated;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? ThreadDestroyed;
        event Action<(IShadowCLR Runtime, COR_PRF_SUSPEND_REASON Reason, EventInfo Info)>? RuntimeSuspendStarted;
        event Action<(IShadowCLR Runtime, EventInfo Info)>? RuntimeSuspendFinished;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? RuntimeThreadSuspended;
        event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? RuntimeThreadResumed;
        event Action<(IShadowCLR Runtime, bool[] Generations, COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)>? GarbageCollectionStarted;
        event Action<(IShadowCLR Runtime, COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)>? GarbageCollectionFinished;
        event Action<(IShadowCLR Runtime, UIntPtr[] BlockStarts, UIntPtr[] Lengths, EventInfo Info)>? SurvivingReferences;
        event Action<(IShadowCLR Runtime, UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, EventInfo Info)>? MovedReferences;

        event Action<(IShadowCLR Runtime, TypeInfo Type, EventInfo Info)>? TypeInjected;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, EventInfo Info)>? MethodInjected;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, MDToken WrapperToken, EventInfo Info)>? MethodWrapperInjected;
        event Action<(IShadowCLR Runtime, TypeInfo Type, EventInfo Info)>? TypeReferenced;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, EventInfo Info)>? HelperMethodReferenced;
        event Action<(IShadowCLR Runtime, FunctionInfo Definition, FunctionInfo Reference, EventInfo Info)>? WrapperMethodReferenced;

        event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject? Instance, EventInfo Info)>? FieldAccessed;
        event Action<(IShadowCLR Runtime, IShadowObject Object, EventInfo Info)>? FieldInstanceAccessed;
        event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject Instance, int Index, EventInfo Info)>? ArrayElementAccessed;
        event Action<(IShadowCLR Runtime, IShadowObject Object, EventInfo Info)>? ArrayInstanceAccessed;
        event Action<(IShadowCLR Runtime, int Index, EventInfo Info)>? ArrayIndexAccessed;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IArgumentsList? Arguments, EventInfo Info)>? MethodCalled;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IValueOrObject? returnValue, IArgumentsList? ByRefArguments, EventInfo Info)>? MethodReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? LockAcquireAttempted;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, EventInfo Info)>? LockAcquireReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? LockReleaseCalled;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? LockReleaseReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? ObjectWaitAttempted;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, EventInfo Info)>? ObjectWaitReturned;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, EventInfo Info)>? ObjectPulseCalled;
        event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, EventInfo Info)>? ObjectPulseReturned;
    }
}
