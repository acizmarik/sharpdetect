// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Plugins.PerThreadOrdering;

public sealed class ThreadCallStackTracker
{
    private readonly Dictionary<ProcessThreadId, Callstack> _callstacks = [];
    
    public void InitializeCallStack(ProcessThreadId processThreadId)
    {
        _callstacks[processThreadId] = new Callstack(processThreadId);
    }
    
    public void Push(ProcessThreadId processThreadId, StackFrame frame)
    {
        _callstacks[processThreadId].Push(frame);
    }
    
    public StackFrame Pop(ProcessThreadId processThreadId)
    {
        return _callstacks[processThreadId].Pop();
    }
    
    public StackFrame Peek(ProcessThreadId processThreadId)
    {
        return _callstacks[processThreadId].Peek();
    }

    public IReadOnlyDictionary<ProcessThreadId, Callstack> GetSnapshot()
    {
        return _callstacks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Clone());
    }
    
    public IReadOnlySet<ProcessThreadId> GetThreadIds()
    {
        return _callstacks.Keys.ToHashSet();
    }
}

