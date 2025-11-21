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

    private void OnThreadJoinAttempted(ThreadJoinAttemptArgs args)
    {
        var id = new ProcessThreadId(args.ProcessId, args.BlockedThreadId);
        _concurrencyContext.RecordThreadJoinCalled(id, args.JoiningThreadId);
        CheckForDeadlocks(id);
    }

    private void OnThreadJoinReturned(ThreadJoinResultArgs args)
    {
        var id = new ProcessThreadId(args.ProcessId, args.BlockedThreadId);
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

            // Check if thread is blocked on a lock
            if (_concurrencyContext.TryGetWaitingLock(currentId, out var blockedLock))
            {
                var lockOwner = blockedLock.Owner;
                
                if (lockOwner is null || lockOwner == currentThreadId)
                {
                    // Lock is not owned or owned by current thread (re-entrance)
                    stack.Pop();
                    continue;
                }

                if (stack.Contains(lockOwner.Value))
                {
                    // Found deadlock
                    var deadlock = ConstructDeadlockInfo(processId, stack);
                    RecordDeadlockInfo(deadlock);
                    return;
                }
                
                if (visited.Add(lockOwner.Value))
                {
                    // Continue searching by current lock owner
                    stack.Push(lockOwner.Value);
                }
                else
                {
                    // Lock owner already visited - backtrack
                    stack.Pop();
                }
            }
            // Check if thread is blocked on Thread.Join
            else if (_concurrencyContext.TryGetWaitingThread(currentId, out var joiningThreadId))
            {
                if (stack.Contains(joiningThreadId.Value))
                {
                    // Found deadlock
                    var deadlock = ConstructDeadlockInfo(processId, stack);
                    RecordDeadlockInfo(deadlock);
                    return;
                }
                
                if (visited.Add(joiningThreadId.Value))
                {
                    // Continue searching by the thread being joined
                    stack.Push(joiningThreadId.Value);
                }
                else
                {
                    // Thread already visited - backtrack
                    stack.Pop();
                }
            }
            else
            {
                // Thread is not blocked on anything - backtrack
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
                    let threadInfo = GetThreadBlockingInfo(id)
                    select threadInfo).ToList());
    }

    private DeadlockThreadInfo GetThreadBlockingInfo(ProcessThreadId id)
    {
        // Check if blocked on a lock
        if (_concurrencyContext.TryGetWaitingLock(id, out var blockedLock))
        {
            return new DeadlockThreadInfo(
                ThreadId: id.ThreadId,
                ThreadName: Threads[id],
                BlockedOn: blockedLock.Owner!.Value,
                BlockedOnType: BlockedOnType.Lock,
                LockId: blockedLock.LockObjectId);
        }
        
        // Check if blocked on Thread.Join
        if (_concurrencyContext.TryGetWaitingThread(id, out var joiningThreadId))
        {
            return new DeadlockThreadInfo(
                ThreadId: id.ThreadId,
                ThreadName: Threads[id],
                BlockedOn: joiningThreadId.Value,
                BlockedOnType: BlockedOnType.Thread,
                LockId: null);
        }
        
        throw new InvalidOperationException($"Thread {id} is not blocked on anything.");
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
