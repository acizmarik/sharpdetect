// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Core.Tests.Plugins;

public class PluginBaseThreadTests
{
    private const uint Pid = 1;

    [Fact]
    public void FindThreadName_DestroyedThread_KeepsName()
    {
        var plugin = new ThreadTrackingPlugin();
        var main = new ThreadId(1);
        var worker = new ThreadId(2);

        plugin.Visit(new RecordedEventMetadata(Pid, main), new ThreadCreateRecordedEvent(main));
        plugin.Visit(new RecordedEventMetadata(Pid, worker), new ThreadCreateRecordedEvent(worker));
        plugin.Visit(new RecordedEventMetadata(Pid, main), new ThreadDestroyRecordedEvent(worker));

        var id = new ProcessThreadId(Pid, worker);
        Assert.Equal("T1", plugin.FindName(id));
        Assert.False(plugin.LiveThreads.ContainsKey(id));
    }

    [Fact]
    public void FindThreadName_DestroyedRenamedThread_KeepsRenamedName()
    {
        var plugin = new ThreadTrackingPlugin();
        var worker = new ThreadId(7);

        plugin.Visit(new RecordedEventMetadata(Pid, worker), new ThreadCreateRecordedEvent(worker));
        plugin.Visit(new RecordedEventMetadata(Pid, worker), new ThreadRenameRecordedEvent(worker, "Worker"));
        plugin.Visit(new RecordedEventMetadata(Pid, worker), new ThreadDestroyRecordedEvent(worker));

        Assert.Equal("T0 (Worker)", plugin.FindName(new ProcessThreadId(Pid, worker)));
    }

    [Fact]
    public void FindThreadName_LiveThread_ReturnsName()
    {
        var plugin = new ThreadTrackingPlugin();
        var main = new ThreadId(1);

        plugin.Visit(new RecordedEventMetadata(Pid, main), new ThreadCreateRecordedEvent(main));

        Assert.Equal("T0", plugin.FindName(new ProcessThreadId(Pid, main)));
    }

    [Fact]
    public void FindThreadName_UnknownThread_ReturnsNull()
    {
        var plugin = new ThreadTrackingPlugin();

        Assert.Null(plugin.FindName(new ProcessThreadId(Pid, new ThreadId(99))));
    }

    [Fact]
    public void ThreadDestroy_UnknownThread_IsIgnored()
    {
        var plugin = new ThreadTrackingPlugin();
        var unknown = new ThreadId(99);

        var exception = Record.Exception(() =>
            plugin.Visit(new RecordedEventMetadata(Pid, unknown), new ThreadDestroyRecordedEvent(unknown)));

        Assert.Null(exception);
        Assert.Null(plugin.FindName(new ProcessThreadId(Pid, unknown)));
    }

    private sealed class ThreadTrackingPlugin() : PluginBase(
        moduleBindContext: null!,
        metadataContext: null!,
        symbolResolver: null!,
        profilerCommandSenderProvider: null!,
        TimeProvider.System,
        NullLogger.Instance)
    {
        public override ProfilerConfiguration ProfilerConfiguration => throw new NotSupportedException();

        public IReadOnlyDictionary<ProcessThreadId, string> LiveThreads => Threads;

        public string? FindName(ProcessThreadId id) => FindThreadName(id);
    }
}
