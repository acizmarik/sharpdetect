// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;

namespace SharpDetect.Plugins;

internal sealed class ThreadCallStackTracker
{
    private readonly Dictionary<ProcessThreadId, Stack<HappensBeforeOrderingPluginBase.CallstackFrame>> _callstacks = [];
    
    public void InitializeCallStack(ProcessThreadId processThreadId)
    {
        _callstacks[processThreadId] = new Stack<HappensBeforeOrderingPluginBase.CallstackFrame>();
    }
    
    public void Push(ProcessThreadId processThreadId, HappensBeforeOrderingPluginBase.CallstackFrame frame)
    {
        _callstacks[processThreadId].Push(frame);
    }
    
    public HappensBeforeOrderingPluginBase.CallstackFrame Pop(ProcessThreadId processThreadId)
    {
        return _callstacks[processThreadId].Pop();
    }
    
    public HappensBeforeOrderingPluginBase.CallstackFrame Peek(ProcessThreadId processThreadId)
    {
        return _callstacks[processThreadId].Peek();
    }
    
    public IReadOnlyDictionary<ProcessThreadId, Stack<HappensBeforeOrderingPluginBase.CallstackFrame>> GetSnapshot()
    {
        return _callstacks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
    
    public IReadOnlySet<ProcessThreadId> GetThreadIds()
    {
        return _callstacks.Keys.ToHashSet();
    }
}

