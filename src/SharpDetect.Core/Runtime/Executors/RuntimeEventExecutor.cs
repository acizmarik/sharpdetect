using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Descriptors;
using SharpDetect.Common.Services.Endpoints;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Runtime.Arguments;
using SharpDetect.Core.Runtime.Threads;
using SharpDetect.Core.Utilities;

namespace SharpDetect.Core.Runtime.Executors
{
    internal class RuntimeEventExecutor
    {
        public readonly int ProcessId;
        private readonly ShadowCLR runtime;
        private readonly RuntimeEventsHub eventsHub;
        private readonly IMetadataContext metadataContext;
        private readonly IProfilingClient profilingClient;
        private readonly IMethodDescriptorRegistry methodRegistry;

        public RuntimeEventExecutor(
            int processId,
            ShadowCLR runtime,
            RuntimeEventsHub eventsHub,
            IMetadataContext metadataContext,
            IProfilingClient profilingClient,
            IMethodDescriptorRegistry methodRegistry)
        {
            this.ProcessId = processId;
            this.runtime = runtime;
            this.eventsHub = eventsHub;
            this.metadataContext = metadataContext;
            this.profilingClient = profilingClient;
            this.methodRegistry = methodRegistry;
        }

        public void ExecuteProfilerInitialized(ShadowThread shadowThread, RawEventInfo info)
        {
            runtime.Process_ProfilerInitialized();
            runtime.Process_ThreadCreated(shadowThread);
            eventsHub.RaiseProfilerInitialized(runtime, info);
        }

        public void ExecuteProfilerDestroyed(RawEventInfo info)
        {
            runtime.Process_ProfilerDestroyed();
            eventsHub.RaiseProfilerDestroyed(runtime, info);
        }

        public void ExecuteModuleLoaded(UIntPtr moduleId, string path, RawEventInfo info)
        {
            var moduleInfo = new ModuleInfo(moduleId);
            runtime.Process_ModuleLoaded(moduleInfo, path);
            eventsHub.RaiseModuleLoaded(runtime, moduleInfo, path, info);
        }

        public void ExecuteTypeLoaded(TypeInfo typeInfo, RawEventInfo info)
        {
            runtime.Process_TypeLoaded(typeInfo);
            eventsHub.RaiseTypeLoaded(runtime, typeInfo, info);
        }

        public void ExecuteJITCompilationStarted(FunctionInfo functionInfo, RawEventInfo info)
        {
            runtime.Process_JITCompilationStarted(functionInfo);
            eventsHub.RaiseJITCompilationStarted(runtime, functionInfo, info);
        }

        public void ExecuteThreadCreated(ShadowThread shadowThread, RawEventInfo info)
        {
            runtime.Process_ThreadCreated(shadowThread);
            eventsHub.RaiseThreadCreated(runtime, shadowThread.Id, info);
        }

        public void ExecuteThreadDestroyed(ShadowThread shadowThread, RawEventInfo info)
        {
            eventsHub.RaiseThreadDestroyed(runtime, shadowThread.Id, info);
            runtime.Process_ThreadDestroyed(shadowThread);
        }

        public void ExecuteRuntimeSuspendStarted(COR_PRF_SUSPEND_REASON reason, RawEventInfo info)
        {
            runtime.Process_RuntimeSuspendStarted(reason);
            eventsHub.RaiseRuntimeSuspendStarted(runtime, reason, info);
        }

        public void ExecuteRuntimeSuspendFinished(RawEventInfo info)
        {
            runtime.Process_RuntimeSuspendFinished();
            eventsHub.RaiseRuntimeSuspendFinished(runtime, info);
        }

        public void ExecuteRuntimeResumeStarted(RawEventInfo info)
        {
            runtime.Process_RuntimeResumeStarted();
            eventsHub.RaiseRuntimeResumeStarted(runtime, info);
        }

        public void ExecuteRuntimeResumeFinished(RawEventInfo info)
        {
            runtime.Process_RuntimeResumeFinished();
            eventsHub.RaiseRuntimeResumeFinished(runtime, info);
        }

