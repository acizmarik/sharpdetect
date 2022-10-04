using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Interop;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Runtime.Arguments;
using SharpDetect.Core.Utilities;

namespace SharpDetect.Core.Runtime.Scheduling
{
    internal class HappensBeforeScheduler : SchedulerBase
    {
        protected readonly ShadowCLR ShadowCLR;
        protected readonly RuntimeEventsHub RuntimeEventsHub;
        protected readonly IMetadataContext MetadataContext;
        protected readonly IMethodDescriptorRegistry MethodRegistry;
        protected readonly IProfilingClient ProfilingClient;

        public HappensBeforeScheduler(
            int processId, 
            ShadowCLR shadowCLR, 
            RuntimeEventsHub runtimeEventsHub, 
            IMethodDescriptorRegistry methodRegistry, 
            IMetadataContext metadataContext, 
            IProfilingClient profilingClient, 
            IDateTimeProvider dateTimeProvider)
            : base(processId, dateTimeProvider)
        {
            this.ShadowCLR = shadowCLR;
            this.RuntimeEventsHub = runtimeEventsHub;
            this.MetadataContext = metadataContext;
            this.MethodRegistry = methodRegistry;
            this.ProfilingClient = profilingClient;
        }

        #region PROFILING_NOTIFICATIONS
        public void Schedule_Heartbeat(RawEventInfo info)
        {
            FeedWatchdog();

            // Notify listeners
            RuntimeEventsHub.RaiseHeartbeat(ShadowCLR, info);
        }

        public void Schedule_ProfilerInitialized(Version? _, RawEventInfo info)
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

        public void Schedule_ProfilerDestroyed(RawEventInfo info)
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

        public void Schedule_ModuleLoaded(UIntPtr moduleId, string path, RawEventInfo info)
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

