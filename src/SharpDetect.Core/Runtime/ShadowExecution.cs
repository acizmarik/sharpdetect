﻿using dnlib.DotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Runtime;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Common.Services;
using SharpDetect.Common.Services.Metadata;
using SharpDetect.Core.Runtime.Scheduling;
using System.Collections.Immutable;

namespace SharpDetect.Core.Runtime
{
    internal class ShadowExecution : IDisposable
    {        
        private ImmutableDictionary<int, HappensBeforeScheduler> schedulersLookup;
        private readonly IMetadataContext metadataContext;
        private readonly IServiceProvider serviceProvider;
        private readonly RuntimeEventsHub runtimeEventsHub;
        private readonly TaskCompletionSource executionFinishedCompletionSource;
        private readonly ILogger<ShadowExecution> logger;
        private int processCount;
        private bool isDisposed;

        public ShadowExecution(
            RuntimeEventsHub runtimeEventsHub,
            IProfilingMessageHub profilingMessageHub, 
            IRewritingMessageHub rewritingMessageHub, 
            IExecutingMessageHub executingMessageHub,
            IMetadataContext metadataContext,
            ILoggerFactory loggerFactory, 
            IServiceProvider serviceProvider)
        {
            this.metadataContext = metadataContext;
            this.logger = loggerFactory.CreateLogger<ShadowExecution>();
            this.runtimeEventsHub = runtimeEventsHub;
            this.serviceProvider = serviceProvider;
            this.schedulersLookup = ImmutableDictionary.Create<int, HappensBeforeScheduler>();
            this.executionFinishedCompletionSource = new TaskCompletionSource();

            profilingMessageHub.Heartbeat += ProfilingMessageHub_Heartbeat;
            profilingMessageHub.ProfilerInitialized += ProfilingMessageHub_ProfilerInitialized;
            profilingMessageHub.ProfilerDestroyed += ProfilingMessageHub_ProfilerDestroyed;
            profilingMessageHub.ModuleLoaded += ProfilingMessageHub_ModuleLoaded;
            profilingMessageHub.TypeLoaded += ProfilingMessageHub_TypeLoaded;
            profilingMessageHub.JITCompilationStarted += ProfilingMessageHub_JITCompilationStarted;
            profilingMessageHub.ThreadCreated += ProfilingMessageHub_ThreadCreated;
            profilingMessageHub.ThreadDestroyed += ProfilingMessageHub_ThreadDestroyed;
            profilingMessageHub.RuntimeSuspendStarted += ProfilingMessageHub_RuntimeSuspendStarted;
            profilingMessageHub.RuntimeSuspendFinished += ProfilingMessageHub_RuntimeSuspendFinished;
            profilingMessageHub.RuntimeThreadSuspended += ProfilingMessageHub_RuntimeThreadSuspended;
            profilingMessageHub.RuntimeThreadResumed += ProfilingMessageHub_RuntimeThreadResumed;
            profilingMessageHub.GarbageCollectionStarted += ProfilingMessageHub_GarbageCollectionStarted;
            profilingMessageHub.GarbageCollectionFinished += ProfilingMessageHub_GarbageCollectionFinished;
            profilingMessageHub.SurvivingReferences += ProfilingMessageHub_SurvivingReferences;
            profilingMessageHub.MovedReferences += ProfilingMessageHub_MovedReferences;

            rewritingMessageHub.TypeInjected += RewritingHub_TypeInjected;
            rewritingMessageHub.TypeReferenced += RewritingHub_TypeReferenced;
            rewritingMessageHub.MethodInjected += RewritingHub_MethodInjected;
            rewritingMessageHub.MethodWrapperInjected += RewritingHub_WrapperInjected;
            rewritingMessageHub.WrapperMethodReferenced += RewritingHub_WrapperReferenced;
            rewritingMessageHub.HelperMethodReferenced += RewritingHub_HelperReferenced;

            executingMessageHub.ArrayElementAccessed += ExecutingHub_ArrayElementAccessed;
            executingMessageHub.ArrayIndexAccessed += ExecutingHub_ArrayIndexAccessed;
            executingMessageHub.ArrayInstanceAccessed += ExecutingHub_ArrayInstanceAccessed;
            executingMessageHub.FieldAccessed += ExecutingHub_FieldAccessed;
            executingMessageHub.FieldInstanceAccessed += ExecutingHub_FieldInstanceAccessed;
            executingMessageHub.LockAcquireAttempted += ExecutingHub_LockAcquireAttempted;
            executingMessageHub.LockAcquireReturned += ExecutingHub_LockAcquireReturned;
            executingMessageHub.LockReleaseCalled += ExecutingHub_LockReleaseCalled;
            executingMessageHub.LockReleaseReturned += ExecutingHub_LockReleaseReturned;
            executingMessageHub.MethodCalled += ExecutingHub_MethodCalled;
            executingMessageHub.MethodReturned += ExecutingHub_MethodReturned;
            executingMessageHub.ObjectWaitAttempted += ExecutingHub_ObjectWaitAttempted;
            executingMessageHub.ObjectWaitReturned += ExecutingHub_ObjectWaitReturned;
            executingMessageHub.ObjectPulseCalled += ExecutingHub_ObjectPulseCalled;
            executingMessageHub.ObjectPulseReturned += ExecutingHub_ObjectPulseReturned;
        }