        public void ExecuteRuntimeThreadSuspended(ShadowThread shadowThread, RawEventInfo info)
        {
            runtime.Process_RuntimeThreadSuspended(shadowThread);
            eventsHub.RaiseRuntimeThreadSuspended(runtime, shadowThread.Id, info);
        }

        public void ExecuteRuntimeThreadResumed(ShadowThread shadowThread, RawEventInfo info)
        {
            runtime.Process_RuntimeThreadResumed(shadowThread);
            eventsHub.RaiseRuntimeThreadResumed(runtime, shadowThread.Id, info);
        }

        public void ExecuteGarbageCollectionStarted(bool[] generationsCollected, COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
        {
            runtime.Process_GarbageCollectionStarted(generationsCollected, bounds);
            eventsHub.RaiseGarbageCollectionStarted(runtime, generationsCollected, bounds, info);
        }

        public void ExecuteGarbageCollectionFinished(COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
        {
            runtime.Process_GarbageCollectionFinished(bounds);
            eventsHub.RaiseGarbageCollectionFinished(runtime, bounds, info);
            profilingClient.IssueContinueExecutionRequestAsync(info);
        }

        public void ExecuteSurvivingReferences(UIntPtr[] blockStarts, uint[] lengths, RawEventInfo info)
        {
            runtime.Process_SurvivingReferences(blockStarts, lengths);
            eventsHub.RaiseSurvivingReferences(runtime, blockStarts, lengths, info);
        }

        public void ExecuteMovedReferences(UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, uint[] lengths, RawEventInfo info)
        {
            runtime.Process_MovedReferences(oldBlockStarts, newBlockStarts, lengths);
            eventsHub.RaiseMovedReferences(runtime, oldBlockStarts, newBlockStarts, lengths, info);
        }

        public void ExecuteTypeInjected(TypeInfo type, RawEventInfo info)
        {
            runtime.Process_TypeInjected(type);
            eventsHub.RaiseTypeInjected(runtime, type, info);
        }

        public void ExecuteMethodInjected(FunctionInfo functionInfo, MethodType type, RawEventInfo info)
        {
            runtime.Process_MethodInjected(functionInfo, type);
            eventsHub.RaiseMethodInjected(runtime, functionInfo, type, info);
        }

        public void ExecuteTypeReferenced(TypeInfo typeInfo, RawEventInfo info)
        {
            runtime.Process_TypeReferenced(typeInfo);
            eventsHub.RaiseTypeReferenced(runtime, typeInfo, info);
        }

        public void ExecuteWrapperInjected(FunctionInfo functionInfo, MDToken wrapperToken, RawEventInfo info)
        {
            runtime.Process_MethodWrapped(functionInfo, wrapperToken);
            eventsHub.RaiseMethodWrapperInjected(runtime, functionInfo, wrapperToken, info);
        }

        public void ExecuteWrapperReferenced(FunctionInfo functionDef, FunctionInfo functionRef, RawEventInfo info)
        {
            runtime.Process_WrapperMethodReferenced(functionDef, functionRef);
            eventsHub.RaiseWrapperMethodReferenced(runtime, functionDef, functionRef, info);
        }

        public void ExecuteHelperReferenced(FunctionInfo functionRef, MethodType type, RawEventInfo info)
        {
            runtime.Process_HelperMethodReferenced(functionRef, type);
            eventsHub.RaiseHelperMethodReferenced(runtime, functionRef, type, info);
        }

        public void ExecuteMethodCalled(ShadowThread thread, FunctionInfo function, RawArgumentsList? arguments, RawEventInfo info)
        {
            // Ensure we can resolve the method
            var resolvedArgumentsList = default(ArgumentsList);
            var resolver = metadataContext.GetResolver(ProcessId);
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
                            new ValueOrObject(runtime.ShadowGC.GetObject(parsedArguments[i].Argument.Pointer!.Value));
                        resolvedArguments[i] = (parsedArguments[i].Index, argValue);
                    }

                    resolvedArgumentsList = new(resolvedArguments);
                }

                // Resolve method interpretation
                if (methodRegistry.TryGetMethodInterpretationData(methodDef, out var interpretationData))
                {
                    // Raise more specific events (based on the method interpretation)
                    switch (interpretationData.Interpretation)
                    {
                        // Lock acquire calls
                        case MethodInterpretation.LockTryAcquire:
                        case MethodInterpretation.LockBlockingAcquire:
                            {
                                var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                eventsHub.RaiseLockAcquireAttempted(runtime, function, instance, info);
                                break;
                            }
                        // Lock release calls
                        case MethodInterpretation.LockRelease:
                            {
                                var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                eventsHub.RaiseLockReleaseCalled(runtime, function, instance, info);
                                break;
                            }
                        // Signal wait calls
                        case MethodInterpretation.SignalTryWait:
                        case MethodInterpretation.SignalBlockingWait:
                            {
                                var instance = resolvedArgumentsList[0].Argument.ShadowObject as ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                var isAlreadyWaiting = (thread.GetCallstackDepth() != 0)
                                    && (thread.PeekCallstack().Interpretation == MethodInterpretation.SignalTryWait ||
                                        thread.PeekCallstack().Interpretation == MethodInterpretation.SignalBlockingWait);
                                thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                if (!isAlreadyWaiting)
                                    instance.SyncBlock.Release(thread);
                                eventsHub.RaiseObjectWaitAttempted(runtime, function, instance, info);
                                break;
                            }
                        // Signal pulse calls
                        case MethodInterpretation.SignalPulseOne:
                        case MethodInterpretation.SignalPulseAll:
                            {
                                var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                thread.PushCallStack(function, interpretationData.Interpretation, instance);
                                var isPulseAll = interpretationData.Interpretation == MethodInterpretation.SignalPulseAll;
                                eventsHub.RaiseObjectPulseCalled(runtime, function, isPulseAll, instance, info);
                                break;
                            }
                        // Fields
                        case MethodInterpretation.FieldAccess:
                            {
                                var isWrite = (bool)resolvedArgumentsList[0].Argument.BoxedValue!;
                                var identifier = (ulong)resolvedArgumentsList[1].Argument.BoxedValue!;
                                var fieldInstance = thread.OperationContext.GetAndResetLastFieldInstance();
                                eventsHub.RaiseFieldAccessed(runtime, identifier, isWrite, fieldInstance, info);
                                break;
                            }
                        case MethodInterpretation.FieldInstanceAccess:
                            {
                                var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                thread.OperationContext.SetFieldInstance(instance as ShadowObject);
                                eventsHub.RaiseFieldInstanceAccessed(runtime, instance, info);
                                break;
                            }
                        // Arrays
                        case MethodInterpretation.ArrayElementAccess:
                            {
                                var isWrite = (bool)resolvedArgumentsList[0].Argument.BoxedValue!;
                                var identifier = (ulong)resolvedArgumentsList[1].Argument.BoxedValue!;
                                var arrayInstance = thread.OperationContext.GetAndResetLastArrayInstance();
                                var arrayIndex = thread.OperationContext.GetAndResetLastArrayIndex();
                                RuntimeContract.Assert(arrayInstance != null);
                                RuntimeContract.Assert(arrayIndex != null);
                                eventsHub.RaiseArrayElementAccessed(runtime, identifier, isWrite, arrayInstance, arrayIndex.Value, info);
                                break;
                            }
                        case MethodInterpretation.ArrayInstanceAccess:
                            {
                                var instance = resolvedArgumentsList[0].Argument.ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                thread.OperationContext.SetArrayInstance(instance as ShadowObject);
                                eventsHub.RaiseArrayInstanceAccessed(runtime, instance, info);
                                break;
                            }
                        case MethodInterpretation.ArrayIndexAccess:
                            {
                                var index = (int)resolvedArgumentsList[0].Argument.BoxedValue!;
                                thread.OperationContext.SetArrayIndex(index);
                                eventsHub.RaiseArrayIndexAccessed(runtime, index, info);
                                break;
                            }
                    }
                }
            }

