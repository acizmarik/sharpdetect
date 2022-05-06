using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Core.Runtime.Arguments;

namespace SharpDetect.Core.Runtime
{
    internal class RuntimeEventsHub : IShadowExecutionObserver
    {
        #region PROFILING_EVENTS
        public event Action<(IShadowCLR Runtime, EventInfo Info)>? Heartbeat;
        public event Action<(IShadowCLR Runtime, EventInfo Info)>? ProfilerInitialized;
        public event Action<(IShadowCLR Runtime, Version? Version, EventInfo Info)>? ProfilerLoaded;
        public event Action<(IShadowCLR Runtime, EventInfo Info)>? ProfilerDestroyed;
        public event Action<(IShadowCLR Runtime, ModuleInfo Module, string Path, EventInfo Info)>? ModuleLoaded;
        public event Action<(IShadowCLR Runtime, TypeInfo Type, EventInfo Info)>? TypeLoaded;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, EventInfo Info)>? JITCompilationStarted;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? ThreadCreated;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? ThreadDestroyed;
        public event Action<(IShadowCLR Runtime, COR_PRF_SUSPEND_REASON Reason, EventInfo Info)>? RuntimeSuspendStarted;
        public event Action<(IShadowCLR Runtime, EventInfo Info)>? RuntimeSuspendFinished;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? RuntimeThreadSuspended;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, EventInfo Info)>? RuntimeThreadResumed;
        public event Action<(IShadowCLR Runtime, bool[] Generations, COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)>? GarbageCollectionStarted;
        public event Action<(IShadowCLR Runtime, COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info)>? GarbageCollectionFinished;
        public event Action<(IShadowCLR Runtime, UIntPtr[] BlockStarts, UIntPtr[] Lengths, EventInfo Info)>? SurvivingReferences;
        public event Action<(IShadowCLR Runtime, UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, EventInfo Info)>? MovedReferences;
        #endregion

        #region REWRITING_EVENTS
        public event Action<(IShadowCLR Runtime, TypeInfo Type, EventInfo Info)>? TypeInjected;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, EventInfo Info)>? MethodInjected;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, MDToken WrapperToken, EventInfo Info)>? MethodWrapperInjected;
        public event Action<(IShadowCLR Runtime, TypeInfo Type, EventInfo Info)>? TypeReferenced;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, EventInfo Info)>? HelperMethodReferenced;
        public event Action<(IShadowCLR Runtime, FunctionInfo Definition, FunctionInfo Reference, EventInfo Info)>? WrapperMethodReferenced;
        #endregion

        #region EXECUTING_EVENTS
        public event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject Instance, EventInfo Info)>? FieldAccessed;
        public event Action<(IShadowCLR Runtime, IShadowObject Object, EventInfo Info)>? FieldInstanceAccessed;
        public event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject Instance, int Index, EventInfo Info)>? ArrayElementAccessed;
        public event Action<(IShadowCLR Runtime, IShadowObject Object, EventInfo Info)>? ArrayInstanceAccessed;
        public event Action<(IShadowCLR Runtime, int Index, EventInfo Info)>? ArrayIndexAccessed;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IArgumentsList? Arguments, EventInfo Info)>? MethodCalled;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IValueOrObject? returnValue, IArgumentsList? ByRefArguments, EventInfo Info)>? MethodReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? LockAcquireAttempted;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, EventInfo Info)>? LockAcquireReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? LockReleaseCalled;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? LockReleaseReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, EventInfo Info)>? ObjectWaitAttempted;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, EventInfo Info)>? ObjectWaitReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, EventInfo Info)>? ObjectPulseCalled;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, EventInfo Info)>? ObjectPulseReturned;
        #endregion

        #region PROFILING_EVENT_RAISERS
        internal void RaiseHeartbeat(IShadowCLR runtime, EventInfo info)
            => Heartbeat?.Invoke((runtime, info));

        internal void RaiseProfilerLoaded(IShadowCLR runtime, Version? version, EventInfo info)
            => ProfilerLoaded?.Invoke((runtime, version, info));

        internal void RaiseProfilerInitialized(IShadowCLR runtime, EventInfo info)
            => ProfilerInitialized?.Invoke((runtime, info));

        internal void RaiseProfilerDestroyed(IShadowCLR runtime, EventInfo info)
            => ProfilerDestroyed?.Invoke((runtime, info));

        internal void RaiseModuleLoaded(IShadowCLR runtime, ModuleInfo module, string path, EventInfo info)
            => ModuleLoaded?.Invoke((runtime, module, path, info));

        internal void RaiseTypeLoaded(IShadowCLR runtime, TypeInfo type, EventInfo info)
            => TypeLoaded?.Invoke((runtime, type, info));

        internal void RaiseJITCompilationStarted(IShadowCLR runtime, FunctionInfo function, EventInfo info)
            => JITCompilationStarted?.Invoke((runtime, function, info));

        internal void RaiseThreadCreated(IShadowCLR runtime, UIntPtr threadId, EventInfo info)
            => ThreadCreated?.Invoke((runtime, threadId, info));

        internal void RaiseThreadDestroyed(IShadowCLR runtime, UIntPtr threadId, EventInfo info)
            => ThreadDestroyed?.Invoke((runtime, threadId, info));

        internal void RaiseRuntimeSuspendStarted(IShadowCLR runtime, COR_PRF_SUSPEND_REASON reason, EventInfo info)
            => RuntimeSuspendStarted?.Invoke((runtime, reason, info));

        internal void RaiseRuntimeSuspendFinished(IShadowCLR runtime, EventInfo info)
            => RuntimeSuspendFinished?.Invoke((runtime, info));

        internal void RaiseRuntimeThreadSuspended(IShadowCLR runtime, UIntPtr threadId, EventInfo info)
            => RuntimeThreadSuspended?.Invoke((runtime, threadId, info));

        internal void RaiseRuntimeThreadResumed(IShadowCLR runtime, UIntPtr threadId, EventInfo info)
            => RuntimeThreadResumed?.Invoke((runtime, threadId, info));

        internal void RaiseGarbageCollectionStarted(IShadowCLR runtime, bool[] generations, COR_PRF_GC_GENERATION_RANGE[] bounds, EventInfo info)
            => GarbageCollectionStarted?.Invoke((runtime, generations, bounds, info));

        internal void RaiseGarbageCollectionFinished(IShadowCLR runtime, COR_PRF_GC_GENERATION_RANGE[] bounds, EventInfo info)
            => GarbageCollectionFinished?.Invoke((runtime, bounds, info));

        internal void RaiseSurvivingReferences(IShadowCLR runtime, UIntPtr[] starts, UIntPtr[] lengths, EventInfo info)
            => SurvivingReferences?.Invoke((runtime, starts, lengths, info));

        internal void RaiseMovedReferences(IShadowCLR runtime, UIntPtr[] oldStarts, UIntPtr[] newStarts, UIntPtr[] lengths, EventInfo info)
            => MovedReferences?.Invoke((runtime, oldStarts, newStarts, lengths, info));
        #endregion

        #region REWRITING_EVENT_RAISERS
        internal void RaiseTypeInjected(IShadowCLR runtime, TypeInfo type, EventInfo info)
            => TypeInjected?.Invoke((runtime, type, info));

        internal void RaiseMethodInjected(IShadowCLR runtime, FunctionInfo function, MethodType type, EventInfo info)
            => MethodInjected?.Invoke((runtime, function, type, info));

        internal void RaiseMethodWrapperInjected(IShadowCLR runtime, FunctionInfo function, MDToken wrapperToken, EventInfo info)
            => MethodWrapperInjected?.Invoke((runtime, function, wrapperToken, info));

        internal void RaiseTypeReferenced(IShadowCLR runtime, TypeInfo type, EventInfo info)
            => TypeReferenced?.Invoke((runtime, type, info));

        internal void RaiseHelperMethodReferenced(IShadowCLR runtime, FunctionInfo function, MethodType type, EventInfo info)
            => HelperMethodReferenced?.Invoke((runtime, function, type, info));

        internal void RaiseWrapperMethodReferenced(IShadowCLR runtime, FunctionInfo definition, FunctionInfo reference, EventInfo info)
            => WrapperMethodReferenced?.Invoke((runtime, definition, reference, info));
        #endregion

        #region EXECUTING_EVENT_RAISERS
        internal void RaiseFieldAccessed(IShadowCLR runtime, ulong identifier, bool isWrite, IShadowObject instance, EventInfo info)
            => FieldAccessed?.Invoke((runtime, identifier, isWrite, instance, info));

        internal void RaiseFieldInstanceAccessed(IShadowCLR runtime, IShadowObject instance, EventInfo info)
            => FieldInstanceAccessed?.Invoke((runtime, instance, info));

        internal void RaiseArrayElementAccessed(IShadowCLR runtime, ulong identifier, bool isWrite, IShadowObject instance, int index, EventInfo info)
            => ArrayElementAccessed?.Invoke((runtime, identifier, isWrite, instance, index, info));

        internal void RaiseArrayInstanceAccessed(IShadowCLR runtime, IShadowObject instance, EventInfo info)
            => ArrayInstanceAccessed?.Invoke((runtime, instance, info));

        internal void RaiseArrayIndexAccessed(IShadowCLR runtime, int index, EventInfo info)
            => ArrayIndexAccessed?.Invoke((runtime, index, info));

        internal void RaiseMethodCalled(IShadowCLR runtime, FunctionInfo function, ArgumentsList? arguments, EventInfo info)
            => MethodCalled?.Invoke((runtime, function, arguments, info));

        internal void RaiseMethodReturned(IShadowCLR runtime, FunctionInfo function, IValueOrObject? returnValue, ArgumentsList? arguments, EventInfo info)
            => MethodReturned?.Invoke((runtime, function, returnValue, arguments, info));

        internal void RaiseLockAcquireAttempted(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, EventInfo info)
            => LockAcquireAttempted?.Invoke((runtime, function, instance, info));

        internal void RaiseLockAcquireReturned(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, bool isSuccess, EventInfo info)
            => LockAcquireReturned?.Invoke((runtime, function, instance, isSuccess, info));

        internal void RaiseLockReleaseCalled(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, EventInfo info)
            => LockReleaseCalled?.Invoke((runtime, function, instance, info));

        internal void RaiseLockReleaseReturned(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, EventInfo info)
            => LockReleaseReturned?.Invoke((runtime, function, instance, info));

        internal void RaiseObjectWaitAttempted(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, EventInfo info)
            => ObjectWaitAttempted?.Invoke((runtime, function, instance, info));

        internal void RaiseObjectWaitReturned(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, bool isSuccess, EventInfo info)
            => ObjectWaitReturned?.Invoke((runtime, function, instance, isSuccess, info));

        internal void RaiseObjectPulseCalled(IShadowCLR runtime, FunctionInfo function, bool isPulseAll, IShadowObject instance, EventInfo info)
            => ObjectPulseCalled?.Invoke((runtime, function, isPulseAll, instance, info));

        internal void RaiseObjectPulseReturned(IShadowCLR runtime, FunctionInfo function, bool isPulseAll, IShadowObject instance, EventInfo info)
            => ObjectPulseReturned?.Invoke((runtime, function, isPulseAll, instance, info));
        #endregion
    }
}
