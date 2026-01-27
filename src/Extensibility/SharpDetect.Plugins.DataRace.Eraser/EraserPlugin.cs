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
using SharpDetect.Plugins.Descriptors.Methods;
using SharpDetect.Plugins.Descriptors.Types;
using SharpDetect.Plugins.PerThreadOrdering;

namespace SharpDetect.Plugins.DataRace.Eraser;

public partial class EraserPlugin : PerThreadOrderingPluginBase, IPlugin
{
    public string ReportCategory => "DataRace";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public override PluginConfiguration Configuration { get; }
    public DirectoryInfo ReportTemplates { get; }

    private readonly EraserDetector _detector;
    private readonly List<DataRaceInfo> _detectedRaces = [];
    private readonly HashSet<ProcessThreadId> _trackedThreads = [];

    public EraserPlugin(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        IArgumentsParser argumentsParser,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        PathsConfiguration pathsConfiguration,
        TimeProvider timeProvider,
        ILogger<EraserPlugin> logger)
        : base(
            moduleBindContext,
            metadataContext,
            argumentsParser,
            profilerCommandSenderProvider,
            timeProvider,
            logger)
    {
        _detector = new EraserDetector(metadataContext, timeProvider, logger, GetThreadName);
        
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
                    ThreadMethodDescriptors.GetAllMethods().Concat(
                    FieldAccessDescriptors.GetAllMethods()))
                    .ToImmutableArray(),
                TypeInjectionDescriptors = SharpDetectHelperTypeDescriptors.GetAllTypes(),
                EnableFieldsAccessInstrumentation = true
            },
            temporaryFilesFolder: pathsConfiguration.TemporaryFilesFolder);

        // Subscribe to execution ordering events
        LockAcquireReturned += OnLockAcquireReturned;
        LockReleased += OnLockReleased;
        ObjectWaitAttempted += OnObjectWaitAttempted;
        ObjectWaitReturned += OnObjectWaitReturned;
        StaticFieldRead += OnStaticFieldRead;
        StaticFieldWritten += OnStaticFieldWritten;

        ReportTemplates = new DirectoryInfo(
            Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location)!,
                "Eraser",
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

    private void OnStaticFieldRead(StaticFieldReadArgs args)
    {
        var raceInfo = _detector.RecordRead(
            args.ProcessThreadId,
            args.ModuleId,
            args.MethodToken,
            args.FieldToken);

        if (raceInfo == null)
            return;
        
        _detectedRaces.Add(raceInfo);
        Logger.LogWarning(
            "Potential data race detected on field {FieldName} by thread {Thread}: {AccessType} access with empty lock set ({PreviousState} -> {NewState})",
            GetFieldDisplayName(raceInfo.FieldId),
            Threads[args.ProcessThreadId],
            raceInfo.CurrentAccess.AccessType,
            raceInfo.PreviousState,
            raceInfo.NewState);
    }

    private void OnStaticFieldWritten(StaticFieldWriteArgs args)
    {
        var raceInfo = _detector.RecordWrite(
            args.ProcessThreadId,
            args.ModuleId,
            args.MethodToken,
            args.FieldToken);

        if (raceInfo == null)
            return;
        
        _detectedRaces.Add(raceInfo);
        Logger.LogWarning(
            "Potential data race detected on field {FieldName} by thread {Thread}: {AccessType} access with empty lock set ({PreviousState} -> {NewState})",
            GetFieldDisplayName(raceInfo.FieldId),
            Threads[args.ProcessThreadId],
            raceInfo.CurrentAccess.AccessType,
            raceInfo.PreviousState,
            raceInfo.NewState);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        if (_trackedThreads.Add(id))
            _detector.RecordThreadCreated(id);

        base.Visit(metadata, args);
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
    
    private static string GetFieldDisplayName(FieldId fieldId)
    {
        return $"{fieldId.FieldDef.DeclaringType.FullName}.{fieldId.FieldDef.Name}";
    }
}
