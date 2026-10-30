// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Configuration;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Serialization;
using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Plugins.Descriptors.Intrinsics;
using SharpDetect.Plugins.Descriptors.Methods;
using SharpDetect.Plugins.Descriptors.Types;
using SharpDetect.Plugins.PerThreadOrdering;

namespace SharpDetect.Plugins.DataRace.FastTrack;

[PluginMetadata(Name = "FastTrack", Description = "Vector-clock data race detector.")]
public partial class FastTrackPlugin : PerThreadOrderingPluginBase, IPlugin
{
    public string ReportCategory => "DataRace";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public override ProfilerConfiguration ProfilerConfiguration { get; }
    public DirectoryInfo ReportTemplates { get; }

    private readonly FastTrackDetector _detector;
    private readonly FastTrackPluginConfiguration _pluginConfiguration;
    private readonly HashSet<FieldId> _reportedRacyFields = [];
    private readonly List<DataRaceInfo> _detectedRaces = [];
    private readonly HashSet<ProcessThreadId> _trackedThreads = [];
    private readonly Dictionary<ProcessTrackedObjectId, ProcessThreadId> _startingThreads = [];

    public FastTrackPlugin(
        PluginOptionsConfiguration pluginOptionsConfiguration,
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver,
        IArgumentsParser argumentsParser,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        PathsConfiguration pathsConfiguration,
        TimeProvider timeProvider,
        ILogger<FastTrackPlugin> logger)
        : base(
            moduleBindContext,
            metadataContext,
            symbolResolver,
            argumentsParser,
            profilerCommandSenderProvider,
            timeProvider,
            logger)
    {
        _pluginConfiguration = pluginOptionsConfiguration.ParseConfigurationOrDefault<FastTrackPluginConfiguration>();
        _detector = new FastTrackDetector(_pluginConfiguration, metadataContext, timeProvider, logger, GetThreadName);

        ProfilerConfiguration = ProfilerConfiguration.Create(
            eventMask: COR_PRF_MONITOR.COR_PRF_MONITOR_ASSEMBLY_LOADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_JIT_COMPILATION |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_THREADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_ENTERLEAVE |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_EXCEPTIONS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_GC |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_ARGS |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_RETVAL |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_STACK_SNAPSHOT |
                       COR_PRF_MONITOR.COR_PRF_ENABLE_FRAME_INFO |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_INLINING |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_OPTIMIZATIONS |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                       COR_PRF_MONITOR.COR_PRF_DISABLE_ALL_NGEN_IMAGES,
            additionalData: new
            {
                MethodDescriptors = SynchronizationMethodDescriptors.GetAllMethods()
                    .Concat(FieldAccessDescriptors.GetAllMethods())
                    .ToImmutableArray(),
                FieldAccessIntrinsicDescriptors = InterlockedIntrinsicDescriptors.GetAllIntrinsics().Concat(
                    VolatileIntrinsicDescriptors.GetAllIntrinsics())
                    .ToImmutableArray(),
                TypeInjectionDescriptors = SharpDetectHelperTypeDescriptors.GetAllTypes(),
                _pluginConfiguration.EnableFieldsAccessInstrumentation,
                _pluginConfiguration.SkipInstrumentationForAssemblies,
                _pluginConfiguration.EnableStackTraceCollection,
                _pluginConfiguration.StackTraceCollectionMaxDepth,
                StackTraceCollectionForFields = _pluginConfiguration.StackTraceCollectionForFields.IsDefault
                    ? ImmutableArray<string>.Empty
                    : _pluginConfiguration.StackTraceCollectionForFields
            },
            temporaryFilesFolder: pathsConfiguration.TemporaryFilesFolder,
            sessionId: pathsConfiguration.SessionId);

        LockAcquireReturned += OnLockAcquireReturned;
        LockReleased += OnLockReleased;
        ObjectWaitAttempted += OnObjectWaitAttempted;
        ObjectWaitReturned += OnObjectWaitReturned;
        SignalCreated += OnSignalCreated;
        Signaled += OnSignaled;
        SignalReset += OnSignalReset;
        SignalWaitReturned += OnSignalWaitReturned;
        PermitsCreated += OnPermitsCreated;
        PermitAcquireReturned += OnPermitAcquireReturned;
        PermitsReleased += OnPermitsReleased;
        PermitsReleaseRetracted += OnPermitsReleaseRetracted;
        ForkRequested += OnForkRequested;
        ForkStarted += OnForkStarted;
        ForkFinished += OnForkFinished;
        ForkJoined += OnForkJoined;
        ThreadJoinReturned += OnThreadJoinReturned;
        ValuePublication += OnValuePublication;
        StaticFieldRead += OnStaticFieldRead;
        StaticFieldWritten += OnStaticFieldWritten;
        InstanceFieldRead += OnInstanceFieldRead;
        InstanceFieldWritten += OnInstanceFieldWritten;
        TypeInitializerEntered += OnTypeInitializerEntered;
        TypeInitialized += OnTypeInitialized;
        TypeInitializerFailed += OnTypeInitializerFailed;

        ReportTemplates = new DirectoryInfo(
            Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location)!,
                "DataRace",
                "Templates",
                "Partials"));
    }

    private string? GetThreadName(ProcessThreadId threadId)
    {
        return Threads.GetValueOrDefault(threadId);
    }

    private void OnLockAcquireReturned(LockAcquireResultArgs args)
    {
        if (args.IsSuccess)
        {
            _detector.RecordLockAcquired(args.ProcessThreadId, args.LockId, args.IsShared);
        }
    }

    private void OnLockReleased(LockReleaseArgs args)
    {
        _detector.RecordLockReleased(args.ProcessThreadId, args.LockId, args.IsShared);
    }

    private void OnObjectWaitAttempted(ObjectWaitAttemptArgs args)
    {
        _detector.RecordObjectWaitCalled(args.ProcessThreadId, args.LockId);
    }

    private void OnObjectWaitReturned(ObjectWaitResultArgs args)
    {
        _detector.RecordObjectWaitReturned(args.ProcessThreadId, args.LockId);
    }

    private void OnThreadJoinReturned(ThreadJoinResultArgs args)
    {
        if (args.IsSuccess)
        {
            _detector.RecordThreadJoin(args.BlockedProcessThreadId, args.JoinedProcessThreadId);
        }
    }

    private void OnStaticFieldRead(StaticFieldReadArgs args)
        => OnFieldRead(args.ProcessThreadId, args.MethodOffset, args.FieldToken, objectId: null, args.DeclaringTypeHandle, args.AccessKind, args.Stack);

    private void OnInstanceFieldRead(InstanceFieldReadArgs args)
        => OnFieldRead(args.ProcessThreadId, args.MethodOffset, args.FieldToken, args.ObjectId, declaringTypeHandle: 0, args.AccessKind, args.Stack);

    private void OnStaticFieldWritten(StaticFieldWriteArgs args)
        => OnFieldWritten(args.ProcessThreadId, args.MethodOffset, args.FieldToken, objectId: null, args.DeclaringTypeHandle, args.AccessKind, args.Stack);

    private void OnInstanceFieldWritten(InstanceFieldWriteArgs args)
        => OnFieldWritten(args.ProcessThreadId, args.MethodOffset, args.FieldToken, args.ObjectId, declaringTypeHandle: 0, args.AccessKind, args.Stack);

    private void OnFieldRead(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        FieldAccessKind accessKind,
        CapturedStackTrace stack)
    {
        switch (accessKind)
        {
            case FieldAccessKind.Volatile:
                if (_detector.RecordVolatileRead(threadId, methodOffset, fieldToken, objectId, declaringTypeHandle, stack) is { } volatileRaceInfo)
                    RecordDataRace(volatileRaceInfo);
                break;

            case FieldAccessKind.Atomic:
                if (_detector.RecordAtomicAcquire(threadId, methodOffset, fieldToken, objectId, declaringTypeHandle, stack) is { } atomicRaceInfo)
                    RecordDataRace(atomicRaceInfo);
                break;

            case FieldAccessKind.CompareExchangeSucceeded:
            case FieldAccessKind.CompareExchangeFailed:
                var compareExchangeRaceInfo = _detector.RecordCompareExchangeCompleted(
                    threadId,
                    methodOffset,
                    fieldToken,
                    objectId,
                    declaringTypeHandle,
                    stack,
                    succeeded: accessKind == FieldAccessKind.CompareExchangeSucceeded);
                if (compareExchangeRaceInfo != null)
                    RecordDataRace(compareExchangeRaceInfo);
                break;

            default:
                if (_detector.RecordRead(threadId, methodOffset, fieldToken, objectId, declaringTypeHandle, stack) is { } raceInfo)
                    RecordDataRace(raceInfo);
                break;
        }
    }

    private void OnFieldWritten(
        ProcessThreadId threadId,
        uint methodOffset,
        MdToken fieldToken,
        ProcessTrackedObjectId? objectId,
        nuint declaringTypeHandle,
        FieldAccessKind accessKind,
        CapturedStackTrace stack)
    {
        switch (accessKind)
        {
            case FieldAccessKind.Volatile:
                if (_detector.RecordVolatileWrite(threadId, methodOffset, fieldToken, objectId, declaringTypeHandle, stack) is { } volatileRaceInfo)
                    RecordDataRace(volatileRaceInfo);
                break;

            case FieldAccessKind.VolatileWriteCompleted:
                _detector.RecordVolatileWriteCompleted(threadId, stack.Top.ModuleId, fieldToken, objectId, declaringTypeHandle);
                break;

            case FieldAccessKind.Atomic:
                _detector.RecordAtomicRelease(threadId, stack.Top.ModuleId, fieldToken, objectId, declaringTypeHandle);
                break;

            case FieldAccessKind.CompareExchange:
                _detector.RecordCompareExchangeRelease(threadId, stack.Top.ModuleId, fieldToken, objectId, declaringTypeHandle);
                break;

            default:
                if (_detector.RecordWrite(threadId, methodOffset, fieldToken, objectId, declaringTypeHandle, stack) is { } raceInfo)
                    RecordDataRace(raceInfo);
                break;
        }
    }
    
    private void OnForkRequested(ForkRequestArgs args)
    {
        switch (args.Kind)
        {
            case ForkKind.Thread:
                _startingThreads[args.ChildId] = args.ProcessThreadId;
                _detector.RecordThreadForkRequested(args.ProcessThreadId, args.ChildId);
                break;
            case ForkKind.Continuation:
                _detector.RecordTaskContinuationRegistered(args.ProcessThreadId, args.ChildId);
                break;
            default:
                _detector.RecordTaskScheduled(args.ProcessThreadId, args.ChildId);
                break;
        }
    }

    private void OnForkStarted(ForkStartArgs args)
    {
        if (args.Kind != ForkKind.Thread)
        {
            _detector.RecordTaskStarted(args.ProcessThreadId, args.ChildId);
            return;
        }

        var childThreadId = args.ProcessThreadId;
        if (_startingThreads.Remove(args.ChildId, out var parentThreadId))
        {
            Logger.LogInformation(
                "Thread {Parent} is parent of thread {Child}.",
                Threads.GetValueOrDefault(parentThreadId, parentThreadId.ToString()),
                Threads.GetValueOrDefault(childThreadId, childThreadId.ToString()));
        }

        _detector.RecordThreadCreated(childThreadId);
        _detector.RecordThreadFork(args.ChildId, childThreadId);
    }

    private void OnForkFinished(ForkFinishArgs args)
    {
        switch (args.FinishKind)
        {
            case ForkFinishKind.Completed:
                _detector.RecordTaskCompleted(args.ProcessThreadId, args.ChildId);
                break;
            case ForkFinishKind.SegmentCompleted:
                _detector.RecordTaskSegmentCompleted(args.ProcessThreadId, args.ChildId);
                break;
            case ForkFinishKind.PromiseCompleted:
                _detector.RecordTaskPromiseCompleted(args.ProcessThreadId, args.ChildId);
                break;
        }
    }

    private void OnForkJoined(ForkJoinArgs args)
    {
        if (args.Outcome == TaskJoinOutcome.Completed)
            _detector.RecordTaskJoinFinished(args.ProcessThreadId, args.ChildId);
        else if (args.Outcome == TaskJoinOutcome.Threw)
            _detector.RecordTaskJoinThrew(args.ProcessThreadId, args.ChildId);
    }

    private void OnPermitsCreated(PermitsCreatedArgs args)
    {
        _detector.RecordSemaphoreCreated(args.PermitPoolId, args.InitialCount);
    }

    private void OnValuePublication(ValuePublicationArgs args)
    {
        switch (args.Kind)
        {
            case ValuePublicationKind.Store:
                _detector.RecordValuePublished(args.ProcessThreadId, args.Container, args.Value);
                break;
            case ValuePublicationKind.Load:
                _detector.RecordValueObserved(args.ProcessThreadId, args.Container, args.Value);
                break;
            case ValuePublicationKind.StoreLoad:
                _detector.RecordValuePublished(args.ProcessThreadId, args.Container, args.Value);
                _detector.RecordValueObserved(args.ProcessThreadId, args.Container, args.Value);
                break;
            case ValuePublicationKind.MaybeStoreLoad:
                _detector.RecordValuePublished(args.ProcessThreadId, args.Container, args.Value, onlyIfAbsent: true);
                _detector.RecordValueObserved(args.ProcessThreadId, args.Container, args.Value);
                break;
        }
    }

    private void OnTypeInitializerEntered(TypeInitializerArgs args)
    {
        _detector.RecordTypeInitializerEntered(args.ProcessThreadId, args.ModuleId, args.TypeInitializerToken);
    }

    private void OnTypeInitialized(TypeInitializerArgs args)
    {
        _detector.RecordTypeInitialized(args.ProcessThreadId, args.ModuleId, args.TypeInitializerToken);
    }

    private void OnTypeInitializerFailed(TypeInitializerArgs args)
    {
        _detector.RecordTypeInitializerFailed(args.ProcessThreadId, args.ModuleId, args.TypeInitializerToken);
    }

    private void OnPermitAcquireReturned(PermitAcquireResultArgs args)
    {
        if (args.IsSuccess)
            _detector.RecordSemaphoreAcquired(args.ProcessThreadId, args.PermitPoolId);
    }

    private void OnPermitsReleased(PermitsReleaseArgs args)
    {
        _detector.RecordSemaphoreReleased(args.ProcessThreadId, args.PermitPoolId, args.ReleaseCount);
    }

    private void OnPermitsReleaseRetracted(PermitsReleaseArgs args)
    {
        _detector.RecordSemaphoreReleaseRetracted(args.ProcessThreadId, args.PermitPoolId, args.ReleaseCount);
    }

    private void OnSignalCreated(SignalCreatedArgs args)
    {
        _detector.RecordEventCreated(args.SignalId, args.InitialState);
    }

    private void OnSignaled(SignalSetArgs args)
    {
        _detector.RecordEventSignaled(args.ProcessThreadId, args.SignalId);
    }

    private void OnSignalReset(SignalResetArgs args)
    {
        _detector.RecordEventReset(args.SignalId);
    }

    private void OnSignalWaitReturned(SignalWaitResultArgs args)
    {
        if (args.IsSuccess)
            _detector.RecordEventWaitReturned(args.ProcessThreadId, args.SignalId, args.IsAutoReset);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        if (_trackedThreads.Add(id))
            _detector.RecordThreadCreated(id);

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadDestroyRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        _trackedThreads.Remove(id);
        _detector.RecordThreadDestroyed(id);

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectedTrackedObjectsRecordedEvent args)
    {
        _detector.RecordGarbageCollectedObjects(metadata.Pid, args.RemovedTrackedObjectIds);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, FinalizationQueuedTrackedObjectsRecordedEvent args)
    {
        _detector.RecordFinalizationQueuedObjects(metadata.Pid, args.FinalizationQueuedTrackedObjectIds);
        base.Visit(metadata, args);
    }

    private void RecordDataRace(DataRaceInfo raceInfo)
    {
        _detectedRaces.Add(raceInfo);
        if (_reportedRacyFields.Add(raceInfo.FieldId))
            DataRaceLogger.LogDataRace(Logger, raceInfo, MetadataContext, SymbolResolver);
    }
}


