// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
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
        additionalData: MonitorMethodDescriptors.GetAllMethods().ToImmutableArray());
    public DirectoryInfo ReportTemplates { get; }
    
    private readonly ICallstackResolver _callStackResolver;
    private readonly Dictionary<(uint Pid, ulong RequestId), DeadlockInfo> _deadlocks;
    private readonly Dictionary<(uint Pid, ulong RequestId), StackTraceSnapshotsRecordedEvent> _deadlockStackTraces;
    private readonly Dictionary<(uint Pid, ThreadId Tid), Lock?> _waitingForLocks;
    private readonly Dictionary<(uint Pid, ThreadId Tid), HashSet<Lock>> _takenLocks;
    private readonly Dictionary<(uint Pid, ThreadId Tid), Stack<RuntimeArgumentList>> _callstackArguments;

    public DeadlockPlugin(
        ICallstackResolver callstackResolver,
        IServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        _callStackResolver = callstackResolver;

        _deadlocks = [];
        _deadlockStackTraces = [];
        _waitingForLocks = [];
        _takenLocks = [];
        _callstackArguments = [];

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
        var key = (args.ProcessId, args.ThreadId);
        _waitingForLocks[key] = args.LockObj;
        CheckForDeadlocks(args.ProcessId, args.ThreadId);
    }

    private void OnLockAcquireReturned(LockAcquireResultArgs args)
    {
        var key = (args.ProcessId, args.ThreadId);
        if (args.IsSuccess)
            _takenLocks[key].Add(args.LockObj);

        _waitingForLocks[key] = null;
    }

    private void OnLockReleased(LockReleaseArgs args)
    {
        var key = (args.ProcessId, args.ThreadId);
        _takenLocks[key].Remove(args.LockObj);
    }

    private void OnObjectWaitAttempted(ObjectWaitAttemptArgs args)
    {
        var key = (args.ProcessId, args.ThreadId);
        _takenLocks[key].Remove(args.LockObj);
    }

    private void OnObjectWaitReturned(ObjectWaitResultArgs args)
    {
        var key = (args.ProcessId, args.ThreadId);
        _takenLocks[key].Add(args.LockObj);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        var threadId = args.ThreadId;
        if (!Threads.ContainsKey(threadId))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            StartNewThread(metadata.Pid, args.ThreadId);
        }

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
    {
        var threadId = args.ThreadId;
        if (!Threads.ContainsKey(threadId))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            StartNewThread(metadata.Pid, args.ThreadId);
        }

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, StackTraceSnapshotsRecordedEvent args)
    {
        var key = (metadata.Pid, metadata.CommandId!.Value);
        _deadlockStackTraces.Add(key, args);
        
        base.Visit(metadata, args);
    }

    private void CheckForDeadlocks(uint processId, ThreadId threadId)
    {
        var stack = new Stack<ThreadId>();
        var visited = new HashSet<ThreadId>();
        stack.Push(threadId);
        visited.Add(threadId);

        while (stack.Count > 0)
        {
            var currentThreadId = stack.Peek();
            var blocked = _waitingForLocks[(processId, currentThreadId)];

            if (blocked is null)
            {
                // Current thread is not waiting for any lock
                stack.Pop();
                continue;
            }

            if (blocked?.Owner is null)
            {
                // Lock is not acquired
                stack.Pop();
                continue;
            }

            var owner = blocked.Owner.Value;
            if (owner == currentThreadId)
            {
                // Reentrancy
                stack.Pop();
                continue;
            }

            if (stack.Contains(owner))
            {
                // Construct deadlock chain
                var deadlock = new List<(ThreadId ThreadId, string ThreadName, ThreadId BlockedOnThreadId, TrackedObjectId LockId)>();
                foreach (var thread in stack)
                {
                    var blockedOnLock = _waitingForLocks[(processId, thread)]!;
                    var blockedOnThreadId = blockedOnLock.Owner!.Value;
                    deadlock.Add((thread, Threads[thread], blockedOnThreadId, blockedOnLock.LockObjectId));
                }

                // Check if we recorded this deadlock
                if (_deadlocks.Any(d => d.Key.Pid == processId && d.Value.Cycle.SequenceEqual(deadlock)))
                    return;
                
                // Request stack traces for all involved threads
                var commandSender = GetCommandSender(processId);
                var commandId = commandSender.SendCommand(new CreateStackTraceSnapshotsCommand(
                    deadlock.Select(e => e.ThreadId).ToArray()));
                
                _deadlocks.Add((processId, commandId.Value), new DeadlockInfo(processId, deadlock));
                Logger.LogWarning("[PID={Pid}] Deadlock detected (affects {ThreadsCount} threads).", processId, deadlock.Count);
                return;
            }

            if (visited.Add(owner))
            {
                stack.Push(owner);
            }
            else
            {
                stack.Pop();
            }
        }
    }

    private void StartNewThread(uint processId, ThreadId threadId)
    {
        var key = (processId, threadId);
        _waitingForLocks[key] = null;
        _takenLocks[key] = [];
        _callstackArguments[key] = new();
    }
}