        public void Schedule_TypeLoaded(TypeInfo typeInfo, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_TypeLoaded(typeInfo);

                // Notify listeners
                RuntimeEventsHub.RaiseTypeLoaded(ShadowCLR, typeInfo, info);
            }));
        }

        public void Schedule_JITCompilationStarted(FunctionInfo functionInfo, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_JITCompilationStarted(functionInfo);

                // Notify listeners
                RuntimeEventsHub.RaiseJITCompilationStarted(ShadowCLR, functionInfo, info);
            }));
        }

        public void Schedule_ThreadCreated(UIntPtr threadId, RawEventInfo info)
        {
            // Create new thread
            var newThread = Register(threadId);

            // Start new shadow executor
            newThread.Start();

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_ThreadCreated(newThread);

                // Notify listeners
                RuntimeEventsHub.RaiseThreadCreated(ShadowCLR, threadId, info);
            }));
        }

        public void Schedule_ThreadDestroyed(UIntPtr threadId, RawEventInfo info)
        {
            // Create new thread
            var destroyedThread = ThreadLookup[threadId];

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Notify listeners
                RuntimeEventsHub.RaiseThreadDestroyed(ShadowCLR, threadId, info);

                // Update ShadowCLR state
                ShadowCLR.Process_ThreadDestroyed(destroyedThread);

                // Ensure thread terminates correctly
                UnregisterThread(threadId);
            }));
        }
        
        public void Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON reason, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_RuntimeSuspendStarted(reason);

                // Notify listeners
                RuntimeEventsHub.RaiseRuntimeSuspendStarted(ShadowCLR, reason, info);
            }));
        }

        public void Schedule_RuntimeSuspendFinished(RawEventInfo info)
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

        public void Schedule_RuntimeResumeStarted(RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_RuntimeResumeStarted();

                // Notify listeners
                RuntimeEventsHub.RaiseRuntimeResumeStarted(ShadowCLR, info);
            }));
        }

        public void Schedule_RuntimeResumeFinished(RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_RuntimeResumeFinished();

                // Notify listeners
                RuntimeEventsHub.RaiseRuntimeResumeFinished(ShadowCLR, info);
            }));
        }

        public void Schedule_RuntimeThreadSuspended(UIntPtr threadId, RawEventInfo info)
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

        public void Schedule_RuntimeThreadResumed(UIntPtr threadId, RawEventInfo info)
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

        public void Schedule_GarbageCollectionStarted(bool[] generationsCollected, COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_GarbageCollectionStarted(generationsCollected, bounds);

                // Notify listeners
                RuntimeEventsHub.RaiseGarbageCollectionStarted(ShadowCLR, generationsCollected, bounds, info);
            }));
        }

        public void Schedule_GarbageCollectionFinished(COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
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

                // Continue execution
                ProfilingClient.IssueContinueExecutionRequestAsync(info).Wait();
            }));
        }

        public void Schedule_SurvivingReferences(UIntPtr[] blockStarts, UIntPtr[] lengths, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_SurvivingReferences(blockStarts, lengths);

                // Notify listeners
                RuntimeEventsHub.RaiseSurvivingReferences(ShadowCLR, blockStarts, lengths, info);
            }));
        }

        public void Schedule_MovedReferences(UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, UIntPtr[] lengths, RawEventInfo info)
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

        public void Schedule_TypeInjected(TypeInfo type, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_TypeInjected(type);

                // Notify listeners
                RuntimeEventsHub.RaiseTypeInjected(ShadowCLR, type, info);
            }));
        }

        public void Schedule_MethodInjected(FunctionInfo functionInfo, MethodType type, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_MethodInjected(functionInfo, type);

                // Notify listeners
                RuntimeEventsHub.RaiseMethodInjected(ShadowCLR, functionInfo, type, info);
            }));
        }

        public void Schedule_TypeReferenced(TypeInfo typeInfo, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_TypeReferenced(typeInfo);

                // Notify listeners
                RuntimeEventsHub.RaiseTypeReferenced(ShadowCLR, typeInfo, info);
            }));
        }

        public void Schedule_WrapperInjected(FunctionInfo functionInfo, MDToken wrapperToken, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_MethodWrapped(functionInfo, wrapperToken);

                // Notify listeners
                RuntimeEventsHub.RaiseMethodWrapperInjected(ShadowCLR, functionInfo, wrapperToken, info);
            }));
        }

        public void Schedule_WrapperReferenced(FunctionInfo functionDef, FunctionInfo functionRef, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                // Update ShadowCLR state
                ShadowCLR.Process_WrapperMethodReferenced(functionDef, functionRef);

                // Notify listeners
                RuntimeEventsHub.RaiseWrapperMethodReferenced(ShadowCLR, functionDef, functionRef, info);
            }));
        }

        public void Schedule_HelperReferenced(FunctionInfo functionRef, MethodType type, RawEventInfo info)
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
        public void Schedule_MethodCalled(FunctionInfo function, RawArgumentsList? arguments, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];

                // Ensure we can resolve the method
                var resolvedArgumentsList = default(ArgumentsList);
                var resolver = MetadataContext.GetResolver(ProcessId);
                if (resolver.TryGetMethodDef(function, new(function.ModuleId), resolveWrappers: true, out var methodDef))
                {
                    // Parse method call arguments
                    var parsedArguments = (arguments.HasValue && !arguments.Value.ArgValues.IsEmpty) ?
                        ArgumentsHelper.ParseArguments(methodDef, arguments.Value.ArgValues.Span, arguments.Value.ArgOffsets.Span) : null;

                    // Resolve arguments
                    if (parsedArguments is not null)
                    {
                        var resolvedArguments = new (ushort, IValueOrObject)[parsedArguments.Length];
                        for (var i = 0; i < parsedArguments.Length; i++)
                        {
                            var argValue = (parsedArguments[i].Argument.HasValue()) ? new ValueOrObject(parsedArguments[i].Argument.BoxedValue!) :
                                new ValueOrObject(ShadowCLR.ShadowGC.GetObject(parsedArguments[i].Argument.Pointer!.Value));
                            resolvedArguments[i] = (parsedArguments[i].Index, argValue);
                        }

                        resolvedArgumentsList = new(resolvedArguments);
                    }

                    // Resolve method interpretation
                    if (MethodRegistry.TryGetMethodInterpretationData(methodDef, out var interpretationData))
                    {
                        // Raise more specific events (based on the method interpretation)
                        switch (interpretationData.Interpretation)
                        {
                            // Lock acquire calls
                            case MethodInterpretation.LockTryAcquire:
                            case MethodInterpretation.LockBlockingAcquire:
                                {
                                    var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                    Guard.NotNull<IShadowObject, ShadowRuntimeStateException>(instance);
                                    thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                    RuntimeEventsHub.RaiseLockAcquireAttempted(ShadowCLR, function, instance, info);
                                    break;
                                }
                            // Lock release calls
                            case MethodInterpretation.LockRelease:
                                {
                                    var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                    Guard.NotNull<IShadowObject, ShadowRuntimeStateException>(instance);
                                    thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                    RuntimeEventsHub.RaiseLockReleaseCalled(ShadowCLR, function, instance, info);
                                    break;
                                }
                            // Signal wait calls
                            case MethodInterpretation.SignalTryWait:
                            case MethodInterpretation.SignalBlockingWait:
                                {
                                    var instance = resolvedArgumentsList[0].Argument.ShadowObject as ShadowObject;
                                    Guard.NotNull<IShadowObject, ShadowRuntimeStateException>(instance);
                                    var isAlreadyWaiting = (thread.GetCallstackDepth() != 0)
                                        && (thread.PeekCallstack().Interpretation == MethodInterpretation.SignalTryWait ||
                                            thread.PeekCallstack().Interpretation == MethodInterpretation.SignalBlockingWait);
                                    thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                    if (!isAlreadyWaiting)
                                        instance.SyncBlock.Release(thread);
                                    RuntimeEventsHub.RaiseObjectWaitAttempted(ShadowCLR, function, instance, info);
                                    break;
                                }
                            // Signal pulse calls
                            case MethodInterpretation.SignalPulseOne:
                            case MethodInterpretation.SignalPulseAll:
                                {
                                    var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                    Guard.NotNull<IShadowObject, ShadowRuntimeStateException>(instance);
                                    thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                    var isPulseAll = interpretationData.Interpretation == MethodInterpretation.SignalPulseAll;
                                    RuntimeEventsHub.RaiseObjectPulseCalled(ShadowCLR, function, isPulseAll, instance, info);
                                    break;
                                }
                            // Fields
                            case MethodInterpretation.FieldAccess:
                                {
                                    var isWrite = (bool)resolvedArgumentsList[0].Argument.BoxedValue!;
                                    var identifier = (ulong)resolvedArgumentsList[1].Argument.BoxedValue!;
                                    var fieldInstance = thread.OperationContext.GetAndResetLastFieldInstance();
                                    RuntimeEventsHub.RaiseFieldAccessed(ShadowCLR, identifier, isWrite, fieldInstance, info);
                                    break;
                                }
                            case MethodInterpretation.FieldInstanceAccess:
                                {
                                    var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                    Guard.NotNull<IShadowObject, ShadowRuntimeStateException>(instance);
                                    thread.OperationContext.SetFieldInstance(instance as ShadowObject);
                                    RuntimeEventsHub.RaiseFieldInstanceAccessed(ShadowCLR, instance, info);
                                    break;
                                }
                            // Arrays
                            case MethodInterpretation.ArrayElementAccess:
                                {
                                    var isWrite = (bool)resolvedArgumentsList[0].Argument.BoxedValue!;
                                    var identifier = (ulong)resolvedArgumentsList[1].Argument.BoxedValue!;
                                    var arrayInstance = thread.OperationContext.GetAndResetLastArrayInstance();
                                    var arrayIndex = thread.OperationContext.GetAndResetLastArrayIndex();
                                    Guard.NotNull<ShadowObject, ShadowRuntimeStateException>(arrayInstance);
                                    Guard.NotNull<int?, ShadowRuntimeStateException>(arrayIndex);
                                    RuntimeEventsHub.RaiseArrayElementAccessed(ShadowCLR, identifier, isWrite, arrayInstance, arrayIndex.Value, info);
                                    break;
                                }
                            case MethodInterpretation.ArrayInstanceAccess:
                                {
                                    var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                    Guard.NotNull<IShadowObject, ShadowRuntimeStateException>(instance);
                                    thread.OperationContext.SetArrayInstance(instance as ShadowObject);
                                    RuntimeEventsHub.RaiseArrayInstanceAccessed(ShadowCLR, instance, info);
                                    break;
                                }
                            case MethodInterpretation.ArrayIndexAccess:
                                {
                                    var index = (int)resolvedArgumentsList[0].Argument.BoxedValue!;
                                    thread.OperationContext.SetArrayIndex(index);
                                    RuntimeEventsHub.RaiseArrayIndexAccessed(ShadowCLR, index, info);
                                    break;
                                }
                        }
                    }
                }

                RuntimeEventsHub.RaiseMethodCalled(ShadowCLR, function, resolvedArgumentsList, info);
            }));
        }

        public void Schedule_MethodReturned(FunctionInfo function, RawReturnValue? retValue, RawArgumentsList? byRefArgs, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, new Task(() =>
            {
                var thread = ThreadLookup[info.ThreadId];

                // Ensure we can resolve the method
                var resolvedByRefArgumentsList = default(ArgumentsList);
                var resolvedReturnValue = default(IValueOrObject);
                var resolver = MetadataContext.GetResolver(ProcessId);
                if (resolver.TryGetMethodDef(function, new(function.ModuleId), resolveWrappers: true, out var methodDef))
                {
                    // Parse method call arguments
                    var parsedByRefArguments = (byRefArgs.HasValue && !byRefArgs.Value.ArgValues.IsEmpty) ?
                        ArgumentsHelper.ParseArguments(
                            methodDef,
                            byRefArgs.Value.ArgValues.Span,
                            byRefArgs.Value.ArgOffsets.Span) :
                        Array.Empty<(ushort Index, IValueOrPointer Argument)>();

                    // Parse return value
                    var parsedReturnValue = (retValue.HasValue && !retValue.Value.ReturnValue.IsEmpty) ?
                        ArgumentsHelper.ParseArgument(methodDef.ReturnType, retValue.Value.ReturnValue.Span) : null as IValueOrPointer;

                    // Resolve return value
                    if (parsedReturnValue is not null)
                    {
                        resolvedReturnValue = (parsedReturnValue.HasValue()) ? new ValueOrObject(parsedReturnValue.BoxedValue!) :
                            new ValueOrObject(ShadowCLR.ShadowGC.GetObject(parsedReturnValue.Pointer!.Value));
                    }

                    // Resolve arguments
                    if (parsedByRefArguments is not null)
                    {
                        var resolvedByRefArguments = new (ushort, IValueOrObject)[parsedByRefArguments.Length];
                        for (var i = 0; i < parsedByRefArguments.Length; i++)
                        {
                            var argValue = (parsedByRefArguments[i].Argument.HasValue()) ? new ValueOrObject(parsedByRefArguments[i].Argument.BoxedValue!) :
                                new ValueOrObject(ShadowCLR.ShadowGC.GetObject(parsedByRefArguments[i].Argument.Pointer!.Value));
                            resolvedByRefArguments[i] = (parsedByRefArguments[i].Index, argValue);
                        }

                        resolvedByRefArgumentsList = new(resolvedByRefArguments);
                    }

                    // Resolve method interpretation
                    if (MethodRegistry.TryGetMethodInterpretationData(methodDef, out var interpretationData))
                    {
                        switch (interpretationData.Interpretation)
                        {
                            // Lock acquire returns
                            case MethodInterpretation.LockTryAcquire:
                            case MethodInterpretation.LockBlockingAcquire:
                                {
                                    Guard.NotNull<ResultChecker, ArgumentException>(interpretationData.Checker);
                                    Guard.NotEqual<int, ShadowRuntimeStateException>(0, thread.GetCallstackDepth());
                                    var instance = thread.PopCallStack().Arguments as ShadowObject;
                                    Guard.NotNull<ShadowObject, ShadowRuntimeStateException>(instance);
                                    var isSuccess = interpretationData.Checker(resolvedReturnValue, resolvedByRefArgumentsList.Raw);
                                    if (isSuccess)
                                        instance.SyncBlock.Acquire(thread);
                                    RuntimeEventsHub.RaiseLockAcquireReturned(ShadowCLR, function, instance, isSuccess, info);
                                    break;
                                }
                            // Lock release returns
                            case MethodInterpretation.LockRelease:
                                {
                                    Guard.NotEqual<int, ShadowRuntimeStateException>(0, thread.GetCallstackDepth());
                                    var instance = thread.PopCallStack().Arguments as ShadowObject;
                                    Guard.NotNull<ShadowObject, ShadowRuntimeStateException>(instance);
                                    instance.SyncBlock.Release(thread);
                                    RuntimeEventsHub.RaiseLockReleaseReturned(ShadowCLR, function, instance, info);
                                    break;
                                }
                            // Signal wait returns
                            case MethodInterpretation.SignalTryWait:
                            case MethodInterpretation.SignalBlockingWait:
                                {
                                    Guard.NotNull<ResultChecker, ArgumentException>(interpretationData.Checker);
                                    Guard.NotEqual<int, ShadowRuntimeStateException>(0, thread.GetCallstackDepth());
                                    var instance = thread.PopCallStack().Arguments as ShadowObject;
                                    var isStillWaiting = (thread.GetCallstackDepth() != 0) && thread.PeekCallstack().Interpretation == MethodInterpretation.SignalTryWait;
                                    if (isStillWaiting)
                                    {
                                        // This event will be processed for the first Wait overload
                                        break;
                                    }
                                    instance!.SyncBlock.Acquire(thread);
                                    var isSuccess = interpretationData.Checker(resolvedReturnValue, resolvedByRefArgumentsList.Raw);
                                    RuntimeEventsHub.RaiseObjectWaitReturned(ShadowCLR, function, instance, isSuccess, info);
                                    break;
                                }
                            // Signal pulse returns
                            case MethodInterpretation.SignalPulseOne:
                            case MethodInterpretation.SignalPulseAll:
                                {
                                    Guard.NotEqual<int, ShadowRuntimeStateException>(0, thread.GetCallstackDepth());
                                    var instance = thread.PopCallStack().Arguments as ShadowObject;
                                    Guard.NotNull<ShadowObject, ShadowRuntimeStateException>(instance);
                                    var isPulseAll = interpretationData.Interpretation == MethodInterpretation.SignalPulseAll;
                                    RuntimeEventsHub.RaiseObjectPulseReturned(ShadowCLR, function, isPulseAll, instance, info);
                                    break;
                                }
                        }
                    }
                }

                RuntimeEventsHub.RaiseMethodReturned(ShadowCLR, function, resolvedReturnValue, resolvedByRefArgumentsList, info);
            }));
        }
        #endregion
    }
}
