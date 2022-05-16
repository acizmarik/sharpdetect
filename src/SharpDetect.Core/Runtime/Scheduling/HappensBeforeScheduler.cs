using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Core.Runtime.Arguments;

namespace SharpDetect.Core.Runtime.Scheduling
{
    internal class HappensBeforeScheduler : SchedulerBase
    {
        protected readonly ShadowCLR ShadowCLR;
        protected readonly RuntimeEventsHub RuntimeEventsHub;

        public HappensBeforeScheduler(int processId, ShadowCLR shadowCLR, RuntimeEventsHub runtimeEventsHub, IDateTimeProvider dateTimeProvider)
            : base(processId, dateTimeProvider)
        {
            this.ShadowCLR = shadowCLR;
            this.RuntimeEventsHub = runtimeEventsHub;
        }

        #region PROFILING_NOTIFICATIONS
        public void Schedule_Heartbeat(EventInfo info)
        {
            FeedWatchdog();

            // Notify listeners
            RuntimeEventsHub.RaiseHeartbeat(ShadowCLR, info);
        }

        public void Schedule_ProfilerInitialized(EventInfo info)
        {
            // Create initial thread
            var newThread = Register(info.ThreadId);
            // Start new shadow executor
            newThread.Start();

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_ProfilerInitialized();
                ShadowCLR.Process_ThreadCreated(newThread);

                // Notify listeners
                RuntimeEventsHub.RaiseProfilerInitialized(ShadowCLR, info);
            }));
        }

        public void Schedule_ProfilerDestroyed(EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_ProfilerDestroyed();

                // Notify listeners
                RuntimeEventsHub.RaiseProfilerDestroyed(ShadowCLR, info);

                // Ensure scheduler exits properly
                Terminate();
            }));
        }

        public void Schedule_ModuleLoaded(UIntPtr moduleId, string path, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                var moduleInfo = new ModuleInfo(moduleId);
                ShadowCLR.Process_ModuleLoaded(moduleInfo, path);

                // Notify listeners
                RuntimeEventsHub.RaiseModuleLoaded(ShadowCLR, moduleInfo, path, info);
            }));
        }

        public void Schedule_TypeLoaded(TypeInfo typeInfo, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_TypeLoaded(typeInfo);

                // Notify listeners
                RuntimeEventsHub.RaiseTypeLoaded(ShadowCLR, typeInfo, info);
            }));
        }

        public void Schedule_JITCompilationStarted(FunctionInfo functionInfo, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_JITCompilationStarted(functionInfo);

                // Notify listeners
                RuntimeEventsHub.RaiseJITCompilationStarted(ShadowCLR, functionInfo, info);
            }));
        }

        public void Schedule_ThreadCreated(UIntPtr threadId, EventInfo info)
        {
            // Create new thread
            var newThread = Register(threadId);

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Start new shadow executor
                newThread.Start();

                // Update ShadowCLR state
                ShadowCLR.Process_ThreadCreated(newThread);

                // Notify listeners
                RuntimeEventsHub.RaiseThreadCreated(ShadowCLR, threadId, info);
            }));
        }

        public void Schedule_ThreadDestroyed(UIntPtr threadId, EventInfo info)
        {
            // Create new thread
            var destroyedThread = ThreadLookup[threadId];

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_ThreadDestroyed(destroyedThread);

                // Notify listeners
                RuntimeEventsHub.RaiseThreadDestroyed(ShadowCLR, threadId, info);

                // Ensure thread terminates correctly
                UnregisterThread(threadId);
                destroyedThread.Dispose();
            }));
        }
        
        public void Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON reason, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_RuntimeSuspendStarted(reason);

                // Notify listeners
                RuntimeEventsHub.RaiseRuntimeSuspendStarted(ShadowCLR, reason, info);
            }));
        }

        public void Schedule_RuntimeSuspendFinished(EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                if (ShadowCLR.SuspensionReason == COR_PRF_SUSPEND_REASON.GC)
                {
                    // Adjust epochs for native threads that were not suspended
                    var nextEpoch = EpochChangeSignaller.Epoch + 1;
                    foreach (var (_, thread) in ShadowCLR.Threads)
                    {
                        if (thread.Epoch < nextEpoch)
                            thread.EnterNewEpoch(nextEpoch);
                    }

                    // Scheduler starts new epoch
                    EpochChangeSignaller.Epoch++;
                }

                // Update ShadowCLR state
                ShadowCLR.Process_RuntimeSuspendFinished();

                // Notify listeners
                RuntimeEventsHub.RaiseRuntimeSuspendFinished(ShadowCLR, info);
            }));
        }

        public void Schedule_RuntimeThreadSuspended(UIntPtr threadId, EventInfo info)
        {
            var thread = ThreadLookup[threadId];

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_RuntimeThreadSuspended(thread);

                // Notify listeners
                RuntimeEventsHub.RaiseRuntimeThreadSuspended(ShadowCLR, threadId, info);
            }));
        }

        public void Schedule_RuntimeThreadResumed(UIntPtr threadId, EventInfo info)
        {
            var thread = ThreadLookup[threadId];

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_RuntimeThreadResumed(thread);

                // Notify listeners
                RuntimeEventsHub.RaiseRuntimeThreadResumed(ShadowCLR, threadId, info);
            }));
        }

        public void Schedule_GarbageCollectionStarted(bool[] generationsCollected, COR_PRF_GC_GENERATION_RANGE[] bounds, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_GarbageCollectionStarted(generationsCollected, bounds);

                // Notify listeners
                RuntimeEventsHub.RaiseGarbageCollectionStarted(ShadowCLR, generationsCollected, bounds, info);
            }));
        }

        public void Schedule_GarbageCollectionFinished(COR_PRF_GC_GENERATION_RANGE[] bounds, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Adjust native epochs for native threads that were not suspended
                var nextEpoch = EpochChangeSignaller.Epoch + 1;
                foreach (var (_, thread) in ShadowCLR.Threads)
                {
                    if (thread.Epoch < nextEpoch)
                        thread.EnterNewEpoch(nextEpoch);
                }

                // Scheduler starts new epoch
                EpochChangeSignaller.Epoch++;

                // Update ShadowCLR state
                ShadowCLR.Process_GarbageCollectionFinished(bounds);

                // Notify listeners
                RuntimeEventsHub.RaiseGarbageCollectionFinished(ShadowCLR, bounds, info);

                // TODO: continue execution
                throw new NotSupportedException();
            }));
        }

        public void Schedule_SurvivingReferences(UIntPtr[] blockStarts, UIntPtr[] lengths, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_SurvivingReferences(blockStarts, lengths);

                // Notify listeners
                RuntimeEventsHub.RaiseSurvivingReferences(ShadowCLR, blockStarts, lengths, info);
            }));
        }

        public void Schedule_MovedReferences(UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, UIntPtr[] lengths, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_MovedReferences(oldBlockStarts, newBlockStarts, lengths);

                // Notify listeners
                RuntimeEventsHub.RaiseMovedReferences(ShadowCLR, oldBlockStarts, newBlockStarts, lengths, info);
            }));
        }
        #endregion

        #region REWRITING_NOTIFICATIONS

        public void Schedule_TypeInjected(TypeInfo type, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_TypeInjected(type);

                // Notify listeners
                RuntimeEventsHub.RaiseTypeInjected(ShadowCLR, type, info);
            }));
        }

        public void Schedule_MethodInjected(FunctionInfo functionInfo, MethodType type, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_MethodInjected(functionInfo, type);

                // Notify listeners
                RuntimeEventsHub.RaiseMethodInjected(ShadowCLR, functionInfo, type, info);
            }));
        }

        public void Schedule_TypeReferenced(TypeInfo typeInfo, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_TypeReferenced(typeInfo);

                // Notify listeners
                RuntimeEventsHub.RaiseTypeReferenced(ShadowCLR, typeInfo, info);
            }));
        }

        public void Schedule_WrapperInjected(FunctionInfo functionInfo, MDToken wrapperToken, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_MethodWrapped(functionInfo, wrapperToken);

                // Notify listeners
                RuntimeEventsHub.RaiseMethodWrapperInjected(ShadowCLR, functionInfo, wrapperToken, info);
            }));
        }

        public void Schedule_WrapperReferenced(FunctionInfo functionDef, FunctionInfo functionRef, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_WrapperMethodReferenced(functionDef, functionRef);

                // Notify listeners
                RuntimeEventsHub.RaiseWrapperMethodReferenced(ShadowCLR, functionDef, functionRef, info);
            }));
        }

        public void Schedule_HelperReferenced(FunctionInfo functionRef, MethodType type, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_HelperMethodReferenced(functionRef, type);

                // Notify listeners
                RuntimeEventsHub.RaiseHelperMethodReferenced(ShadowCLR, functionRef, type, info);
            }));
        }

        #endregion

        #region EXECUTING_NOTIFICATIONS
        public void Schedule_ArrayElementAccessed(ulong identifier, bool isWrite, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                Guard.NotNull<ShadowObject, ShadowRuntimeStateException>(thread.OperationContext.ArrayInstance);
                Guard.NotNull<int?, ShadowRuntimeStateException>(thread.OperationContext.ArrayIndex);
                var array = thread.OperationContext.ArrayInstance!;
                var index = thread.OperationContext.ArrayIndex!.Value;

                // Notify listeners
                RuntimeEventsHub.RaiseArrayElementAccessed(ShadowCLR, identifier, isWrite, array, index, info);
            }));
        }

        public void Schedule_ArrayInstanceAccessed(UIntPtr instancePtr, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var instance = ShadowCLR.ShadowGC.GetObject(instancePtr);
                var thread = ThreadLookup[info.ThreadId];
                thread.OperationContext.ArrayInstance = instance;

                // Notify listeners
                RuntimeEventsHub.RaiseArrayInstanceAccessed(ShadowCLR, instance, info);
            }));
        }

        public void Schedule_ArrayIndexAccessed(int index, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                thread.OperationContext.ArrayIndex = index;

                // Notify listeners
                RuntimeEventsHub.RaiseArrayIndexAccessed(ShadowCLR, index, info);
            }));
        }

        public void Schedule_FieldAccessed(ulong identifier, bool isWrite, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                Guard.NotNull<ShadowObject, ShadowRuntimeStateException>(thread.OperationContext.FieldInstance);
                var instance = thread.OperationContext.FieldInstance!;

                // Notify listeners
                RuntimeEventsHub.RaiseFieldAccessed(ShadowCLR, identifier, isWrite, instance, info);
            }));
        }

        public void Schedule_FieldInstanceAccessed(UIntPtr instancePtr, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var instance = ShadowCLR.ShadowGC.GetObject(instancePtr);
                var thread = ThreadLookup[info.ThreadId];
                thread.OperationContext.FieldInstance = instance;

                // Notify listeners
                RuntimeEventsHub.RaiseFieldInstanceAccessed(ShadowCLR, instance, info);
            }));
        }

        public void Schedule_LockAcquireAttempted(FunctionInfo function, UIntPtr instancePtr, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.SynchronizedBlocking, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = ShadowCLR.ShadowGC.GetObject(instancePtr);
                thread.PushCallStack(function, MethodInterpretation.LockTryAcquire, instance);

                // Notify listeners
                RuntimeEventsHub.RaiseLockAcquireAttempted(ShadowCLR, function, instance, info);
            }));
        }

        public void Schedule_LockAcquireReturned(FunctionInfo function, bool isSuccess, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.SynchronizedUnblocking, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = thread.PeekCallstack().Arguments as ShadowObject;
                if (isSuccess)
                    instance!.SyncBlock.Acquire(thread);
                thread.PopCallStack();

                // Notify listeners
                Guard.NotNull<ShadowObject, ArgumentException>(instance);
                RuntimeEventsHub.RaiseLockAcquireReturned(ShadowCLR, function, instance, isSuccess, info);
            }));
        }

        public void Schedule_LockReleaseCalled(FunctionInfo function, UIntPtr instancePtr, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = ShadowCLR.ShadowGC.GetObject(instancePtr);
                thread.PushCallStack(function, MethodInterpretation.LockRelease, instance);

                // Notify listeners
                RuntimeEventsHub.RaiseLockReleaseReturned(ShadowCLR, function, instance, info);
            }));
        }

        public void Schedule_LockReleaseReturned(FunctionInfo function, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = thread.PeekCallstack().Arguments as ShadowObject;
                instance!.SyncBlock.Release(thread);
                thread.PopCallStack();

                // Notify listeners
                RuntimeEventsHub.RaiseLockReleaseReturned(ShadowCLR, function, instance, info);
            }));
        }

        public void Schedule_MethodCalled(FunctionInfo function, (ushort Index, IValueOrPointer Arg)[]? arguments, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Resolve arguments
                var resolvedArgumentsList = default(ArgumentsList);
                if (arguments is not null)
                {
                    var resolvedArguments = new (ushort, IValueOrObject)[arguments.Length];
                    for (var i = 0; i < arguments.Length; i++)
                    {
                        var argValue = (arguments[i].Arg.HasValue()) ? new ValueOrObject(arguments[i].Arg.BoxedValue!) : 
                            new ValueOrObject(ShadowCLR.ShadowGC.GetObject(arguments[i].Arg.Pointer!.Value));
                        resolvedArguments[i] = (arguments[i].Index, argValue);
                    }

                    resolvedArgumentsList = new(resolvedArguments);
                }

                // Notify listeners
                RuntimeEventsHub.RaiseMethodCalled(ShadowCLR, function, resolvedArgumentsList, info);
            }));
        }

        public void Schedule_MethodReturned(FunctionInfo function, IValueOrPointer? retValue, (ushort Index, IValueOrPointer Arg)[]? byRefArgs, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Resolve return value
                var resolvedReturnValue = default(IValueOrObject);
                if (retValue is not null)
                {
                    resolvedReturnValue = (retValue.HasValue()) ? new ValueOrObject(retValue.BoxedValue!) :
                        new ValueOrObject(ShadowCLR.ShadowGC.GetObject(retValue.Pointer!.Value));
                }

                // Resolve arguments
                var resolvedByRefArgumentsList = default(ArgumentsList);
                if (byRefArgs is not null)
                {
                    var resolvedByRefArguments = new (ushort, IValueOrObject)[byRefArgs.Length];
                    for (var i = 0; i < byRefArgs.Length; i++)
                    {
                        var argValue = (byRefArgs[i].Arg.HasValue()) ? new ValueOrObject(byRefArgs[i].Arg.BoxedValue!) :
                            new ValueOrObject(ShadowCLR.ShadowGC.GetObject(byRefArgs[i].Arg.Pointer!.Value));
                        resolvedByRefArguments[i] = (byRefArgs[i].Index, argValue);
                    }

                    resolvedByRefArgumentsList = new(resolvedByRefArguments);
                }

                // Notify listeners
                RuntimeEventsHub.RaiseMethodReturned(ShadowCLR, function, resolvedReturnValue, resolvedByRefArgumentsList, info);
            }));
        }

        public void Schedule_ObjectWaitAttempted(FunctionInfo function, UIntPtr instancePtr, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.SynchronizedBlocking, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = ShadowCLR.ShadowGC.GetObject(instancePtr);
                var isWaiting = (thread.GetCallstackDepth() != 0) && thread.PeekCallstack().Interpretation == MethodInterpretation.SignalTryWait;
                thread.PushCallStack(function, MethodInterpretation.SignalTryWait, instance);

                if (isWaiting)
                {
                    // We already processed this event
                    return;
                }

                // Release lock
                instance.SyncBlock.Release(thread);

                // Notify listeners
                RuntimeEventsHub.RaiseObjectWaitAttempted(ShadowCLR, function, instance, info);
            }));
        }

        public void Schedule_ObjectWaitReturned(FunctionInfo function, bool isSuccess, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.SynchronizedUnblocking, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = thread.PopCallStack().Arguments as ShadowObject;
                var isStillWaiting = (thread.GetCallstackDepth() != 0) && thread.PeekCallstack().Interpretation == MethodInterpretation.SignalTryWait;
                thread.PushCallStack(function, MethodInterpretation.SignalTryWait, instance);

                if (isStillWaiting)
                {
                    // This event will be processed for the first Wait overload
                    return;
                }

                // Re-acquire lock
                instance!.SyncBlock.Acquire(thread);

                // Notify listeners
                RuntimeEventsHub.RaiseObjectWaitReturned(ShadowCLR, function, instance, isSuccess, info);
            }));
        }

        public void Schedule_ObjectPulseCalled(FunctionInfo function, bool isPulseAll, UIntPtr instancePtr, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = ShadowCLR.ShadowGC.GetObject(instancePtr);
                var interpretation = (isPulseAll) ? MethodInterpretation.SignalPulseAll : MethodInterpretation.SignalPulseOne;
                thread.PushCallStack(function, interpretation, instance);

                // Notify listeners
                RuntimeEventsHub.RaiseObjectPulseCalled(ShadowCLR, function, isPulseAll, instance, info);
            }));
        }

        public void Schedule_ObjectPulseReturned(FunctionInfo function, bool isPulseAll, EventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];
                var instance = thread.PeekCallstack().Arguments as ShadowObject;
                instance!.SyncBlock.Release(thread);
                thread.PopCallStack();

                // Notify listeners
                RuntimeEventsHub.RaiseObjectPulseReturned(ShadowCLR, function, isPulseAll, instance, info);
            }));
        }
        #endregion
    }
}
