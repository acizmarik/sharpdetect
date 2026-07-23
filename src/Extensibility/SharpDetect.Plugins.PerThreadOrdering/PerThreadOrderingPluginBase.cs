// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Loader;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Serialization;

namespace SharpDetect.Plugins.PerThreadOrdering;

public abstract partial class PerThreadOrderingPluginBase : PluginBase
{
    private readonly ThreadCallStackTracker _callStackTracker = new();
    private readonly IArgumentsParser _argumentsParser;

    protected PerThreadOrderingPluginBase(
        IModuleBindContext moduleBindContext,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver,
        IArgumentsParser argumentsParser,
        IProfilerCommandSenderProvider profilerCommandSenderProvider,
        TimeProvider timeProvider,
        ILogger logger)
        : base(moduleBindContext, metadataContext, symbolResolver, profilerCommandSenderProvider, timeProvider, logger)
    {
        _argumentsParser = argumentsParser;
        RegisterMonitorBindings();
        RegisterSemaphoreBindings();
        RegisterWaitHandleBindings();
        RegisterThreadBindings();
        RegisterTaskBindings();
        RegisterFieldAccessBindings();
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
    {
        _callStackTracker.InitializeCallStack(new ProcessThreadId(metadata.Pid, args.ThreadId));
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, ThreadDestroyRecordedEvent args)
    {
        _callStackTracker.RemoveCallStack(new ProcessThreadId(metadata.Pid, args.ThreadId));
        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, GarbageCollectedTrackedObjectsRecordedEvent args)
    {
        if (_waitAsyncTaskToSemaphore.Count > 0)
        {
            foreach (var objectId in args.RemovedTrackedObjectIds)
                _waitAsyncTaskToSemaphore.Remove(new ProcessTrackedObjectId(metadata.Pid, objectId));
        }

        if (_waitHandleKinds.Count > 0)
        {
            foreach (var objectId in args.RemovedTrackedObjectIds)
                _waitHandleKinds.Remove(new ProcessTrackedObjectId(metadata.Pid, objectId));
        }

        if (_lastMutexAcquirers.Count > 0)
        {
            foreach (var objectId in args.RemovedTrackedObjectIds)
                _lastMutexAcquirers.Remove(new ProcessTrackedObjectId(metadata.Pid, objectId));
        }

        base.Visit(metadata, args);
    }

    protected override void Visit(RecordedEventMetadata metadata, MethodUnwoundRecordedEvent args)
    {
        // The instrumented method exited via an exception, so no method-exit event was produced
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, args.ModuleId, args.MethodToken);
        var frame = frameLease.Frame;

        switch ((RecordedEventType)args.Interpretation)
        {
            case RecordedEventType.MonitorLockAcquireResult:
            case RecordedEventType.LockAcquireResult:
            {
                var lockId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                LockAcquireReturned?.Invoke(new(id, args.ModuleId, args.MethodToken, lockId, IsSuccess: false));
                break;
            }

            case RecordedEventType.SemaphoreAcquireResult:
            {
                var semaphoreId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                SemaphoreAcquireReturned?.Invoke(new(id, args.ModuleId, args.MethodToken, semaphoreId, IsSuccess: false));
                break;
            }

            case RecordedEventType.WaitHandleWaitResult:
            {
                var handleId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                if (_pendingAbandonedMutexes.Remove(id))
                    ProcessAbandonedMutexAcquireReturn(id, args.ModuleId, args.MethodToken, handleId);
                else
                    DispatchWaitHandleWaitResult(id, args.ModuleId, args.MethodToken, handleId, isSuccess: false);
                break;
            }

            case RecordedEventType.WaitHandleSignalAndWaitResult:
            {
                var awaitedHandleId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments![1].Value.AsTrackedObject);
                if (_pendingAbandonedMutexes.Remove(id))
                    ProcessAbandonedMutexAcquireReturn(id, args.ModuleId, args.MethodToken, awaitedHandleId);
                else
                    DispatchWaitHandleWaitResult(id, args.ModuleId, args.MethodToken, awaitedHandleId, isSuccess: false);
                break;
            }

            case RecordedEventType.WaitHandleWaitMultipleResult:
            {
                if (!_pendingAbandonedMutexes.Remove(id, out var abandonedHandle))
                    break;

                var waitAll = (bool)frame.Arguments![1].Value.AsPrimitive;
                if (waitAll)
                {
                    var handles = frame.Arguments![0].Value.AsTrackedObjectArray;
                    foreach (var handle in handles)
                        DispatchWaitHandleWaitResult(id, args.ModuleId, args.MethodToken, new ProcessTrackedObjectId(id.ProcessId, handle), isSuccess: true);
                }
                else if (abandonedHandle is { } mutexId)
                {
                    ProcessAbandonedMutexAcquireReturn(id, args.ModuleId, args.MethodToken, mutexId);
                }

                break;
            }

            case RecordedEventType.MonitorWaitResult:
            {
                var lockId = ExtractSynchronizationObjectIdFromFrame(id, frame);
                OnAfterWaitReturn(id, lockId);
                ObjectWaitReturned?.Invoke(new ObjectWaitResultArgs(id, args.ModuleId, args.MethodToken, lockId, IsSuccess: false));
                break;
            }

            case RecordedEventType.ThreadJoinResult:
            {
                var joinedThreadObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments![0].Value.AsTrackedObject);
                if (_threadObjectRegistry.TryGetThreadId(joinedThreadObjectId, out var joinedThreadId))
                    ThreadJoinReturned?.Invoke(new ThreadJoinResultArgs(id, joinedThreadId, args.ModuleId, args.MethodToken, IsSuccess: false));
                break;
            }

