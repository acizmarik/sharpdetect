// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using SharpDetect.Core.Commands;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins.Deadlock;

public partial class DeadlockPlugin
{
    private void CheckForDeadlocks(uint processId)
    {
        var graph = _waitForGraphs.GetValueOrDefault(processId);
        if (graph == null)
        {
            graph = new WaitForGraph(processId);
            _waitForGraphs[processId] = graph;
        }

        UpdateWaitForGraph(processId, graph);
        foreach (var deadlock in graph.DetectDeadlocks().Select(cycle => ConstructDeadlockInfo(processId, cycle, graph)))
            RecordDeadlockInfo(deadlock);
    }

    private void UpdateWaitForGraph(uint processId, WaitForGraph graph)
    {
        var threads = Threads.Keys.Where(t => t.ProcessId == processId);
        
        foreach (var processThreadId in threads)
        {
            if (TryGetThreadBlockedOnLockInfo(processThreadId, out var waitInfo, out var blockedOnThreadId) ||
                TryGetThreadThreadBlockedOnJoinInfo(processThreadId, out waitInfo, out blockedOnThreadId))
            {
                graph.SetThreadWaiting(processThreadId, waitingFor: blockedOnThreadId.Value, waitInfo);
            }
            else
            {
                graph.ClearThreadWaiting(processThreadId);
            }
        }
    }
    
    private bool TryGetThreadBlockedOnLockInfo(
        ProcessThreadId processThreadId,
        [NotNullWhen(true)] out WaitInfo? waitInfo,
        [NotNullWhen(true)] out ProcessThreadId? lockOwnerThreadId)
    {
        if (_concurrencyContext.TryGetWaitingLock(processThreadId, out var blockedLockId) &&
            _concurrencyContext.TryGetLockOwner(blockedLockId.Value, out var lockOwner) &&
            lockOwner != processThreadId)
        {
            waitInfo = new WaitInfo(
                BlockedOnType.Lock,
                lockOwner,
                blockedLockId.Value);
            lockOwnerThreadId = lockOwner;
            return true;
        }

        waitInfo = null;
        lockOwnerThreadId = null;
        return false;
    }
    
    private bool TryGetThreadThreadBlockedOnJoinInfo(
        ProcessThreadId threadId,
        [NotNullWhen(true)] out WaitInfo? waitInfo,
        [NotNullWhen(true)] out ProcessThreadId? joiningThreadId)
    {
        if (_concurrencyContext.TryGetWaitingThread(threadId, out joiningThreadId))
        {
            waitInfo = new WaitInfo(
                BlockedOnType.Thread,
                joiningThreadId,
                null);
            return true;
        }
        
        waitInfo = null;
        joiningThreadId = null;
        return false;
    }
    
    private DeadlockInfo ConstructDeadlockInfo(uint processId, ImmutableArray<ProcessThreadId> cycle, WaitForGraph graph)
    {
        var cycleLength = cycle.Length;
        var threadInfos = new List<DeadlockThreadInfo>(cycleLength);
        
        for (var i = 0; i < cycleLength; i++)
        {
            var currentThread = cycle[i];
            var nextThread = cycle[(i + 1) % cycleLength];
            var threadInfo = CreateDeadlockThreadInfo(currentThread, nextThread, graph);
            threadInfos.Add(threadInfo);
        }

        return new DeadlockInfo(
            ProcessId: processId,
            TimeStamp: _timeProvider.GetUtcNow().DateTime,
            Cycle: threadInfos);
    }
    
    private DeadlockThreadInfo CreateDeadlockThreadInfo(
        ProcessThreadId currentProcessThreadId,
        ProcessThreadId nextProcessThreadId,
        WaitForGraph graph)
    {
        var waitInfo = graph.GetWaitInfo(currentProcessThreadId) ?? throw new InvalidOperationException(
                $"Thread {currentProcessThreadId} is in a cycle but has no wait information.");

        var threadName = Threads.TryGetValue(currentProcessThreadId, out var name) ? name : $"Thread-{currentProcessThreadId.ThreadId.Value}";
        return new DeadlockThreadInfo(
            ProcessThreadId: currentProcessThreadId,
            ThreadName: threadName,
            BlockedOn: nextProcessThreadId,
            BlockedOnType: waitInfo.BlockedOnType,
            ProcessLockObjectId: waitInfo.ProcessLockObjectId);
    }
    
    private void RecordDeadlockInfo(DeadlockInfo deadlock)
    {
        if (IsDeadlockAlreadyRecorded(deadlock))
            return;
        
        var commandSender = GetCommandSender(deadlock.ProcessId);
        var threadIds = deadlock.Cycle.Select(t => t.ProcessThreadId.ThreadId).ToArray();
        var commandId = commandSender.SendCommand(new CreateStackTraceSnapshotsCommand(threadIds));
        
        _deadlocks.Add((deadlock.ProcessId, commandId.Value), deadlock);

        Logger.LogWarning(
            "[PID={Pid}] Deadlock detected (affects {ThreadsCount} threads).", 
            deadlock.ProcessId, 
            deadlock.Cycle.Count);
    }
    
    private bool IsDeadlockAlreadyRecorded(DeadlockInfo deadlock)
    {
        return _deadlocks.Any(d => 
            d.Key.Pid == deadlock.ProcessId && 
            d.Value.Cycle.SequenceEqual(deadlock.Cycle));
    }
}