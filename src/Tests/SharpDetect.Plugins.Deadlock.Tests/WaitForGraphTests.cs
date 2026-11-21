// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Plugins.Deadlock.Tests;

public class WaitForGraphTests
{
    private const uint Pid = 1;
    private static readonly WaitInfo LockWait = new(BlockedOnType.Lock, null, null);

    [Fact]
    public void DetectDeadlocks_NoCycle_ReturnsNothing()
    {
        var graph = new WaitForGraph(Pid);
        graph.SetThreadWaiting(T(1), T(2), LockWait);
        graph.SetThreadWaiting(T(2), T(3), LockWait);

        Assert.Empty(graph.DetectDeadlocks());
    }

    [Fact]
    public void DetectDeadlocks_TwoThreadCycle_IsInWaitOrder()
    {
        var graph = new WaitForGraph(Pid);
        graph.SetThreadWaiting(T(1), T(2), LockWait);
        graph.SetThreadWaiting(T(2), T(1), LockWait);

        var cycle = Assert.Single(graph.DetectDeadlocks());

        AssertWaitOrder(graph, cycle, expectedLength: 2);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void DetectDeadlocks_LongerCycle_IsInWaitOrder(int length)
    {
        var graph = new WaitForGraph(Pid);
        for (var i = 1; i <= length; i++)
            graph.SetThreadWaiting(T(i), T(i % length + 1), Waiting(i % length + 1));

        var cycle = Assert.Single(graph.DetectDeadlocks());

        AssertWaitOrder(graph, cycle, expectedLength: length);
    }

    [Fact]
    public void DetectDeadlocks_CycleWithTail_ReportsOnlyTheCycle()
    {
        var graph = new WaitForGraph(Pid);
        graph.SetThreadWaiting(T(4), T(1), LockWait);
        graph.SetThreadWaiting(T(1), T(2), LockWait);
        graph.SetThreadWaiting(T(2), T(3), LockWait);
        graph.SetThreadWaiting(T(3), T(1), LockWait);

        var cycle = Assert.Single(graph.DetectDeadlocks());

        Assert.DoesNotContain(T(4), cycle);
        AssertWaitOrder(graph, cycle, expectedLength: 3);
    }

    [Fact]
    public void SetThreadWaiting_EdgeChangesTarget_OldEdgeIsDropped()
    {
        var graph = new WaitForGraph(Pid);
        graph.SetThreadWaiting(T(1), T(2), LockWait);
        graph.SetThreadWaiting(T(2), T(1), LockWait);
        graph.SetThreadWaiting(T(2), T(3), LockWait);

        Assert.Empty(graph.DetectDeadlocks());
    }

    [Fact]
    public void SetThreadWaiting_EdgeChangesTarget_NewCycleIsSimple()
    {
        var graph = new WaitForGraph(Pid);
        graph.SetThreadWaiting(T(1), T(2), LockWait);
        graph.SetThreadWaiting(T(2), T(1), LockWait);
        graph.SetThreadWaiting(T(2), T(3), LockWait);
        graph.SetThreadWaiting(T(3), T(1), LockWait);

        var cycle = Assert.Single(graph.DetectDeadlocks());

        AssertWaitOrder(graph, cycle, expectedLength: 3);
    }

    [Fact]
    public void ClearThreadWaiting_BreaksCycle()
    {
        var graph = new WaitForGraph(Pid);
        graph.SetThreadWaiting(T(1), T(2), LockWait);
        graph.SetThreadWaiting(T(2), T(1), LockWait);
        graph.ClearThreadWaiting(T(2));

        Assert.Empty(graph.DetectDeadlocks());
        Assert.Null(graph.GetWaitInfo(T(2)));
    }

    [Fact]
    public void AreEqualUpToRotation_RotatedCycle_IsEqual()
    {
        int[] cycle = [1, 2, 3];

        Assert.True(Cycles.AreEqualUpToRotation(cycle, [2, 3, 1]));
        Assert.True(Cycles.AreEqualUpToRotation(cycle, [3, 1, 2]));
    }

    [Fact]
    public void AreEqualUpToRotation_ReversedOrDifferentCycle_IsNotEqual()
    {
        int[] cycle = [1, 2, 3];

        Assert.False(Cycles.AreEqualUpToRotation(cycle, [3, 2, 1]));
        Assert.False(Cycles.AreEqualUpToRotation(cycle, [1, 2]));
        Assert.False(Cycles.AreEqualUpToRotation(cycle, [1, 2, 4]));
    }

    private static void AssertWaitOrder(WaitForGraph graph, IReadOnlyList<ProcessThreadId> cycle, int expectedLength)
    {
        Assert.Equal(expectedLength, cycle.Count);
        Assert.Equal(expectedLength, cycle.Distinct().Count());
        for (var i = 0; i < cycle.Count; i++)
        {
            var waitInfo = graph.GetWaitInfo(cycle[i]);
            Assert.NotNull(waitInfo);
            if (waitInfo.BlockedOnProcessThreadId is { } blockedOn)
                Assert.Equal(blockedOn, cycle[(i + 1) % cycle.Count]);
        }
    }

    private static WaitInfo Waiting(int threadId) => new(BlockedOnType.Thread, T(threadId), null);

    private static ProcessThreadId T(int threadId) => new(Pid, new ThreadId((nuint)threadId));
}
