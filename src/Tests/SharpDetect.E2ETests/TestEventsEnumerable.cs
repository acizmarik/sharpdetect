// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Concurrent;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Plugins.PerThreadOrdering;
using SharpDetect.TemporalAsserts;

namespace SharpDetect.E2ETests;

public sealed class TestEventsEnumerable : IEnumerable<IEvent<ulong, RecordedEventType>>
{
    private readonly ConcurrentQueue<IEvent<ulong, RecordedEventType>> _queue = new();
    private ulong _currentId = 0;
    
    public TestEventsEnumerable(TestExecutionOrderingPlugin plugin)
        : this(plugin, plugin) { }

    public TestEventsEnumerable(TestPerThreadOrderingPlugin plugin)
        : this(plugin, plugin) { }

    public TestEventsEnumerable(PerThreadOrderingPluginBase pluginBase, ITestEventsSource eventsSource)
    {
        eventsSource.AssemblyLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, AssemblyLoadRecordedEvent)>(GetNextId(), RecordedEventType.AssemblyLoad, args));
        eventsSource.AssemblyReferenceInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, AssemblyReferenceInjectionRecordedEvent)>(GetNextId(), RecordedEventType.AssemblyReferenceInjection, args));
        eventsSource.GarbageCollectedTrackedObjects += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, GarbageCollectedTrackedObjectsRecordedEvent)>(GetNextId(), RecordedEventType.GarbageCollectedTrackedObjects, args));
        eventsSource.GarbageCollectionStarted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, GarbageCollectionStartRecordedEvent)>(GetNextId(), RecordedEventType.GarbageCollectionStart, args));
        eventsSource.GarbageCollectionFinished += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, GarbageCollectionFinishRecordedEvent)>(GetNextId(), RecordedEventType.GarbageCollectionFinish, args));
        eventsSource.JitCompilationStarted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, JitCompilationRecordedEvent)>(GetNextId(), RecordedEventType.JITCompilation, args));
        eventsSource.MethodBodyRewritten += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodBodyRewriteRecordedEvent)>(GetNextId(), RecordedEventType.MethodBodyRewrite, args));
        eventsSource.MethodDefinitionInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodDefinitionInjectionRecordedEvent)>(GetNextId(), RecordedEventType.MethodDefinitionInjection, args));
        eventsSource.MethodEntered += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodEnterRecordedEvent)>(GetNextId(), RecordedEventType.MethodEnter, args));
        eventsSource.MethodEnteredWithArguments += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodEnterWithArgumentsRecordedEvent)>(GetNextId(), RecordedEventType.MethodEnterWithArguments, args));
        eventsSource.MethodExited += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodExitRecordedEvent)>(GetNextId(), RecordedEventType.MethodExit, args));
        eventsSource.MethodExitedWithArguments += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodExitWithArgumentsRecordedEvent)>(GetNextId(), RecordedEventType.MethodExitWithArguments, args));
        eventsSource.MethodReferenceInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodReferenceInjectionRecordedEvent)>(GetNextId(), RecordedEventType.MethodReferenceInjection, args));
        eventsSource.MethodWrapperInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodWrapperInjectionRecordedEvent)>(GetNextId(), RecordedEventType.MethodWrapperInjection, args));
        eventsSource.ModuleLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ModuleLoadRecordedEvent)>(GetNextId(), RecordedEventType.ModuleLoad, args));
        eventsSource.ProfilerInitialized += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ProfilerInitializeRecordedEvent)>(GetNextId(), RecordedEventType.ProfilerInitialize, args));
        eventsSource.ProfilerLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ProfilerLoadRecordedEvent)>(GetNextId(), RecordedEventType.ProfilerLoad, args));
        eventsSource.ProfilerDestroyed += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ProfilerDestroyRecordedEvent)>(GetNextId(), RecordedEventType.ProfilerDestroy, args));
        eventsSource.Tailcalled += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TailcallRecordedEvent)>(GetNextId(), RecordedEventType.Tailcall, args));
        eventsSource.TailcalledWithArguments += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TailcallWithArgumentsRecordedEvent)>(GetNextId(), RecordedEventType.TailcallWithArguments, args));
        eventsSource.ThreadCreated += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadCreateRecordedEvent)>(GetNextId(), RecordedEventType.ThreadCreate, args));
        eventsSource.ThreadDestroyed += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadDestroyRecordedEvent)>(GetNextId(), RecordedEventType.ThreadDestroy, args));
        eventsSource.ThreadRenamed += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadRenameRecordedEvent)>(GetNextId(), RecordedEventType.ThreadRename, args));
        eventsSource.TypeLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TypeLoadRecordedEvent)>(GetNextId(), RecordedEventType.TypeLoad, args));
        eventsSource.TypeDefinitionInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TypeDefinitionInjectionRecordedEvent)>(GetNextId(), RecordedEventType.TypeDefinitionInjection, args));
        eventsSource.TypeReferenceInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TypeReferenceInjectionRecordedEvent)>(GetNextId(), RecordedEventType.TypeReferenceInjection, args));
        pluginBase.LockAcquireAttempted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, LockAcquireAttemptArgs)>(GetNextId(), RecordedEventType.LockAcquire, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.LockAcquireReturned += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, LockAcquireResultArgs)>(GetNextId(), RecordedEventType.LockAcquireResult, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.LockReleased += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, LockReleaseArgs)>(GetNextId(), RecordedEventType.LockReleaseResult, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.ObjectPulsedAll += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ObjectPulseAllArgs)>(GetNextId(), RecordedEventType.MonitorPulseAllResult, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.ObjectPulsedOne += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ObjectPulseOneArgs)>(GetNextId(), RecordedEventType.MonitorPulseOneResult, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.ObjectWaitAttempted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ObjectWaitAttemptArgs)>(GetNextId(), RecordedEventType.MonitorWaitAttempt, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.ObjectWaitReturned += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ObjectWaitResultArgs)>(GetNextId(), RecordedEventType.MonitorWaitResult, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.ThreadJoinAttempted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadJoinAttemptArgs)>(GetNextId(), RecordedEventType.ThreadJoinAttempt, (new RecordedEventMetadata(args.BlockedProcessThreadId.ProcessId, args.BlockedProcessThreadId.ThreadId), args)));
        pluginBase.ThreadJoinReturned += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadJoinResultArgs)>(GetNextId(), RecordedEventType.ThreadJoinResult, (new RecordedEventMetadata(args.BlockedProcessThreadId.ProcessId, args.BlockedProcessThreadId.ThreadId), args)));
        pluginBase.ThreadStarting += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadStartingArgs)>(GetNextId(), RecordedEventType.ThreadStartCore, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.ThreadStarted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadStartArgs)>(GetNextId(), RecordedEventType.ThreadStartCallback, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.StaticFieldRead += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, StaticFieldReadArgs)>(GetNextId(), RecordedEventType.StaticFieldRead, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.StaticFieldWritten += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, StaticFieldWriteArgs)>(GetNextId(), RecordedEventType.StaticFieldWrite, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.InstanceFieldRead += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, InstanceFieldReadArgs)>(GetNextId(), RecordedEventType.InstanceFieldRead, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.InstanceFieldWritten += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, InstanceFieldWriteArgs)>(GetNextId(), RecordedEventType.InstanceFieldWrite, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.TaskScheduled += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TaskScheduleArgs)>(GetNextId(), RecordedEventType.TaskSchedule, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.TaskStarted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TaskStartArgs)>(GetNextId(), RecordedEventType.TaskStart, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.TaskCompleted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TaskCompleteArgs)>(GetNextId(), RecordedEventType.TaskComplete, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.TaskJoinFinished += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TaskJoinFinishArgs)>(GetNextId(), RecordedEventType.TaskJoinFinish, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.SemaphoreAcquireAttempted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, SemaphoreAcquireAttemptArgs)>(GetNextId(), RecordedEventType.SemaphoreAcquire, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.SemaphoreAcquireReturned += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, SemaphoreAcquireResultArgs)>(GetNextId(), RecordedEventType.SemaphoreAcquireResult, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
        pluginBase.SemaphoreReleased += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, SemaphoreReleaseArgs)>(GetNextId(), RecordedEventType.SemaphoreReleaseResult, (new RecordedEventMetadata(args.ProcessThreadId.ProcessId, args.ProcessThreadId.ThreadId), args)));
    }
    
    public IEnumerator<IEvent<ulong, RecordedEventType>> GetEnumerator()
    {
        foreach (var item in _queue)
            yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    
    private ulong GetNextId()
    {
        return Interlocked.Increment(ref _currentId);
    }
}