            case RecordedEventType.TaskComplete:
            {
                // A task whose body faulted completes and release its waiters.
                var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments![0].Value.AsTrackedObject);
                ProcessTaskComplete(id, taskObjectId);
                break;
            }

            case RecordedEventType.TaskJoinFinish:
            {
                var taskObjectId = new ProcessTrackedObjectId(id.ProcessId, frame.Arguments![0].Value.AsTrackedObject);
                if (_waitAsyncTaskToSemaphore.Remove(taskObjectId))
                    break;

                ProcessTaskJoinFinish(id, taskObjectId, isSuccess: false);
                break;
            }
        }
    }

    private (ProcessThreadId Id, RuntimeArgumentList Arguments, ProcessTrackedObjectId SynchronizationObjectId) ExtractSynchronizationContext(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = ParseArguments(metadata, args);
        try
        {
            var synchronizationObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
            return (id, arguments, synchronizationObjectId);
        }
        catch
        {
            arguments.Dispose();
            throw;
        }
    }

    private (ProcessThreadId Id, RuntimeArgumentList Arguments, ProcessTrackedObjectId SynchronizationObjectId) PushSynchronizationContext(
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        var arguments = PushArgumentsOnCallStack(id, metadata, args);
        var synchronizationObjectId = new ProcessTrackedObjectId(id.ProcessId, arguments[0].Value.AsTrackedObject);
        return (id, arguments, synchronizationObjectId);
    }

    private (ProcessThreadId Id, ProcessTrackedObjectId LockId) PopLockContext(
        RecordedEventMetadata metadata,
        MethodExitRecordedEvent args)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        using var frameLease = _callStackTracker.PopFrame(id, args.ModuleId, args.MethodToken);
        var lockId = ExtractSynchronizationObjectIdFromFrame(id, frameLease.Frame);
        return (id, lockId);
    }

    private void PopFrame(RecordedEventMetadata metadata, ModuleId moduleId, MdMethodDef methodToken)
    {
        var id = new ProcessThreadId(metadata.Pid, metadata.Tid);
        _callStackTracker.PopFrame(id, moduleId, methodToken).Dispose();
    }

    private static ProcessTrackedObjectId ExtractSynchronizationObjectIdFromFrame(ProcessThreadId id, StackFrame frame)
        => new(id.ProcessId, frame.Arguments![0].Value.AsTrackedObject);

    private void PushArgumentsOnCallStack(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => PushArgumentsOnCallStack(new ProcessThreadId(metadata.Pid, metadata.Tid), metadata, args);

    private RuntimeArgumentList PushArgumentsOnCallStack(
        ProcessThreadId id,
        RecordedEventMetadata metadata,
        MethodEnterWithArgumentsRecordedEvent args)
    {
        var arguments = ParseArguments(metadata, args);
        try
        {
            _callStackTracker.Push(id, new StackFrame(args.ModuleId, args.MethodToken, arguments));
        }
        catch
        {
            arguments.Dispose();
            throw;
        }

        return arguments;
    }

    private RuntimeArgumentList ParseArguments(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => ParseArgumentsCore(metadata, args.ModuleId, args.MethodToken, args.ArgumentValues, args.ArgumentInfos);

    private RuntimeArgumentList ParseArguments(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => ParseArgumentsCore(metadata, args.ModuleId, args.MethodToken, args.ByRefArgumentValues, args.ByRefArgumentInfos);

    private RuntimeArgumentList ParseArgumentsCore(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> infos)
    {
        var result = _argumentsParser.ParseArguments(metadata, moduleId, methodToken, values, infos);
        return !result.IsError
            ? result.Value
            : throw new PluginException($"Could not parse arguments for method {methodToken} from module {moduleId.Value}");
    }
}
