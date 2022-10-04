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
        public event Action<(IShadowCLR Runtime, RawEventInfo Info)>? Heartbeat;
        public event Action<(IShadowCLR Runtime, RawEventInfo Info)>? ProfilerInitialized;
        public event Action<(IShadowCLR Runtime, Version? Version, RawEventInfo Info)>? ProfilerLoaded;
        public event Action<(IShadowCLR Runtime, RawEventInfo Info)>? ProfilerDestroyed;
        public event Action<(IShadowCLR Runtime, ModuleInfo Module, string Path, RawEventInfo Info)>? ModuleLoaded;
        public event Action<(IShadowCLR Runtime, TypeInfo Type, RawEventInfo Info)>? TypeLoaded;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, RawEventInfo Info)>? JITCompilationStarted;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? ThreadCreated;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? ThreadDestroyed;
        public event Action<(IShadowCLR Runtime, COR_PRF_SUSPEND_REASON Reason, RawEventInfo Info)>? RuntimeSuspendStarted;
        public event Action<(IShadowCLR Runtime, RawEventInfo Info)>? RuntimeSuspendFinished;
        public event Action<(IShadowCLR Runtime, RawEventInfo Info)>? RuntimeResumeStarted;
        public event Action<(IShadowCLR Runtime, RawEventInfo Info)>? RuntimeResumeFinished;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? RuntimeThreadSuspended;
        public event Action<(IShadowCLR Runtime, UIntPtr ThreadId, RawEventInfo Info)>? RuntimeThreadResumed;
        public event Action<(IShadowCLR Runtime, bool[] Generations, COR_PRF_GC_GENERATION_RANGE[] Bounds, RawEventInfo Info)>? GarbageCollectionStarted;
        public event Action<(IShadowCLR Runtime, COR_PRF_GC_GENERATION_RANGE[] Bounds, RawEventInfo Info)>? GarbageCollectionFinished;
        public event Action<(IShadowCLR Runtime, UIntPtr[] BlockStarts, UIntPtr[] Lengths, RawEventInfo Info)>? SurvivingReferences;
        public event Action<(IShadowCLR Runtime, UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, RawEventInfo Info)>? MovedReferences;
        #endregion

        #region REWRITING_EVENTS
        public event Action<(IShadowCLR Runtime, TypeInfo Type, RawEventInfo Info)>? TypeInjected;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, RawEventInfo Info)>? MethodInjected;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, MDToken WrapperToken, RawEventInfo Info)>? MethodWrapperInjected;
        public event Action<(IShadowCLR Runtime, TypeInfo Type, RawEventInfo Info)>? TypeReferenced;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, MethodType Type, RawEventInfo Info)>? HelperMethodReferenced;
        public event Action<(IShadowCLR Runtime, FunctionInfo Definition, FunctionInfo Reference, RawEventInfo Info)>? WrapperMethodReferenced;
        #endregion

        #region EXECUTING_EVENTS
        public event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject? Instance, RawEventInfo Info)>? FieldAccessed;
        public event Action<(IShadowCLR Runtime, IShadowObject Object, RawEventInfo Info)>? FieldInstanceAccessed;
        public event Action<(IShadowCLR Runtime, ulong Identifier, bool IsWrite, IShadowObject Instance, int Index, RawEventInfo Info)>? ArrayElementAccessed;
        public event Action<(IShadowCLR Runtime, IShadowObject Object, RawEventInfo Info)>? ArrayInstanceAccessed;
        public event Action<(IShadowCLR Runtime, int Index, RawEventInfo Info)>? ArrayIndexAccessed;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IArgumentsList? Arguments, RawEventInfo Info)>? MethodCalled;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IValueOrObject? returnValue, IArgumentsList? ByRefArguments, RawEventInfo Info)>? MethodReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? LockAcquireAttempted;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, RawEventInfo Info)>? LockAcquireReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? LockReleaseCalled;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? LockReleaseReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, RawEventInfo Info)>? ObjectWaitAttempted;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, IShadowObject Instance, bool IsSuccess, RawEventInfo Info)>? ObjectWaitReturned;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, RawEventInfo Info)>? ObjectPulseCalled;
        public event Action<(IShadowCLR Runtime, FunctionInfo Function, bool IsPulseAll, IShadowObject Instance, RawEventInfo Info)>? ObjectPulseReturned;
        #endregion

        #region PROFILING_EVENT_RAISERS
        internal void RaiseHeartbeat(IShadowCLR runtime, RawEventInfo info)
            => Heartbeat?.Invoke((runtime, info));

        internal void RaiseProfilerLoaded(IShadowCLR runtime, Version? version, RawEventInfo info)
            => ProfilerLoaded?.Invoke((runtime, version, info));

        internal void RaiseProfilerInitialized(IShadowCLR runtime, RawEventInfo info)
            => ProfilerInitialized?.Invoke((runtime, info));

        internal void RaiseProfilerDestroyed(IShadowCLR runtime, RawEventInfo info)
            => ProfilerDestroyed?.Invoke((runtime, info));

        internal void RaiseModuleLoaded(IShadowCLR runtime, ModuleInfo module, string path, RawEventInfo info)
            => ModuleLoaded?.Invoke((runtime, module, path, info));

        internal void RaiseTypeLoaded(IShadowCLR runtime, TypeInfo type, RawEventInfo info)
            => TypeLoaded?.Invoke((runtime, type, info));

        internal void RaiseJITCompilationStarted(IShadowCLR runtime, FunctionInfo function, RawEventInfo info)
            => JITCompilationStarted?.Invoke((runtime, function, info));

        internal void RaiseThreadCreated(IShadowCLR runtime, UIntPtr threadId, RawEventInfo info)
            => ThreadCreated?.Invoke((runtime, threadId, info));

        internal void RaiseThreadDestroyed(IShadowCLR runtime, UIntPtr threadId, RawEventInfo info)
            => ThreadDestroyed?.Invoke((runtime, threadId, info));

        internal void RaiseRuntimeSuspendStarted(IShadowCLR runtime, COR_PRF_SUSPEND_REASON reason, RawEventInfo info)
            => RuntimeSuspendStarted?.Invoke((runtime, reason, info));

        internal void RaiseRuntimeSuspendFinished(IShadowCLR runtime, RawEventInfo info)
            => RuntimeSuspendFinished?.Invoke((runtime, info));

        internal void RaiseRuntimeResumeStarted(IShadowCLR runtime, RawEventInfo info)
            => RuntimeResumeStarted?.Invoke((runtime, info));

        internal void RaiseRuntimeResumeFinished(IShadowCLR runtime, RawEventInfo info)
            => RuntimeResumeFinished?.Invoke((runtime, info));

        internal void RaiseRuntimeThreadSuspended(IShadowCLR runtime, UIntPtr threadId, RawEventInfo info)
            => RuntimeThreadSuspended?.Invoke((runtime, threadId, info));

        internal void RaiseRuntimeThreadResumed(IShadowCLR runtime, UIntPtr threadId, RawEventInfo info)
            => RuntimeThreadResumed?.Invoke((runtime, threadId, info));

        internal void RaiseGarbageCollectionStarted(IShadowCLR runtime, bool[] generations, COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
            => GarbageCollectionStarted?.Invoke((runtime, generations, bounds, info));

        internal void RaiseGarbageCollectionFinished(IShadowCLR runtime, COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
            => GarbageCollectionFinished?.Invoke((runtime, bounds, info));

        internal void RaiseSurvivingReferences(IShadowCLR runtime, UIntPtr[] starts, UIntPtr[] lengths, RawEventInfo info)
            => SurvivingReferences?.Invoke((runtime, starts, lengths, info));

        internal void RaiseMovedReferences(IShadowCLR runtime, UIntPtr[] oldStarts, UIntPtr[] newStarts, UIntPtr[] lengths, RawEventInfo info)
            => MovedReferences?.Invoke((runtime, oldStarts, newStarts, lengths, info));
        #endregion

        #region REWRITING_EVENT_RAISERS
        internal void RaiseTypeInjected(IShadowCLR runtime, TypeInfo type, RawEventInfo info)
            => TypeInjected?.Invoke((runtime, type, info));

        internal void RaiseMethodInjected(IShadowCLR runtime, FunctionInfo function, MethodType type, RawEventInfo info)
            => MethodInjected?.Invoke((runtime, function, type, info));

        internal void RaiseMethodWrapperInjected(IShadowCLR runtime, FunctionInfo function, MDToken wrapperToken, RawEventInfo info)
            => MethodWrapperInjected?.Invoke((runtime, function, wrapperToken, info));

        internal void RaiseTypeReferenced(IShadowCLR runtime, TypeInfo type, RawEventInfo info)
            => TypeReferenced?.Invoke((runtime, type, info));

        internal void RaiseHelperMethodReferenced(IShadowCLR runtime, FunctionInfo function, MethodType type, RawEventInfo info)
            => HelperMethodReferenced?.Invoke((runtime, function, type, info));

        internal void RaiseWrapperMethodReferenced(IShadowCLR runtime, FunctionInfo definition, FunctionInfo reference, RawEventInfo info)
            => WrapperMethodReferenced?.Invoke((runtime, definition, reference, info));
        #endregion

        #region EXECUTING_EVENT_RAISERS
        internal void RaiseFieldAccessed(IShadowCLR runtime, ulong identifier, bool isWrite, IShadowObject? instance, RawEventInfo info)
            => FieldAccessed?.Invoke((runtime, identifier, isWrite, instance, info));

        internal void RaiseFieldInstanceAccessed(IShadowCLR runtime, IShadowObject instance, RawEventInfo info)
            => FieldInstanceAccessed?.Invoke((runtime, instance, info));

        internal void RaiseArrayElementAccessed(IShadowCLR runtime, ulong identifier, bool isWrite, IShadowObject instance, int index, RawEventInfo info)
            => ArrayElementAccessed?.Invoke((runtime, identifier, isWrite, instance, index, info));

        internal void RaiseArrayInstanceAccessed(IShadowCLR runtime, IShadowObject instance, RawEventInfo info)
            => ArrayInstanceAccessed?.Invoke((runtime, instance, info));

        internal void RaiseArrayIndexAccessed(IShadowCLR runtime, int index, RawEventInfo info)
            => ArrayIndexAccessed?.Invoke((runtime, index, info));

        internal void RaiseMethodCalled(IShadowCLR runtime, FunctionInfo function, ArgumentsList? arguments, RawEventInfo info)
            => MethodCalled?.Invoke((runtime, function, arguments, info));

        internal void RaiseMethodReturned(IShadowCLR runtime, FunctionInfo function, IValueOrObject? returnValue, ArgumentsList? arguments, RawEventInfo info)
            => MethodReturned?.Invoke((runtime, function, returnValue, arguments, info));

        internal void RaiseLockAcquireAttempted(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, RawEventInfo info)
            => LockAcquireAttempted?.Invoke((runtime, function, instance, info));

        internal void RaiseLockAcquireReturned(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, bool isSuccess, RawEventInfo info)
            => LockAcquireReturned?.Invoke((runtime, function, instance, isSuccess, info));

        internal void RaiseLockReleaseCalled(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, RawEventInfo info)
            => LockReleaseCalled?.Invoke((runtime, function, instance, info));

        internal void RaiseLockReleaseReturned(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, RawEventInfo info)
            => LockReleaseReturned?.Invoke((runtime, function, instance, info));

        internal void RaiseObjectWaitAttempted(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, RawEventInfo info)
            => ObjectWaitAttempted?.Invoke((runtime, function, instance, info));

        internal void RaiseObjectWaitReturned(IShadowCLR runtime, FunctionInfo function, IShadowObject instance, bool isSuccess, RawEventInfo info)
            => ObjectWaitReturned?.Invoke((runtime, function, instance, isSuccess, info));

        internal void RaiseObjectPulseCalled(IShadowCLR runtime, FunctionInfo function, bool isPulseAll, IShadowObject instance, RawEventInfo info)
            => ObjectPulseCalled?.Invoke((runtime, function, isPulseAll, instance, info));

        internal void RaiseObjectPulseReturned(IShadowCLR runtime, FunctionInfo function, bool isPulseAll, IShadowObject instance, RawEventInfo info)
            => ObjectPulseReturned?.Invoke((runtime, function, isPulseAll, instance, info));
        #endregion
    }
}
