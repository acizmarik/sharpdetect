// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Collections.Immutable;
using SharpDetect.Plugins.Descriptors;

namespace SharpDetect.Plugins.Deadlock;

public partial class DeadlockPlugin : HappensBeforeOrderingPluginBase, IPlugin
{
    public string ReportCategory => "Deadlock";
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public override PluginConfiguration Configuration { get; } = PluginConfiguration.Create(
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
        additionalData: MonitorMethodDescriptors.GetAllMethods().Concat(
            ThreadMethodDescriptors.GetAllMethods())
            .ToImmutableArray());
    public DirectoryInfo ReportTemplates { get; }
    
    private readonly ICallstackResolver _callStackResolver;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrencyContext _concurrencyContext;
    private readonly Dictionary<uint, WaitForGraph> _waitForGraphs;
    private readonly Dictionary<(uint Pid, ulong RequestId), DeadlockInfo> _deadlocks;
    private readonly Dictionary<(uint Pid, ulong RequestId), StackTraceSnapshotsRecordedEvent> _deadlockStackTraces;

    public DeadlockPlugin(
        ICallstackResolver callstackResolver,
        TimeProvider timeProvider,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _callStackResolver = callstackResolver;
        _timeProvider = timeProvider;

        _waitForGraphs = [];
        _deadlocks = [];
        _deadlockStackTraces = [];
        _concurrencyContext = new ConcurrencyContext();

        LockAcquireAttempted += OnLockAcquireAttempted;
        LockAcquireReturned += OnLockAcquireReturned;
        LockReleased += OnLockReleased;
        ObjectWaitAttempted += OnObjectWaitAttempted;
        ObjectWaitReturned += OnObjectWaitReturned;
        ThreadJoinAttempted += OnThreadJoinAttempted;
        ThreadJoinReturned += OnThreadJoinReturned;

        ReportTemplates = new DirectoryInfo(
            Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location)!,
                "Deadlock",
                "Templates",
                "Partials"));
    }

    private void OnLockAcquireAttempted(LockAcquireAttemptArgs args)
    {
        var id = args.ProcessThreadId;
        _concurrencyContext.RecordLockAcquireCalled(id, args.LockObj);
        CheckForDeadlocks(id.ProcessId);
    }

    private void OnLockAcquireReturned(LockAcquireResultArgs args)
    {
        var id = args.ProcessThreadId;
        _concurrencyContext.RecordLockAcquireReturned(id, args.LockObj, args.IsSuccess);
    }

    private void OnLockReleased(LockReleaseArgs args)
    {
        var id = args.ProcessThreadId;
        _concurrencyContext.RecordLockReleaseReturned(id, args.LockObj);
    }

    private void OnObjectWaitAttempted(ObjectWaitAttemptArgs args)
    {
        var id = args.ProcessThreadId;
        _concurrencyContext.RecordObjectWaitCalled(id, args.LockObj);
    }

    private void OnObjectWaitReturned(ObjectWaitResultArgs args)
    {
        var id = args.ProcessThreadId;
        _concurrencyContext.RecordObjectWaitReturned(id, args.LockObj);
    }

    private void OnThreadJoinAttempted(ThreadJoinAttemptArgs args)
    {
        var id = args.BlockedProcessThreadId;
        _concurrencyContext.RecordThreadJoinCalled(id, args.JoiningProcessThreadId);
        CheckForDeadlocks(id.ProcessId);
    }

    private void OnThreadJoinReturned(ThreadJoinResultArgs args)
    {
        var id = args.BlockedProcessThreadId;
        _concurrencyContext.RecordThreadJoinReturned(id);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        if (!_concurrencyContext.HasThread(id))
            _concurrencyContext.RecordThreadCreated(id);

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, args.ThreadId);
        if (!_concurrencyContext.HasThread(id))
            _concurrencyContext.RecordThreadCreated(id);

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, StackTraceSnapshotsRecordedEvent args)
    {
        var key = (metadata.Pid, metadata.CommandId!.Value);
        _deadlockStackTraces.Add(key, args);
        
        base.Visit(metadata, args);
    }
}
