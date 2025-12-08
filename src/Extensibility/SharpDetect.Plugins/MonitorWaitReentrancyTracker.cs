// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins;

internal sealed class MonitorWaitReentrancyTracker
{
    private readonly Dictionary<ProcessThreadId, Stack<int>> _reentrancyCounts = [];
    
    public void PushReentrancyCount(ProcessThreadId processThreadId, int reentrancyCount)
    {
        if (!_reentrancyCounts.TryGetValue(processThreadId, out var stack))
        {
            stack = new Stack<int>();
            _reentrancyCounts[processThreadId] = stack;
        }
        
        stack.Push(reentrancyCount);
    }
    
    public int PeekReentrancyCount(ProcessThreadId processThreadId)
    {
        if (!_reentrancyCounts.TryGetValue(processThreadId, out var stack) || stack.Count == 0)
            throw new InvalidOperationException($"No reentrancy count found for thread {processThreadId.ThreadId.Value}");
        
        return stack.Peek();
    }
    
    public int PopReentrancyCount(ProcessThreadId processThreadId)
    {
        if (!_reentrancyCounts.TryGetValue(processThreadId, out var stack) || stack.Count == 0)
            throw new InvalidOperationException($"No reentrancy count found for thread {processThreadId.ThreadId.Value}");
        
        return stack.Pop();
    }
}