        public Task GetAwaitableTaskAsync()
        {
            return executionFinishedCompletionSource.Task;
        }

        internal IEnumerable<HappensBeforeScheduler> Schedulers
        {
            get => schedulersLookup.Values;
        }

        private HappensBeforeScheduler Register(int processId)
        {
            var metadataResolver = metadataContext.GetResolver(processId);
            var metadataEmitter = metadataContext.GetEmitter(processId);
            var shadowCLR = new ShadowCLR(processId, metadataResolver, metadataEmitter, serviceProvider.GetRequiredService<IModuleBindContext>());
            var scheduler = new HappensBeforeScheduler(processId, shadowCLR, runtimeEventsHub, new UtcDateTimeProvider());
            logger.LogInformation("[{class}] Process with PID={pid} started.", nameof(ShadowExecution), processId);

            scheduler.ProcessFinished += () =>
            {
                // Process exited normally
                logger.LogInformation("[{class}] Process with PID={pid} terminated.", nameof(ShadowExecution), processId);
                Unregister(processId);
            };
            scheduler.ProcessCrashed += () =>
            {
                // Process probably crashed
                logger.LogError("[{class}] Process with PID={pid} stopped responding.", nameof(ShadowExecution), processId);
                Unregister(processId);
            };

            ImmutableDictionary<int, HappensBeforeScheduler> oldLookup;
            ImmutableDictionary<int, HappensBeforeScheduler> newLookup;

            do
            {
                oldLookup = schedulersLookup;
                newLookup = oldLookup.Add(processId, scheduler);
            } while (Interlocked.CompareExchange(ref schedulersLookup, newLookup, oldLookup) != oldLookup);

            Interlocked.Increment(ref processCount);
            return scheduler;
        }

        private void Unregister(int processId)
        {
            ImmutableDictionary<int, HappensBeforeScheduler> oldLookup;
            ImmutableDictionary<int, HappensBeforeScheduler> newLookup;

            do
            {
                oldLookup = schedulersLookup;
                newLookup = oldLookup.Remove(processId);
            } while (Interlocked.CompareExchange(ref schedulersLookup, newLookup, oldLookup) != oldLookup);

            var newCount = Interlocked.Decrement(ref processCount);

            // Make sure to notify about end once there are not profiled processes
            if (newCount == 0)
                executionFinishedCompletionSource.SetResult();
        }

        private HappensBeforeScheduler GetScheduler(int processId)
        {
            if (!schedulersLookup.TryGetValue(processId, out var scheduler))
            {
                lock (schedulersLookup)
                {
                    // Make sure each process gets initialized only once
                    if (!schedulersLookup.TryGetValue(processId, out scheduler))
                        scheduler = Register(processId);
                }
            }

            return scheduler;
        }

