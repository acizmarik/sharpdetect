// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using Microsoft.Extensions.Logging;
using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;
using SharpDetect.Extensibility;
using SharpDetect.Extensibility.Models;
using SharpDetect.Extensibility.PluginBases.OrderedEvents;
using SharpDetect.Loaders;
using SharpDetect.Metadata;
using SharpDetect.MethodDescriptors.Arguments;
using SharpDetect.Serialization;
using System.Collections.Immutable;

namespace SharpDetect.Plugins.Deadlock;

public partial class DeadlockPlugin : HappensBeforeOrderingPluginBase, IPlugin
{
    public readonly record struct DeadlockInfo(uint ProcessId, List<(ThreadId ThreadId, string ThreadName, ThreadId BlockedOn, TrackedObjectId LockId)> Cycle);
    public COR_PRF_MONITOR ProfilerMonitoringOptions => _requiredProfilerFlags;
    public RecordedEventActionVisitorBase EventsVisitor => this;
    public ImmutableArray<MethodDescriptor> MethodDescriptors { get; }
    public string ReportCategory => "Deadlock";

    private readonly HashSet<DeadlockInfo> _deadlocks;

    private const COR_PRF_MONITOR _requiredProfilerFlags =
        COR_PRF_MONITOR.COR_PRF_MONITOR_ASSEMBLY_LOADS |
        COR_PRF_MONITOR.COR_PRF_MONITOR_MODULE_LOADS |
        COR_PRF_MONITOR.COR_PRF_MONITOR_JIT_COMPILATION |
        COR_PRF_MONITOR.COR_PRF_MONITOR_THREADS |
        COR_PRF_MONITOR.COR_PRF_MONITOR_ENTERLEAVE |
        COR_PRF_MONITOR.COR_PRF_MONITOR_GC |
        COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_ARGS |
        COR_PRF_MONITOR.COR_PRF_ENABLE_FUNCTION_RETVAL |
        COR_PRF_MONITOR.COR_PRF_ENABLE_FRAME_INFO |
        COR_PRF_MONITOR.COR_PRF_DISABLE_INLINING |
        COR_PRF_MONITOR.COR_PRF_DISABLE_OPTIMIZATIONS |
        COR_PRF_MONITOR.COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
        COR_PRF_MONITOR.COR_PRF_DISABLE_ALL_NGEN_IMAGES;

    private readonly IMetadataContext _metadataContext;
    private readonly Dictionary<ThreadId, string> _threads;
    private readonly Dictionary<ThreadId, Lock?> _waitingForLocks;
    private readonly Dictionary<ThreadId, HashSet<Lock>> _takenLocks;
    private readonly Dictionary<ThreadId, Callstack> _callstacks;
    private readonly Dictionary<ThreadId, Stack<RuntimeArgumentList>> _callstackArguments;
    private int nextFreeThreadId;

    public DeadlockPlugin(
        IMetadataContext metadataContext,
        IModuleBindContext moduleBindContext,
        IArgumentsParser argumentsParser,
        IEventsDeliveryContext eventsSourceController,
        ILogger<DeadlockPlugin> logger)
        : base(moduleBindContext, metadataContext, argumentsParser, eventsSourceController, logger)
    {
        _metadataContext = metadataContext;

        _deadlocks = [];
        _threads = [];
        _waitingForLocks = [];
        _takenLocks = [];
        _callstacks = [];
        _callstackArguments = [];

        LockAcquireAttempted += OnLockAcquireAttempted;
        LockAcquireReturned += OnLockAcquireReturned;
        LockReleased += OnLockReleased;
        ObjectWaitAttempted += OnObjectWaitAttempted;
        ObjectWaitReturned += OnObjectWaitReturned;
        MethodDescriptors = GetRequiredMethodDescriptors();
    }

    private void OnLockAcquireAttempted(LockAcquireAttemptArgs args)
    {
        _waitingForLocks[args.ThreadId] = args.LockObj;
        CheckForDeadlocks(args.ProcessId, args.ThreadId);
    }

    private void OnLockAcquireReturned(LockAcquireResultArgs args)
    {
        if (args.IsSuccess)
            _takenLocks[args.ThreadId].Add(args.LockObj);

        _waitingForLocks[args.ThreadId] = null;
    }

    private void OnLockReleased(LockReleaseArgs args)
    {
        _takenLocks[args.ThreadId].Remove(args.LockObj);
    }

    private void OnObjectWaitAttempted(ObjectWaitAttemptArgs args)
    {
        _takenLocks[args.ThreadId].Remove(args.LockObj);
    }

    private void OnObjectWaitReturned(ObjectWaitResultArgs args)
    {
        _takenLocks[args.ThreadId].Add(args.LockObj);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        var threadId = args.ThreadId;
        if (!_threads.ContainsKey(threadId))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            StartNewThread(metadata.Pid, args.ThreadId);
        }

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadDestroyRecordedEvent args)
    {
        var threadId = args.ThreadId;
        var name = _threads[threadId];
        _threads.Remove(threadId);
        Logger.LogInformation("Destroyed thread {Name}.", name);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
    {
        var threadId = args.ThreadId;
        if (!_threads.ContainsKey(threadId))
        {
            // Note: if runtime spawns a thread with a custom name, we first receive the rename notification
            StartNewThread(metadata.Pid, args.ThreadId);
        }

        var oldName = _threads[threadId];
        var newName = $"{oldName} ({args.NewName})";
        _threads[threadId] = newName;
        Logger.LogInformation("Renamed thread {PldName} -> {NewName}.", oldName, newName);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterRecordedEvent args)
    {
        _callstacks[metadata.Tid].Push(args.ModuleId, args.MethodToken);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        _callstacks[metadata.Tid].Push(args.ModuleId, args.MethodToken);
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        _callstacks[metadata.Tid].Pop();
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
    {
        _callstacks[metadata.Tid].Pop();
        base.Visit(metadata, args);
    }

    private void StartNewThread(uint processId, ThreadId threadId)
    {
        _threads[threadId] = $"T{nextFreeThreadId++}";
        _waitingForLocks[threadId] = null;
        _takenLocks[threadId] = [];
        _callstacks[threadId] = new(processId, threadId);
        _callstackArguments[threadId] = new();
        Logger.LogInformation("Started thread {Name}.", _threads[threadId]);
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
            var blocked = _waitingForLocks[currentThreadId];

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
                    var blockedOnLock = _waitingForLocks[thread]!;
                    var blockedOnThreadId = blockedOnLock.Owner!.Value;
                    deadlock.Add((thread, _threads[thread], blockedOnThreadId, blockedOnLock.LockObjectId));
                }

                // Check if we recorded this deadlock
                if (_deadlocks.Any(d => d.Cycle.SequenceEqual(deadlock)))
                    return;

                Logger.LogWarning("[PID={Pid}] Deadlock detected (affects {ThreadsCount} threads).", processId, deadlock.Count);
                _deadlocks.Add(new DeadlockInfo(processId, deadlock));
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
}
