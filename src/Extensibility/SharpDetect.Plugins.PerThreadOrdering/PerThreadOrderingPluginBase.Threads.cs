// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using System.Runtime.InteropServices;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase
{
    private readonly ThreadObjectRegistry _threadObjectRegistry = new();
    private readonly Dictionary<ProcessTrackedObjectId, List<(ProcessThreadId JoiningThread, ModuleId ModuleId, MdMethodDef MethodToken)>> _pendingJoinAttempts = [];

    public event Action<ThreadStartingArgs>? ThreadStarting;
    public event Action<ThreadStartArgs>? ThreadStarted;
    public event Action<ThreadJoinAttemptArgs>? ThreadJoinAttempted;
    public event Action<ThreadJoinResultArgs>? ThreadJoinReturned;

    private void RegisterThreadBindings()
    {
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.ThreadStartCore, OnThreadStartCore);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.ThreadStartCallback, OnThreadStartCallback);
        Bind<MethodEnterWithArgumentsRecordedEvent>(RecordedEventType.ThreadJoinAttempt, OnThreadJoinAttempt);
        Bind<MethodExitRecordedEvent>(RecordedEventType.ThreadJoinResult, OnThreadJoinResult);
    }

    private void OnThreadStartCore(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Note: this method is invoked by parent thread
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var threadObjectId = new ProcessTrackedObjectId(id.ProcessId, new TrackedObjectId(MemoryMarshal.Read<nuint>(args.ArgumentValues)));
        ProcessThreadStartCore(id, threadObjectId);
    }

    private void OnThreadStartCallback(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        // Note: this method is invoked by the newly started thread
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var arguments = ParseArguments(metadata, args);
        var threadObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
        _threadObjectRegistry.RegisterMapping(threadObjectId, id);
        if (_pendingJoinAttempts.Remove(threadObjectId, out var pendingJoins))
        {
            foreach (var (joiningThread, moduleId, methodToken) in pendingJoins)
                ProcessThreadJoinAttempt(joiningThread, threadObjectId, moduleId, methodToken);
        }
        ProcessThreadStartCallback(id, threadObjectId);
        Logger.LogInformation("Thread started {Name}.", Threads.GetValueOrDefault(id, id.ToString()));
    }

    private void OnThreadJoinAttempt(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
    {
        var (id, _, joinedThreadObjectId) = PushSynchronizationContext(metadata, args);
        ProcessThreadJoinAttempt(id, joinedThreadObjectId, args.ModuleId, args.MethodToken);
    }

    private void OnThreadJoinResult(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, args.ModuleId, args.MethodToken);
        var joinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, frameLease.Frame.Arguments![0].Value.AsTrackedObject);
        var joinedThreadId = _threadObjectRegistry.GetThreadId(joinedThreadObjectId);
        ThreadJoinReturned?.Invoke(new ThreadJoinResultArgs(id, joinedThreadId, args.ModuleId, args.MethodToken, IsSuccess: true));
    }

    protected virtual void ProcessThreadStartCore(ProcessThreadId id, ProcessTrackedObjectId threadObjectId)
    {
        ThreadStarting?.Invoke(new ThreadStartingArgs(id, threadObjectId));
    }

    protected virtual void ProcessThreadStartCallback(ProcessThreadId id, ProcessTrackedObjectId threadObjectId)
    {
        ThreadStarted?.Invoke(new ThreadStartArgs(id, threadObjectId));
    }

    protected virtual void ProcessThreadJoinAttempt(
        ProcessThreadId id,
        ProcessTrackedObjectId joinedThreadObjectId,
        ModuleId moduleId,
        MdMethodDef methodToken)
    {
        if (_threadObjectRegistry.TryGetThreadId(joinedThreadObjectId, out var joiningThreadId))
            ThreadJoinAttempted?.Invoke(new ThreadJoinAttemptArgs(id, joiningThreadId, moduleId, methodToken));
        else
        {
            if (!_pendingJoinAttempts.TryGetValue(joinedThreadObjectId, out var pending))
                _pendingJoinAttempts[joinedThreadObjectId] = pending = [];
            pending.Add((id, moduleId, methodToken));
        }
    }

    protected bool TryGetThreadId(ProcessTrackedObjectId threadObjectId, out ProcessThreadId threadId)
        => _threadObjectRegistry.TryGetThreadId(threadObjectId, out threadId);

    protected ProcessThreadId GetThreadIdFromRegistry(ProcessTrackedObjectId threadObjectId)
        => _threadObjectRegistry.GetThreadId(threadObjectId);
}