        #region PROFILING_NOTIFICATIONS
        private void ProfilingMessageHub_Heartbeat(EventInfo info)
        {
            var scheduler = GetScheduler(info.ProcessId);
            scheduler.Schedule_Heartbeat(info);
        }

        private void ProfilingMessageHub_ProfilerInitialized(EventInfo info)
        {
            var scheduler = GetScheduler(info.ProcessId);
            scheduler.Schedule_ProfilerInitialized(info);
        }

        private void ProfilingMessageHub_ProfilerDestroyed(EventInfo info)
        {
            var scheduler = GetScheduler(info.ProcessId);
            scheduler.Schedule_ProfilerDestroyed(info);
        }

        private void ProfilingMessageHub_ModuleLoaded((UIntPtr ModuleId, string Path, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ModuleLoaded(args.ModuleId, args.Path, args.Info);
        }

        private void ProfilingMessageHub_TypeLoaded((TypeInfo TypeInfo, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_TypeLoaded(args.TypeInfo, args.Info);
        }

        private void ProfilingMessageHub_JITCompilationStarted((FunctionInfo FunctionInfo, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_JITCompilationStarted(args.FunctionInfo, args.Info);
        }

        private void ProfilingMessageHub_ThreadCreated((UIntPtr ThreadId, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ThreadCreated(args.ThreadId, args.Info);
        }

        private void ProfilingMessageHub_ThreadDestroyed((UIntPtr ThreadId, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ThreadDestroyed(args.ThreadId, args.Info);
        }

        private void ProfilingMessageHub_RuntimeSuspendStarted((COR_PRF_SUSPEND_REASON Reason, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_RuntimeSuspendStarted(args.Reason, args.Info);
        }
       
        private void ProfilingMessageHub_RuntimeSuspendFinished(EventInfo info)
        {
            var scheduler = GetScheduler(info.ProcessId);
            scheduler.Schedule_RuntimeSuspendFinished(info);
        }

        private void ProfilingMessageHub_RuntimeThreadSuspended((UIntPtr ThreadId, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_RuntimeThreadSuspended(args.ThreadId, args.Info);
        }

        private void ProfilingMessageHub_RuntimeThreadResumed((UIntPtr ThreadId, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_RuntimeThreadResumed(args.ThreadId, args.Info);
        }

        private void ProfilingMessageHub_GarbageCollectionStarted((bool[] GenerationsCollected, COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_GarbageCollectionStarted(args.GenerationsCollected, args.Bounds, args.Info);
        }

        private void ProfilingMessageHub_GarbageCollectionFinished((COR_PRF_GC_GENERATION_RANGE[] Bounds, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_GarbageCollectionFinished(args.Bounds, args.Info);
        }

        private void ProfilingMessageHub_SurvivingReferences((UIntPtr[] BlockStarts, UIntPtr[] Lengths, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_SurvivingReferences(args.BlockStarts, args.Lengths, args.Info);
        }

        private void ProfilingMessageHub_MovedReferences((UIntPtr[] OldBlockStarts, UIntPtr[] NewBlockStarts, UIntPtr[] Lengths, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_MovedReferences(args.OldBlockStarts, args.NewBlockStarts, args.Lengths, args.Info);
        }
        #endregion

        #region REWRITING_NOTIFICATIONS
        private void RewritingHub_TypeInjected((TypeInfo TypeInfo, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_TypeInjected(args.TypeInfo, args.Info);
        }

        private void RewritingHub_TypeReferenced((TypeInfo TypeInfo, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_TypeReferenced(args.TypeInfo, args.Info);
        }

        private void RewritingHub_MethodInjected((FunctionInfo FunctionInfo, MethodType Type, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_MethodInjected(args.FunctionInfo, args.Type, args.Info);
        }

        private void RewritingHub_WrapperInjected((FunctionInfo FunctionInfo, MDToken WrapperToken, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_WrapperInjected(args.FunctionInfo, args.WrapperToken, args.Info);
        }

        private void RewritingHub_WrapperReferenced((FunctionInfo FunctionDef, FunctionInfo FunctionRef, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_WrapperReferenced(args.FunctionDef, args.FunctionRef, args.Info);
        }

        private void RewritingHub_HelperReferenced((FunctionInfo FunctionRef, MethodType Type, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_HelperReferenced(args.FunctionRef, args.Type, args.Info);
        }
        #endregion

        #region EXECUTING_NOTIFICATIONS
        private void ExecutingHub_ArrayElementAccessed((ulong Identifier, bool IsWrite, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ArrayElementAccessed(args.Identifier, args.IsWrite, args.Info);
        }

        private void ExecutingHub_ArrayInstanceAccessed((UIntPtr Instance, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ArrayInstanceAccessed(args.Instance, args.Info);
        }

        private void ExecutingHub_ArrayIndexAccessed((int Index, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ArrayIndexAccessed(args.Index, args.Info);
        }

        private void ExecutingHub_FieldAccessed((ulong Identifier, bool IsWrite, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_FieldAccessed(args.Identifier, args.IsWrite, args.Info);
        }

        private void ExecutingHub_FieldInstanceAccessed((UIntPtr Instance, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_FieldInstanceAccessed(args.Instance, args.Info);
        }

        private void ExecutingHub_LockAcquireAttempted((FunctionInfo Function, UIntPtr InstancePtr, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_LockAcquireAttempted(args.Function, args.InstancePtr, args.Info);
        }

        private void ExecutingHub_LockAcquireReturned((FunctionInfo Function, bool IsSuccess, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_LockAcquireReturned(args.Function, args.IsSuccess, args.Info);
        }

        private void ExecutingHub_LockReleaseCalled((FunctionInfo Function, UIntPtr InstancePtr, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_LockReleaseCalled(args.Function, args.InstancePtr, args.Info);
        }

        private void ExecutingHub_LockReleaseReturned((FunctionInfo Function, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_LockReleaseReturned(args.Function, args.Info);
        }

        private void ExecutingHub_MethodCalled((FunctionInfo Function, (ushort Index, IValueOrPointer Arg)[]? Arguments, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_MethodCalled(args.Function, args.Arguments, args.Info);
        }

        private void ExecutingHub_MethodReturned((FunctionInfo Function, IValueOrPointer? ReturnValue, (ushort Index, IValueOrPointer Arg)[]? ByRefArguments, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_MethodReturned(args.Function, args.ReturnValue, args.ByRefArguments, args.Info);
        }

        private void ExecutingHub_ObjectWaitAttempted((FunctionInfo Function, UIntPtr InstancePtr, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ObjectWaitAttempted(args.Function, args.InstancePtr, args.Info);
        }

        private void ExecutingHub_ObjectWaitReturned((FunctionInfo Function, bool IsSuccess, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ObjectWaitReturned(args.Function, args.IsSuccess, args.Info);
        }

        private void ExecutingHub_ObjectPulseCalled((FunctionInfo Function, bool IsPulseAll, UIntPtr InstancePtr, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ObjectPulseCalled(args.Function, args.IsPulseAll, args.InstancePtr, args.Info);
        }

        private void ExecutingHub_ObjectPulseReturned((FunctionInfo Function, bool IsPulseAll, EventInfo Info) args)
        {
            var scheduler = GetScheduler(args.Info.ProcessId);
            scheduler.Schedule_ObjectPulseReturned(args.Function, args.IsPulseAll, args.Info);
        }
        #endregion

        public void Dispose()
        {
            if (!isDisposed)
            {
                isDisposed = true;

                // Dispose all not yet unregistered schedulers
                foreach (var (_, scheduler) in schedulersLookup)
                    scheduler.Dispose();

                GC.SuppressFinalize(this);
            }
        }
    }
}