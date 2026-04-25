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
using SharpDetect.Plugins.Descriptors.Methods;
using SharpDetect.Plugins.Descriptors.Types;
using SharpDetect.Plugins.PerThreadOrdering;

namespace SharpDetect.Plugins.DataRace.FastTrack;

[PluginMetadata(Name = "FastTrack", Description = "Vector-clock data race detector.")]
public partial class FastTrackPlugin : PerThreadOrderingPluginBase, IPlugin
{
    public string ReportCategory => "DataRace";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public override PluginConfiguration Configuration { get; }
    public DirectoryInfo ReportTemplates { get; }

    private readonly FastTrackDetector _detector;
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
        var configuration = pluginOptionsConfiguration.ParseConfigurationOrDefault<FastTrackPluginConfiguration>(Logger);
        _detector = new FastTrackDetector(configuration, metadataContext, timeProvider, logger, GetThreadName);

        Configuration = PluginConfiguration.Create(
            eventMask: COR_PRF_MONITOR.COR_PRF_MONITOR_ASSEMBLY_LOADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_JIT_COMPILATION |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_THREADS |
                       COR_PRF_MONITOR.COR_PRF_MONITOR_ENTERLEAVE |
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
                MethodDescriptors = MonitorMethodDescriptors.GetAllMethods().Concat(
                    LockMethodDescriptors.GetAllMethods()).Concat(
                    ThreadMethodDescriptors.GetAllMethods()).Concat(
                    TaskMethodDescriptors.GetAllMethods()).Concat(
                    SemaphoreSlimMethodDescriptors.GetAllMethods()).Concat(
                    FieldAccessDescriptors.GetAllMethods())
                    .ToImmutableArray(),
                TypeInjectionDescriptors = SharpDetectHelperTypeDescriptors.GetAllTypes(),
                configuration.EnableFieldsAccessInstrumentation,
                configuration.SkipInstrumentationForAssemblies
            },
            temporaryFilesFolder: pathsConfiguration.TemporaryFilesFolder,
            sessionId: pathsConfiguration.SessionId);

        LockAcquireReturned += OnLockAcquireReturned;
        LockReleased += OnLockReleased;
        ObjectWaitAttempted += OnObjectWaitAttempted;
        ObjectWaitReturned += OnObjectWaitReturned;
        StaticFieldRead += OnStaticFieldRead;
        StaticFieldWritten += OnStaticFieldWritten;
        InstanceFieldRead += OnInstanceFieldRead;
        InstanceFieldWritten += OnInstanceFieldWritten;
        ThreadJoinReturned += OnThreadJoinReturned;
        ThreadStarting += OnThreadStarting;
        ThreadStarted += OnThreadStarted;
        TaskScheduled += OnTaskScheduled;
        TaskStarted += OnTaskStarted;
        TaskCompleted += OnTaskCompleted;
        TaskJoinFinished += OnTaskJoinFinished;
        SemaphoreAcquireReturned += OnSemaphoreAcquireReturned;
        SemaphoreReleased += OnSemaphoreReleased;

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
            _detector.RecordLockAcquired(args.ProcessThreadId, args.LockId);
        }
    }

    private void OnLockReleased(LockReleaseArgs args)
    {
        _detector.RecordLockReleased(args.ProcessThreadId, args.LockId);
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
    {
        if (args.IsVolatile)
        {
            _detector.RecordVolatileRead(
                args.ProcessThreadId,
                args.ModuleId,
                args.FieldToken,
                objectId: null);
        }
        else
        {
            if (_detector.RecordRead(
                    args.ProcessThreadId,
                    args.ModuleId,
                    args.MethodToken,
                    args.MethodOffset,
                    args.FieldToken,
                    objectId: null) is { } raceInfo)
            {
                RecordDataRace(raceInfo);
            }
        }
    }

    private void OnInstanceFieldRead(InstanceFieldReadArgs args)
    {
        if (args.IsVolatile)
        {
            _detector.RecordVolatileRead(
                args.ProcessThreadId,
                args.ModuleId,
                args.FieldToken,
                args.ObjectId);
        }
        else
        {
            if (_detector.RecordRead(
                    args.ProcessThreadId,
                    args.ModuleId,
                    args.MethodToken,
                    args.MethodOffset,
                    args.FieldToken,
                    args.ObjectId) is { } raceInfo)
            {
                RecordDataRace(raceInfo);
            }
        }
    }

    private void OnStaticFieldWritten(StaticFieldWriteArgs args)
    {
        if (args.IsVolatile)
        {
            _detector.RecordVolatileWrite(
                args.ProcessThreadId,
                args.ModuleId,
                args.FieldToken,
                objectId: null);
        }
        else
        {
            if (_detector.RecordWrite(
                    args.ProcessThreadId,
                    args.ModuleId,
                    args.MethodToken,
                    args.MethodOffset,
                    args.FieldToken,
                    objectId: null) is { } raceInfo)
            {
                RecordDataRace(raceInfo);
            }
        }
    }

    private void OnInstanceFieldWritten(InstanceFieldWriteArgs args)
    {
        if (args.IsVolatile)
        {
            _detector.RecordVolatileWrite(
                args.ProcessThreadId,
                args.ModuleId,
                args.FieldToken,
                args.ObjectId);
        }
        else
        {
            if (_detector.RecordWrite(
                    args.ProcessThreadId,
                    args.ModuleId,
                    args.MethodToken,
                    args.MethodOffset,
                    args.FieldToken,
                    args.ObjectId) is { } raceInfo)
            {
                RecordDataRace(raceInfo);
            }
        }
    }
    
    private void OnThreadStarting(ThreadStartingArgs obj)
    {
        _startingThreads[obj.ThreadObjectId] = obj.ProcessThreadId;
    }
    
    private void OnThreadStarted(ThreadStartArgs obj)
    {
        var childThreadId = obj.ProcessThreadId;
        var parentThreadId = _startingThreads[obj.ThreadObjectId];
        _startingThreads.Remove(obj.ThreadObjectId);
        Logger.LogInformation("Thread {Parent} is parent of thread {Child}.", Threads[parentThreadId], Threads[childThreadId]);
        
        _detector.RecordThreadCreated(childThreadId);
        _detector.RecordThreadFork(parentThreadId, childThreadId);
    }

    private void OnTaskScheduled(TaskScheduleArgs args)
    {
        _detector.RecordTaskScheduled(args.ProcessThreadId, args.TaskObjectId);
    }

    private void OnTaskStarted(TaskStartArgs args)
    {
        _detector.RecordTaskStarted(args.ProcessThreadId, args.TaskObjectId);
    }

    private void OnTaskCompleted(TaskCompleteArgs args)
    {
        _detector.RecordTaskCompleted(args.ProcessThreadId, args.TaskObjectId);
    }

    private void OnTaskJoinFinished(TaskJoinFinishArgs args)
    {
        if (args.IsSuccess)
            _detector.RecordTaskJoinFinished(args.ProcessThreadId, args.TaskObjectId);
    }

    private void OnSemaphoreAcquireReturned(SemaphoreAcquireResultArgs args)
    {
        if (args.IsSuccess)
            _detector.RecordSemaphoreAcquired(args.ProcessThreadId, args.SemaphoreId);
    }

    private void OnSemaphoreReleased(SemaphoreReleaseArgs args)
    {
        _detector.RecordSemaphoreReleased(args.ProcessThreadId, args.SemaphoreId);
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

    private void RecordDataRace(DataRaceInfo raceInfo)
    {
        _detectedRaces.Add(raceInfo);
        if (_reportedRacyFields.Add(raceInfo.FieldId))
            DataRaceLogger.LogDataRace(Logger, raceInfo, MetadataContext);
    }
}