            eventsHub.RaiseMethodCalled(runtime, function, resolvedArgumentsList, info);
        }

        public void ExecuteMethodReturned(ShadowThread thread, FunctionInfo function, RawReturnValue? retValue, RawArgumentsList? byRefArgs, RawEventInfo info)
        {
            // Ensure we can resolve the method
            var resolvedByRefArgumentsList = default(ArgumentsList);
            var resolvedReturnValue = default(IValueOrObject);
            var resolver = metadataContext.GetResolver(ProcessId);
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
                        new ValueOrObject(runtime.ShadowGC.GetObject(parsedReturnValue.Pointer!.Value));
                }

                // Resolve arguments
                if (parsedByRefArguments is not null)
                {
                    var resolvedByRefArguments = new (ushort, IValueOrObject)[parsedByRefArguments.Length];
                    for (var i = 0; i < parsedByRefArguments.Length; i++)
                    {
                        var argValue = (parsedByRefArguments[i].Argument.HasValue()) ? new ValueOrObject(parsedByRefArguments[i].Argument.BoxedValue!) :
                            new ValueOrObject(runtime.ShadowGC.GetObject(parsedByRefArguments[i].Argument.Pointer!.Value));
                        resolvedByRefArguments[i] = (parsedByRefArguments[i].Index, argValue);
                    }

                    resolvedByRefArgumentsList = new(resolvedByRefArguments);
                }

