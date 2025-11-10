// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using System.Collections;

namespace SharpDetect.Core.Plugins.Models;

public class Callstack(ProcessThreadId processThreadId) : IReadOnlyCollection<StackFrame>
{
    public ProcessThreadId ProcessThreadId { get; } = processThreadId;
    private readonly Stack<StackFrame> stack = [];

    public int Count => stack.Count;

    public void Push(ModuleId moduleId, MdMethodDef methodToken)
        => stack.Push(new(moduleId, methodToken));

    public StackFrame Pop()
        => stack.Pop();

    public IEnumerator<StackFrame> GetEnumerator()
        => stack.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public Callstack Clone()
    {
        var copy = new Callstack(ProcessThreadId);
        foreach (var item in stack.Reverse())
            copy.Push(item.ModuleId, item.MethodToken);
        return copy;
    }
}
