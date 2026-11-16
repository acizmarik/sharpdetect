// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.PluginHost.Services.Strategies;
using Xunit;

namespace SharpDetect.PluginHost.Tests;

public class PassthroughPluginHostTests
{
    private const uint NativeThread = 0;
    private const uint ManagedThread = 1;

    public static TheoryData<IRecordedEventArgs> GarbageCollectionEvents => new()
    {
        new GarbageCollectionStartRecordedEvent(),
        new GarbageCollectedTrackedObjectsRecordedEvent([new TrackedObjectId(0x1000)]),
        new FinalizationQueuedTrackedObjectsRecordedEvent([new TrackedObjectId(0x1000)]),
        new GarbageCollectionFinishRecordedEvent(OldTrackedObjectsCount: 1, NewTrackedObjectsCount: 0)
    };

    [Theory]
    [MemberData(nameof(GarbageCollectionEvents))]
    public void PassthroughPluginHost_GarbageCollectionEventFromNativeThread_ReachesPlugin(IRecordedEventArgs args)
    {
        // Arrange
        var plugin = new RecordingPlugin();
        using var host = new PassthroughPluginHost(plugin, NullLogger<PassthroughPluginHost>.Instance);

        // Act
        var state = host.ProcessEvent(new RecordedEvent(SyncEventBuilder.Meta(NativeThread), args));

        // Assert
        Assert.Equal(RecordedEventState.Executed, state);
        Assert.Equal([args], plugin.Visitor.Visited);
    }

    [Fact]
    public void PassthroughPluginHost_GarbageCollectionEventFromManagedThread_ReachesPlugin()
    {
        // Arrange
        var plugin = new RecordingPlugin();
        using var host = new PassthroughPluginHost(plugin, NullLogger<PassthroughPluginHost>.Instance);
        var collected = SyncEventBuilder.GarbageCollected(ManagedThread, 0x1000);

        // Act
        var state = host.ProcessEvent(collected);

        // Assert
        Assert.Equal(RecordedEventState.Executed, state);
        Assert.Equal([collected.EventArgs], plugin.Visitor.Visited);
    }

    [Fact]
    public void PassthroughPluginHost_ThreadBoundEventFromNativeThread_IsDiscarded()
    {
        // Arrange
        var plugin = new RecordingPlugin();
        using var host = new PassthroughPluginHost(plugin, NullLogger<PassthroughPluginHost>.Instance);

        // Act
        var state = host.ProcessEvent(SyncEventBuilder.FieldRead(NativeThread));

        // Assert
        Assert.Equal(RecordedEventState.Discarded, state);
        Assert.Empty(plugin.Visitor.Visited);
    }

    private sealed class RecordingVisitor : RecordedEventActionVisitorBase
    {
        public List<IRecordedEventArgs> Visited { get; } = [];

        protected override void DefaultVisit(RecordedEventMetadata metadata, IRecordedEventArgs args)
            => Visited.Add(args);
    }

    private sealed class RecordingPlugin : IPlugin
    {
        public RecordingVisitor Visitor { get; } = new();
        public string ReportCategory => nameof(RecordingPlugin);
        public DirectoryInfo ReportTemplates => throw new NotSupportedException();
        public ProfilerConfiguration ProfilerConfiguration => throw new NotSupportedException();
        public RecordedEventActionVisitorBase EventsVisitor => Visitor;
        public IReadOnlyDictionary<RecordedEventHandlerType, RecordedEventHandler> CustomEventHandlers { get; }
            = new Dictionary<RecordedEventHandlerType, RecordedEventHandler>();

        public Summary CreateDiagnostics() => throw new NotSupportedException();
        public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports) => throw new NotSupportedException();
    }
}
