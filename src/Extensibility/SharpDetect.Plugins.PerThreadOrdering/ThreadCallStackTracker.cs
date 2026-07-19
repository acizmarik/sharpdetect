// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
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

    public FrameLease PopFrame(ProcessThreadId processThreadId, ModuleId moduleId, MdMethodDef methodToken)
    {
        var callstack = _callstacks[processThreadId];
        var frame = callstack.Peek();
        if (frame.ModuleId != moduleId || frame.MethodToken != methodToken)
            throw new PluginException("Call stack frame does not match the expected method.");

        return new FrameLease(callstack.Pop());
    }

    public void RemoveCallStack(ProcessThreadId processThreadId)
    {
        if (!_callstacks.Remove(processThreadId, out var callstack))
            return;

        foreach (var frame in callstack)
            frame.Arguments?.Dispose();
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
