// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Deadlock;

public static class DeadlockFormatter
{
    public static string FormatTitle(DeadlockInfo deadlock)
    {
        var names = deadlock.Cycle.Select(thread => thread.ThreadName).ToArray();
        var threads = names.Length <= 2
            ? string.Join(" and ", names)
            : $"{string.Join(", ", names[..^1])} and {names[^1]}";
        return $"Deadlock between {threads}";
    }

    public static string FormatDescription(DeadlockInfo deadlock)
    {
        return $"{deadlock.Cycle.Count} threads are blocked in a cycle.";
    }

    public static string FormatReason(DeadlockInfo deadlock, DeadlockThreadInfo thread)
    {
        var wait = FormatWait(deadlock, thread);
        return char.ToUpperInvariant(wait[0]) + wait[1..];
    }

    public static string FormatLine(DeadlockInfo deadlock)
    {
        return "deadlock: " + string.Join(", ", deadlock.Cycle.Select(thread => $"{thread.ThreadName} {FormatWait(deadlock, thread)}"));
    }

    public static string FormatBlock(DeadlockInfo deadlock)
    {
        var lines = deadlock.Cycle.Select(thread => $"    {thread.ThreadName} {FormatWait(deadlock, thread)}");
        return $"[PID={deadlock.ProcessId}] {FormatTitle(deadlock)}{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }
    
    private static string FormatWait(DeadlockInfo deadlock, DeadlockThreadInfo thread)
    {
        var blockedOnName = deadlock.Cycle.First(other => other.ProcessThreadId == thread.BlockedOn).ThreadName;
        return thread.BlockedOnType switch
        {
            BlockedOnType.Lock => $"waits for lock #{thread.ProcessLockObjectId!.Value.ObjectId.Value} held by {blockedOnName}",
            BlockedOnType.Thread => $"waits for {blockedOnName} to finish",
            _ => throw new InvalidOperationException($"Unknown blocking type: {thread.BlockedOnType}")
        };
    }
}
