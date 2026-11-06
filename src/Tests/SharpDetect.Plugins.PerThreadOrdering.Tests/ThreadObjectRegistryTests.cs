// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Plugins.PerThreadOrdering.Tests;

public class ThreadObjectRegistryTests
{
    private static readonly ProcessThreadId Thread1 = new(ProcessId: 1, new ThreadId(1));
    private static readonly ProcessThreadId Thread2 = new(ProcessId: 1, new ThreadId(2));
    private static readonly ProcessTrackedObjectId Thread1Object = new(ProcessId: 1, new TrackedObjectId(10));
    private static readonly ProcessTrackedObjectId Thread2Object = new(ProcessId: 1, new TrackedObjectId(11));

    private readonly ThreadObjectRegistry _registry = new();

    [Fact]
    public void RemovedThreadObject_IsNoLongerResolved()
    {
        _registry.RegisterMapping(Thread1Object, Thread1);
        _registry.RegisterMapping(Thread2Object, Thread2);

        _registry.Remove(Thread1Object);

        Assert.False(_registry.TryGetThreadId(Thread1Object, out _));
        Assert.True(_registry.TryGetThreadId(Thread2Object, out var thread2));
        Assert.Equal(Thread2, thread2);
    }

    [Fact]
    public void RemovingUnknownThreadObject_DoesNothing()
    {
        _registry.RegisterMapping(Thread1Object, Thread1);

        _registry.Remove(Thread2Object);

        Assert.True(_registry.TryGetThreadId(Thread1Object, out var thread1));
        Assert.Equal(Thread1, thread1);
    }
}
