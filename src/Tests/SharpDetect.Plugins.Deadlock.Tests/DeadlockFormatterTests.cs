// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Plugins.Deadlock.Tests;

public class DeadlockFormatterTests
{
    private const uint ProcessId = 42;

    private static readonly ProcessThreadId First = CreateThreadId(1);
    private static readonly ProcessThreadId Second = CreateThreadId(2);
    private static readonly ProcessThreadId Third = CreateThreadId(3);

    [Fact]
    public void FormatLine_DescribesWhatEachThreadWaitsFor()
    {
        var deadlock = CreateDeadlock(
            CreateLockWait(First, "T2 (Subject 1)", Second, lockId: 7),
            CreateLockWait(Second, "T3 (Subject 2)", First, lockId: 8));

        Assert.Equal(
            "deadlock: T2 (Subject 1) waits for lock #7 held by T3 (Subject 2), T3 (Subject 2) waits for lock #8 held by T2 (Subject 1)",
            DeadlockFormatter.FormatLine(deadlock));
    }

    [Fact]
    public void FormatBlock_ListsEachThreadUnderTheTitle()
    {
        var deadlock = CreateDeadlock(
            CreateLockWait(First, "T1", Second, lockId: 7),
            CreateJoinWait(Second, "T2", First));

        Assert.Equal(
            string.Join(Environment.NewLine,
                "[PID=42] Deadlock between T1 and T2",
                "    T1 waits for lock #7 held by T2",
                "    T2 waits for T1 to finish"),
            DeadlockFormatter.FormatBlock(deadlock));
    }

    [Fact]
    public void FormatTitle_ListsAllThreadsOfALongerCycle()
    {
        var deadlock = CreateDeadlock(
            CreateJoinWait(First, "T1", Second),
            CreateJoinWait(Second, "T2", Third),
            CreateJoinWait(Third, "T3", First));

        Assert.Equal("Deadlock between T1, T2 and T3", DeadlockFormatter.FormatTitle(deadlock));
        Assert.Equal("3 threads are blocked in a cycle.", DeadlockFormatter.FormatDescription(deadlock));
    }

    [Fact]
    public void FormatReason_StartsWithACapital()
    {
        var deadlock = CreateDeadlock(
            CreateJoinWait(First, "T1", Second),
            CreateJoinWait(Second, "T2", First));

        Assert.Equal("Waits for T2 to finish", DeadlockFormatter.FormatReason(deadlock, deadlock.Cycle[0]));
    }

    private static DeadlockInfo CreateDeadlock(params DeadlockThreadInfo[] cycle)
        => new(ProcessId, DateTime.UnixEpoch, [.. cycle]);

    private static DeadlockThreadInfo CreateLockWait(ProcessThreadId thread, string name, ProcessThreadId owner, nuint lockId)
        => new(thread, name, owner, BlockedOnType.Lock, new ProcessTrackedObjectId(ProcessId, new TrackedObjectId(lockId)));

    private static DeadlockThreadInfo CreateJoinWait(ProcessThreadId thread, string name, ProcessThreadId joined)
        => new(thread, name, joined, BlockedOnType.Thread);

    private static ProcessThreadId CreateThreadId(nuint value)
        => new(ProcessId, new ThreadId(value));
}