                // Resolve method interpretation
                if (methodRegistry.TryGetMethodInterpretationData(methodDef, out var interpretationData))
                {
                    switch (interpretationData.Interpretation)
                    {
                        // Lock acquire returns
                        case MethodInterpretation.LockTryAcquire:
                        case MethodInterpretation.LockBlockingAcquire:
                            {
                                RuntimeContract.Assert(thread.GetCallstackDepth() != 0);
                                var instance = thread.PopCallStack().Arguments as ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                RuntimeContract.Assert(interpretationData.Checker != null);
                                var isSuccess = interpretationData.Checker(resolvedReturnValue, resolvedByRefArgumentsList.Raw);
                                if (isSuccess)
                                    instance.SyncBlock.Acquire(thread);
                                eventsHub.RaiseLockAcquireReturned(runtime, function, instance, isSuccess, info);
                                break;
                            }
                        // Lock release returns
                        case MethodInterpretation.LockRelease:
                            {
                                RuntimeContract.Assert(thread.GetCallstackDepth() != 0);
                                var instance = thread.PopCallStack().Arguments as ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                instance.SyncBlock.Release(thread);
                                eventsHub.RaiseLockReleaseReturned(runtime, function, instance, info);
                                break;
                            }
                        // Signal wait returns
                        case MethodInterpretation.SignalTryWait:
                        case MethodInterpretation.SignalBlockingWait:
                            {
                                RuntimeContract.Assert(interpretationData.Checker != null);
                                RuntimeContract.Assert(thread.GetCallstackDepth() != 0);
                                var instance = thread.PopCallStack().Arguments as ShadowObject;
                                var isStillWaiting = (thread.GetCallstackDepth() != 0) && thread.PeekCallstack().Interpretation == MethodInterpretation.SignalTryWait;
                                if (isStillWaiting)
                                {
                                    // This event will be processed for the first Wait overload
                                    break;
                                }
                                instance!.SyncBlock.Acquire(thread);
                                var isSuccess = interpretationData.Checker(resolvedReturnValue, resolvedByRefArgumentsList.Raw);
                                eventsHub.RaiseObjectWaitReturned(runtime, function, instance, isSuccess, info);
                                break;
                            }
                        // Signal pulse returns
                        case MethodInterpretation.SignalPulseOne:
                        case MethodInterpretation.SignalPulseAll:
                            {
                                RuntimeContract.Assert(thread.GetCallstackDepth() != 0);
                                var instance = thread.PopCallStack().Arguments as ShadowObject;
                                RuntimeContract.Assert(instance != null);
                                var isPulseAll = interpretationData.Interpretation == MethodInterpretation.SignalPulseAll;
                                eventsHub.RaiseObjectPulseReturned(runtime, function, isPulseAll, instance, info);
                                break;
                            }
                    }
                }
            }

            eventsHub.RaiseMethodReturned(runtime, function, resolvedReturnValue, resolvedByRefArgumentsList, info);
        }
    }
}
