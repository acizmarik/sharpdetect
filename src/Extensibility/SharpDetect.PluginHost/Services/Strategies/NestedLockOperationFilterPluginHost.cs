// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;

namespace SharpDetect.PluginHost.Services.Strategies;

internal sealed class NestedLockOperationFilterPluginHost(IPluginHost inner) : IPluginHost, IDisposable
{
    private const ushort FirstRuntimeWithNestedLockOperations = 11;

    private enum ActiveOperation
    {
        ForwardedLockOperation,
        DiscardedLockOperation,
        WaitHandleWait
    }

    private readonly HashSet<uint> _filteredProcesses = [];
    private readonly Dictionary<ProcessThreadId, Stack<ActiveOperation>> _activeOperations = [];
    private bool _isDisposed;

    public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
    {
        if (recordedEvent.EventArgs is ProfilerLoadRecordedEvent { MajorVersion: >= FirstRuntimeWithNestedLockOperations })
            _filteredProcesses.Add(recordedEvent.Metadata.Pid);
        
        if (!_filteredProcesses.Contains(recordedEvent.Metadata.Pid))
            return inner.ProcessEvent(recordedEvent);

        var tid = new ProcessThreadId(recordedEvent.Metadata.Pid, recordedEvent.Metadata.Tid);
        switch (recordedEvent.EventArgs)
        {
            case MethodEnterRecordedEvent enter when IsLockRelease(enter.Interpretation):
            case MethodEnterWithArgumentsRecordedEvent enterWithArguments when IsLockRelease(enterWithArguments.Interpretation):
                return IsNestedInLockOperation(tid) ? RecordedEventState.Discarded : inner.ProcessEvent(recordedEvent);

            case MethodEnterRecordedEvent enter when IsLockOperationEnter(enter.Interpretation):
            case MethodEnterWithArgumentsRecordedEvent enterWithArguments when IsLockOperationEnter(enterWithArguments.Interpretation):
            {
                var isNested = IsNestedInLockOperation(tid);
                GetOrCreateStack(tid).Push(isNested ? ActiveOperation.DiscardedLockOperation : ActiveOperation.ForwardedLockOperation);
                return isNested ? RecordedEventState.Discarded : inner.ProcessEvent(recordedEvent);
            }

            case MethodExitRecordedEvent exit when IsLockOperationExit(exit.Interpretation):
            case MethodExitWithArgumentsRecordedEvent exitWithArguments when IsLockOperationExit(exitWithArguments.Interpretation):
            case MethodUnwoundRecordedEvent unwound when IsLockOperationExit(unwound.Interpretation):
            {
                if (_activeOperations.TryGetValue(tid, out var stack) &&
                    stack.TryPeek(out var active) &&
                    active != ActiveOperation.WaitHandleWait)
                {
                    stack.Pop();
                    if (active == ActiveOperation.DiscardedLockOperation)
                        return RecordedEventState.Discarded;
                }

                return inner.ProcessEvent(recordedEvent);
            }

            case MethodEnterRecordedEvent enter when IsWaitHandleWaitEnter(enter.Interpretation):
            case MethodEnterWithArgumentsRecordedEvent enterWithArguments when IsWaitHandleWaitEnter(enterWithArguments.Interpretation):
            {
                if (_activeOperations.TryGetValue(tid, out var stack) && stack.Count > 0)
                    stack.Push(ActiveOperation.WaitHandleWait);

                return inner.ProcessEvent(recordedEvent);
            }

            case MethodExitRecordedEvent exit when IsWaitHandleWaitExit(exit.Interpretation):
            case MethodExitWithArgumentsRecordedEvent exitWithArguments when IsWaitHandleWaitExit(exitWithArguments.Interpretation):
            case MethodUnwoundRecordedEvent unwound when IsWaitHandleWaitExit(unwound.Interpretation):
            {
                if (_activeOperations.TryGetValue(tid, out var stack) &&
                    stack.TryPeek(out var active) &&
                    active == ActiveOperation.WaitHandleWait)
                {
                    stack.Pop();
                }

                return inner.ProcessEvent(recordedEvent);
            }

            case ThreadDestroyRecordedEvent destroy:
                _activeOperations.Remove(new ProcessThreadId(recordedEvent.Metadata.Pid, destroy.ThreadId));
                return inner.ProcessEvent(recordedEvent);

            default:
                return inner.ProcessEvent(recordedEvent);
        }
    }

    private bool IsNestedInLockOperation(ProcessThreadId tid)
    {
        return _activeOperations.TryGetValue(tid, out var stack) &&
            stack.TryPeek(out var enclosing) &&
            enclosing != ActiveOperation.WaitHandleWait;
    }

    private Stack<ActiveOperation> GetOrCreateStack(ProcessThreadId tid)
    {
        if (!_activeOperations.TryGetValue(tid, out var stack))
            _activeOperations[tid] = stack = new Stack<ActiveOperation>();
        return stack;
    }

    private static bool IsLockOperationEnter(ushort interpretation)
        => (RecordedEventType)interpretation is
            RecordedEventType.LockAcquire or
            RecordedEventType.LockTryAcquire or
            RecordedEventType.MonitorWaitAttempt or
            RecordedEventType.MonitorPulseOneAttempt or
            RecordedEventType.MonitorPulseAllAttempt;

    private static bool IsLockOperationExit(ushort interpretation)
        => (RecordedEventType)interpretation is
            RecordedEventType.LockAcquireResult or
            RecordedEventType.MonitorWaitResult or
            RecordedEventType.MonitorPulseOneResult or
            RecordedEventType.MonitorPulseAllResult;

    private static bool IsLockRelease(ushort interpretation)
        => (RecordedEventType)interpretation is RecordedEventType.LockRelease;

    private static bool IsWaitHandleWaitEnter(ushort interpretation)
        => (RecordedEventType)interpretation is
            RecordedEventType.WaitHandleWait or
            RecordedEventType.WaitHandleWaitMultiple or
            RecordedEventType.WaitHandleSignalAndWait;

    private static bool IsWaitHandleWaitExit(ushort interpretation)
        => (RecordedEventType)interpretation is
            RecordedEventType.WaitHandleWaitResult or
            RecordedEventType.WaitHandleWaitMultipleResult or
            RecordedEventType.WaitHandleSignalAndWaitResult;

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        if (inner is IDisposable disposable)
            disposable.Dispose();
    }
}
