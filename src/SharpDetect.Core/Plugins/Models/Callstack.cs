// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using System.Collections;
using SharpDetect.Core.Events;

namespace SharpDetect.Core.Plugins.Models;

public class Callstack(ProcessThreadId processThreadId) : IReadOnlyCollection<StackFrame>
{
    public ProcessThreadId ProcessThreadId { get; } = processThreadId;
    private readonly Stack<StackFrame> _stack = [];

    public int Count => _stack.Count;

    public void Push(StackFrame frame)
        => _stack.Push(frame);
    
    public void Push(ModuleId moduleId, MdMethodDef methodToken)
        => Push(new StackFrame(moduleId, methodToken, null));
    
    public void Push(ModuleId moduleId, MdMethodDef methodToken, RuntimeArgumentList arguments)
        => Push(new StackFrame(moduleId, methodToken, arguments));

    public StackFrame Pop()
        => _stack.Pop();
    
    public StackFrame Peek()
        => _stack.Peek();

    public IEnumerator<StackFrame> GetEnumerator()
        => _stack.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Callstack Clone()
    {
        var copy = new Callstack(ProcessThreadId);
        foreach (var item in _stack.Reverse())
            copy.Push(item.ModuleId, item.MethodToken);
        return copy;
    }
}
