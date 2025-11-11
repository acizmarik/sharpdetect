// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.Deadlock.Descriptors;
using System.Collections.Immutable;
using SharpDetect.Core.Commands;

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
    private readonly ConcurrencyContext _concurrencyContext;
    private readonly Dictionary<(uint Pid, ulong RequestId), DeadlockInfo> _deadlocks;
    private readonly Dictionary<(uint Pid, ulong RequestId), StackTraceSnapshotsRecordedEvent> _deadlockStackTraces;

    public DeadlockPlugin(
        ICallstackResolver callstackResolver,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _callStackResolver = callstackResolver;

        _deadlocks = [];
        _deadlockStackTraces = [];
        _concurrencyContext = new ConcurrencyContext();

        LockAcquireAttempted += OnLockAcquireAttempted;
        LockAcquireReturned += OnLockAcquireReturned;
        LockReleased += OnLockReleased;
        ObjectWaitAttempted += OnObjectWaitAttempted;
        ObjectWaitReturned += OnObjectWaitReturned;

        ReportTemplates = new DirectoryInfo(
            Path.Combine(
                Path.GetDirectoryName(GetType().Assembly.Location)!,
                "Deadlock",
                "Templates",
                "Partials"));
    }

    private void OnLockAcquireAttempted(LockAcquireAttemptArgs args)
    {
        var id = new ProcessThreadId(args.ProcessId, args.ThreadId);
        _concurrencyContext.RecordLockAcquireCalled(id, args.LockObj);
        CheckForDeadlocks(id);
    }

    private void OnLockAcquireReturned(LockAcquireResultArgs args)
    {
        var id = new ProcessThreadId(args.ProcessId, args.ThreadId);
        _concurrencyContext.RecordLockAcquireReturned(id, args.LockObj, args.IsSuccess);
    }

    private void OnLockReleased(LockReleaseArgs args)
    {
        var id = new ProcessThreadId(args.ProcessId, args.ThreadId);
        _concurrencyContext.RecordLockReleaseReturned(id, args.LockObj);
    }

    private void OnObjectWaitAttempted(ObjectWaitAttemptArgs args)
    {
        var id = new ProcessThreadId(args.ProcessId, args.ThreadId);
        _concurrencyContext.RecordObjectWaitCalled(id, args.LockObj);
    }

    private void OnObjectWaitReturned(ObjectWaitResultArgs args)
    {
        var id = new ProcessThreadId(args.ProcessId, args.ThreadId);
        _concurrencyContext.RecordObjectWaitReturned(id, args.LockObj);
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

    private void CheckForDeadlocks(ProcessThreadId sourceId)
    {
        var processId = sourceId.ProcessId;
        var stack = new Stack<ThreadId>();
        var visited = new HashSet<ThreadId>();
        stack.Push(sourceId.ThreadId);
        visited.Add(sourceId.ThreadId);

        while (stack.Count > 0)
        {
            var currentThreadId = stack.Peek();
            var currentId = sourceId with { ThreadId = currentThreadId };

            if (!_concurrencyContext.TryGetWaitingLock(currentId, out var blocked) ||
                blocked.Owner is not { } owner ||
                owner == currentThreadId)
            {
                // Skip current thread - one of the following situation happened:
                // 1) it is not waiting for any lock
                // 2) the lock it is waiting for is not owned by any thread
                // 3) the lock it is waiting for is owned by itself (re-entrance)
                stack.Pop();
                continue;
            }

            if (stack.Contains(owner))
            {
                // Found deadlock
                var deadlock = ConstructDeadlockInfo(processId, stack);
                RecordDeadlockInfo(deadlock);
                return;
            }
            
            if (visited.Add(owner))
            {
                // Continue searching by current lock owner
                stack.Push(owner);
            }
            else
            {
                // Lock owner already visited - backtrack
                stack.Pop();
            }
        }
    }

    private DeadlockInfo ConstructDeadlockInfo(uint processId, IEnumerable<ThreadId> blockedThreadsIds)
    {
        return new DeadlockInfo(
            ProcessId: processId, 
            Cycle: (from threadId in blockedThreadsIds
                    let id = new ProcessThreadId(processId, threadId)
                    let blockedOnLock = _concurrencyContext.GetWaitingLock(id)
                    select new DeadlockThreadInfo(
                        ThreadId: threadId,
                        ThreadName: Threads[id], 
                        BlockedOn: blockedOnLock.Owner!.Value,
                        LockId: blockedOnLock.LockObjectId)).ToList());
    }

    private void RecordDeadlockInfo(DeadlockInfo deadlock)
    {
        if (_deadlocks.Any(d => d.Key.Pid == deadlock.ProcessId && d.Value.Cycle.SequenceEqual(deadlock.Cycle)))
        {
            // Already recorded
            return;
        }
        
        var commandSender = GetCommandSender(deadlock.ProcessId);
        var threadIds = deadlock.Cycle.Select(t => t.ThreadId).ToArray();
        var commandId = commandSender.SendCommand(new CreateStackTraceSnapshotsCommand(threadIds));
        _deadlocks.Add((deadlock.ProcessId, commandId.Value), deadlock);

        var threadsCount = deadlock.Cycle.Count;
        Logger.LogWarning("[PID={Pid}] Deadlock detected (affects {ThreadsCount} threads).", deadlock.ProcessId, threadsCount);
    }
}
