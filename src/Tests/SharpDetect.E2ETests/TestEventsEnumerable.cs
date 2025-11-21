// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections;
using System.Collections.Concurrent;
using SharpDetect.Core.Events;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Plugins;
using SharpDetect.TemporalAsserts;

namespace SharpDetect.E2ETests;

public sealed class TestEventsEnumerable : IEnumerable<IEvent<ulong, RecordedEventType>>
{
    private readonly TestHappensBeforePlugin _plugin;
    private readonly ConcurrentQueue<IEvent<ulong, RecordedEventType>> _queue = new();
    private ulong _currentId = 0;
    
    public TestEventsEnumerable(TestHappensBeforePlugin plugin)
    {
        _plugin = plugin;
        _plugin.AssemblyLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, AssemblyLoadRecordedEvent)>(GetNextId(), RecordedEventType.AssemblyLoad, args));
        _plugin.AssemblyReferenceInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, AssemblyReferenceInjectionRecordedEvent)>(GetNextId(), RecordedEventType.AssemblyReferenceInjection, args));
        _plugin.GarbageCollectedTrackedObjects += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, GarbageCollectedTrackedObjectsRecordedEvent)>(GetNextId(), RecordedEventType.GarbageCollectedTrackedObjects, args));
        _plugin.GarbageCollectionStarted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, GarbageCollectionStartRecordedEvent)>(GetNextId(), RecordedEventType.GarbageCollectionStart, args));
        _plugin.GarbageCollectionFinished += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, GarbageCollectionFinishRecordedEvent)>(GetNextId(), RecordedEventType.GarbageCollectionFinish, args));
        _plugin.JitCompilationStarted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, JitCompilationRecordedEvent)>(GetNextId(), RecordedEventType.JITCompilation, args));
        _plugin.MethodBodyRewritten += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodBodyRewriteRecordedEvent)>(GetNextId(), RecordedEventType.MethodBodyRewrite, args));
        _plugin.MethodDefinitionInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodDefinitionInjectionRecordedEvent)>(GetNextId(), RecordedEventType.MethodDefinitionInjection, args));
        _plugin.MethodEntered += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodEnterRecordedEvent)>(GetNextId(), RecordedEventType.MethodEnter, args));
        _plugin.MethodEnteredWithArguments += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodEnterWithArgumentsRecordedEvent)>(GetNextId(), RecordedEventType.MethodEnterWithArguments, args));
        _plugin.MethodExited += args 
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodExitRecordedEvent)>(GetNextId(), RecordedEventType.MethodExit, args));
        _plugin.MethodExitedWithArguments += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodExitWithArgumentsRecordedEvent)>(GetNextId(), RecordedEventType.MethodExitWithArguments, args));
        _plugin.MethodReferenceInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodReferenceInjectionRecordedEvent)>(GetNextId(), RecordedEventType.MethodReferenceInjection, args));
        _plugin.MethodWrapperInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, MethodWrapperInjectionRecordedEvent)>(GetNextId(), RecordedEventType.MethodWrapperInjection, args));
        _plugin.ModuleLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ModuleLoadRecordedEvent)>(GetNextId(), RecordedEventType.ModuleLoad, args));
        _plugin.ProfilerInitialized += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ProfilerInitializeRecordedEvent)>(GetNextId(), RecordedEventType.ProfilerInitialize, args));
        _plugin.ProfilerLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ProfilerLoadRecordedEvent)>(GetNextId(), RecordedEventType.ProfilerLoad, args));
        _plugin.ProfilerDestroyed += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ProfilerDestroyRecordedEvent)>(GetNextId(), RecordedEventType.ProfilerDestroy, args));
        _plugin.Tailcalled += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TailcallRecordedEvent)>(GetNextId(), RecordedEventType.Tailcall, args));
        _plugin.TailcalledWithArguments += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TailcallWithArgumentsRecordedEvent)>(GetNextId(), RecordedEventType.TailcallWithArguments, args));
        _plugin.ThreadCreated += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadCreateRecordedEvent)>(GetNextId(), RecordedEventType.ThreadCreate, args));
        _plugin.ThreadDestroyed += args 
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadDestroyRecordedEvent)>(GetNextId(), RecordedEventType.ThreadDestroy, args));
        _plugin.ThreadRenamed += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, ThreadRenameRecordedEvent)>(GetNextId(), RecordedEventType.ThreadRename, args));
        _plugin.TypeLoaded += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TypeLoadRecordedEvent)>(GetNextId(), RecordedEventType.TypeLoad, args));
        _plugin.TypeDefinitionInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TypeDefinitionInjectionRecordedEvent)>(GetNextId(), RecordedEventType.TypeDefinitionInjection, args));
        _plugin.TypeReferenceInjected += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, TypeReferenceInjectionRecordedEvent)>(GetNextId(), RecordedEventType.TypeReferenceInjection, args));
        _plugin.LockAcquireAttempted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.LockAcquireAttemptArgs)>(GetNextId(), RecordedEventType.MonitorLockAcquire, (new RecordedEventMetadata(args.ProcessId, args.ThreadId), args)));
        _plugin.LockAcquireReturned += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.LockAcquireResultArgs)>(GetNextId(), RecordedEventType.MonitorLockAcquireResult, (new RecordedEventMetadata(args.ProcessId, args.ThreadId), args)));
        _plugin.LockReleased += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.LockReleaseArgs)>(GetNextId(), RecordedEventType.MonitorLockRelease, (new RecordedEventMetadata(args.ProcessId, args.ThreadId), args)));
        _plugin.ObjectPulsedAll += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.ObjectPulseAllArgs)>(GetNextId(), RecordedEventType.MonitorPulseAllResult, (new RecordedEventMetadata(args.ProcessId, args.ThreadId), args)));
        _plugin.ObjectPulsedOne += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.ObjectPulseOneArgs)>(GetNextId(), RecordedEventType.MonitorPulseOneResult, (new RecordedEventMetadata(args.ProcessId, args.ThreadId), args)));
        _plugin.ObjectWaitAttempted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.ObjectWaitAttemptArgs)>(GetNextId(), RecordedEventType.MonitorWaitAttempt, (new RecordedEventMetadata(args.ProcessId, args.ThreadId), args)));
        _plugin.ObjectWaitReturned += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.ObjectWaitResultArgs)>(GetNextId(), RecordedEventType.MonitorWaitResult, (new RecordedEventMetadata(args.ProcessId, args.ThreadId), args)));
        _plugin.ThreadJoinAttempted += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.ThreadJoinAttemptArgs)>(GetNextId(), RecordedEventType.ThreadJoinAttempt, (new RecordedEventMetadata(args.ProcessId, args.BlockedThreadId), args)));
        _plugin.ThreadJoinReturned += args
            => _queue.Enqueue(new Event<ulong, RecordedEventType, (RecordedEventMetadata, HappensBeforeOrderingPluginBase.ThreadJoinResultArgs)>(GetNextId(), RecordedEventType.ThreadJoinResult, (new RecordedEventMetadata(args.ProcessId, args.BlockedThreadId), args)));
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