// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
using System.Buffers;
using Xunit;

namespace SharpDetect.Plugins.PerThreadOrdering.Tests;

public class ThreadCallStackTrackerTests
{
    private static readonly ProcessThreadId Thread = new(ProcessId: 1, new ThreadId(1));
    private static readonly ModuleId Module = new(1);
    private static readonly MdMethodDef Method = new(0x06000001);
    private static readonly MdMethodDef OtherMethod = new(0x06000002);

    private readonly ThreadCallStackTracker _tracker = new();

    public ThreadCallStackTrackerTests()
    {
        _tracker.InitializeCallStack(Thread);
    }

    private static RuntimeArgumentList RentArguments()
        => RuntimeArgumentList.Rent(ArrayPool<RuntimeArgumentInfo>.Shared.Rent(1), 1);

    private static StackFrame Frame(MdMethodDef method, RuntimeArgumentList arguments)
        => new(Module, method, arguments);

    [Fact]
    public void PopFrame_MatchingMethod_ReturnsTheFrame()
    {
        var arguments = RentArguments();
        _tracker.Push(Thread, Frame(Method, arguments));

        using var lease = _tracker.PopFrame(Thread, Module, Method);

        Assert.Equal(Method, lease.Frame.MethodToken);
        Assert.Same(arguments, lease.Frame.Arguments);
    }

    [Fact]
    public void PopFrame_DisposingTheLease_DisposesTheArguments()
    {
        var arguments = RentArguments();
        _tracker.Push(Thread, Frame(Method, arguments));

        _tracker.PopFrame(Thread, Module, Method).Dispose();

        Assert.True(arguments.IsDisposed);
    }

    [Fact]
    public void PopFrame_MismatchedMethod_Throws()
    {
        var arguments = RentArguments();
        _tracker.Push(Thread, Frame(Method, arguments));

        Assert.Throws<PluginException>(() => _tracker.PopFrame(Thread, Module, OtherMethod));
    }

    [Fact]
    public void PopFrame_MismatchedMethod_LeavesTheFrameOwnedByTheCallStack()
    {
        var arguments = RentArguments();
        _tracker.Push(Thread, Frame(Method, arguments));

        Assert.Throws<PluginException>(() => _tracker.PopFrame(Thread, Module, OtherMethod));

        Assert.False(arguments.IsDisposed);
        _tracker.RemoveCallStack(Thread);
        Assert.True(arguments.IsDisposed);
    }

    [Fact]
    public void RemoveCallStack_DisposesArgumentsOfEveryPendingFrame()
    {
        var outer = RentArguments();
        var inner = RentArguments();
        _tracker.Push(Thread, Frame(Method, outer));
        _tracker.Push(Thread, Frame(OtherMethod, inner));

        _tracker.RemoveCallStack(Thread);

        Assert.True(outer.IsDisposed);
        Assert.True(inner.IsDisposed);
    }

    [Fact]
    public void RemoveCallStack_DropsTheThread()
    {
        _tracker.RemoveCallStack(Thread);

        Assert.DoesNotContain(Thread, _tracker.GetThreadIds());
    }

    [Fact]
    public void RemoveCallStack_UnknownThread_IsANoOp()
    {
        _tracker.RemoveCallStack(new ProcessThreadId(1, new ThreadId(99)));
    }

    [Fact]
    public void RemoveCallStack_FrameWithoutArguments_IsANoOp()
    {
        _tracker.Push(Thread, new StackFrame(Module, Method, null));

        _tracker.RemoveCallStack(Thread);
    }
}
