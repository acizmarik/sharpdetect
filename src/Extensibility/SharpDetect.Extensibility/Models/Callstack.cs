// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events.Descriptors.Profiler;
using System.Collections;

namespace SharpDetect.Extensibility.Models;

public class Callstack(uint pid, ThreadId tid) : IReadOnlyCollection<StackFrame>
{
    public readonly uint Pid = pid;
    public readonly ThreadId Tid = tid;
    private readonly Stack<StackFrame> stack = [];

    public int Count => stack.Count;

    public void Push(ModuleId moduleId, MdMethodDef methodToken)
        => stack.Push(new(moduleId, methodToken));

    public StackFrame Pop()
        => stack.Pop();

    public CallStackSnapshot CreateSnapshot()
    {
        var projection = stack.Select(sf => (sf.ModuleId, sf.MethodToken));
        var snapshotCallStack = new Stack<(ModuleId ModuleId, MdMethodDef MethodToken)>(projection);
        return new CallStackSnapshot(Pid, Tid, snapshotCallStack);
    }

    public IEnumerator<StackFrame> GetEnumerator()
        => stack.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
