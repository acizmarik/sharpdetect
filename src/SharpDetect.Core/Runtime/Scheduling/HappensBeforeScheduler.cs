using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using SharpDetect.Common;
using SharpDetect.Common.Interop;
using SharpDetect.Common.Messages;
using SharpDetect.Common.Services;
using SharpDetect.Core.Runtime.Executors;
using SharpDetect.Core.Runtime.Threads;

namespace SharpDetect.Core.Runtime.Scheduling
{
    internal class HappensBeforeScheduler : SchedulerBase
    {
        protected readonly RuntimeEventExecutor Executor;

        public HappensBeforeScheduler(
            int processId, 
            RuntimeEventExecutor executor,
            IDateTimeProvider dateTimeProvider,
            ILoggerFactory loggerFactory)
            : base(processId, dateTimeProvider, loggerFactory)
        {
            Executor = executor;
        }

        #region PROFILING_NOTIFICATIONS
        public void Schedule_Heartbeat(RawEventInfo info)
        {
            FeedWatchdog();
        }

        public void Schedule_ProfilerInitialized(Version? _, RawEventInfo info)
        {
            var thread = StartNewThread(info.ThreadId);

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteProfilerInitialized(thread, info);
            });
        }

        public void Schedule_ProfilerDestroyed(RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteProfilerDestroyed(info);
                Terminate();
            });
        }

        public void Schedule_ModuleLoaded(UIntPtr moduleId, string path, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteModuleLoaded(moduleId, path, info);
            });
        }

        public void Schedule_TypeLoaded(TypeInfo typeInfo, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteTypeLoaded(typeInfo, info);
            });
        }

        public void Schedule_JITCompilationStarted(FunctionInfo functionInfo, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteJITCompilationStarted(functionInfo, info);
            });
        }

        public void Schedule_ThreadCreated(UIntPtr threadId, RawEventInfo info)
        {
            var newThread = Register(threadId);
            newThread.Start();

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteThreadCreated(newThread, info);
            });
        }

        public void Schedule_ThreadDestroyed(UIntPtr threadId, RawEventInfo info)
        {
            var destroyedThread = ThreadLookup[threadId];

            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteThreadDestroyed(destroyedThread, info);
            });
        }
        
        public void Schedule_RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON reason, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteRuntimeSuspendStarted(reason, info);
            });
        }

        public void Schedule_RuntimeSuspendFinished(RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteRuntimeSuspendFinished(info);
            });
        }

        public void Schedule_RuntimeResumeStarted(RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteRuntimeResumeStarted(info);
            });
        }

        public void Schedule_RuntimeResumeFinished(RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteRuntimeResumeFinished(info);
            });
        }

        public void Schedule_RuntimeThreadSuspended(UIntPtr threadId, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                var thread = ThreadLookup[threadId];
                Executor.ExecuteRuntimeThreadSuspended(thread, info);
            });
        }

        public void Schedule_RuntimeThreadResumed(UIntPtr threadId, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                var thread = ThreadLookup[threadId];
                Executor.ExecuteRuntimeThreadResumed(thread, info);
            });
        }

        public void Schedule_GarbageCollectionStarted(bool[] generationsCollected, COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteGarbageCollectionStarted(generationsCollected, bounds, info);
            });
        }

        public void Schedule_GarbageCollectionFinished(COR_PRF_GC_GENERATION_RANGE[] bounds, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteGarbageCollectionFinished(bounds, info);
            });
        }

        public void Schedule_SurvivingReferences(UIntPtr[] blockStarts, uint[] lengths, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteSurvivingReferences(blockStarts, lengths, info);
            });
        }

        public void Schedule_MovedReferences(UIntPtr[] oldBlockStarts, UIntPtr[] newBlockStarts, uint[] lengths, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent | JobFlags.OverrideSuspend, () =>
            {
                Executor.ExecuteMovedReferences(oldBlockStarts, newBlockStarts, lengths, info);
            });
        }
        #endregion

        #region REWRITING_NOTIFICATIONS

        public void Schedule_TypeInjected(TypeInfo type, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteTypeInjected(type, info);
            });
        }

        public void Schedule_MethodInjected(FunctionInfo functionInfo, MethodType type, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteMethodInjected(functionInfo, type, info);
            });
        }

        public void Schedule_TypeReferenced(TypeInfo typeInfo, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteTypeReferenced(typeInfo, info);
            });
        }

        public void Schedule_WrapperInjected(FunctionInfo functionInfo, MDToken wrapperToken, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteWrapperInjected(functionInfo, wrapperToken, info);
            });
        }

        public void Schedule_WrapperReferenced(FunctionInfo functionDef, FunctionInfo functionRef, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteWrapperReferenced(functionDef, functionRef, info);
            });
        }

        public void Schedule_HelperReferenced(FunctionInfo functionRef, MethodType type, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                Executor.ExecuteHelperReferenced(functionRef, type, info);
            });
        }

        #endregion

        #region EXECUTING_NOTIFICATIONS
        public void Schedule_MethodCalled(FunctionInfo function, RawArgumentsList? arguments, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                var thread = ThreadLookup[info.ThreadId];
                Executor.ExecuteMethodCalled(thread, function, arguments, info);
            });
        }

        public void Schedule_MethodReturned(FunctionInfo function, RawReturnValue? retValue, RawArgumentsList? byRefArgs, RawEventInfo info)
        {
            Schedule(info.ThreadId, info.Id, JobFlags.Concurrent, () =>
            {
                var thread = ThreadLookup[info.ThreadId];
                Executor.ExecuteMethodReturned(thread, function, retValue, byRefArgs, info);
            });
        }
        #endregion

        private ShadowThread StartNewThread(nuint threadId)
        {
            var newThread = Register(threadId);
            newThread.Start();
            return newThread;
        }
    }
}
