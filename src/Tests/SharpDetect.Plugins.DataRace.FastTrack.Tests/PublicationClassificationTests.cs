// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class PublicationClassificationTests
{
    private readonly TestMetadata _metadata = new();
    private readonly FastTrackDetector _detector;
    private static readonly ProcessThreadId Publisher = new(TestMetadata.ProcessId, new ThreadId(1));
    private static readonly ProcessThreadId Observer = new(TestMetadata.ProcessId, new ThreadId(2));
    private static readonly ProcessThreadId Mutator = new(TestMetadata.ProcessId, new ThreadId(3));
    private static readonly ProcessTrackedObjectId Value = new(TestMetadata.ProcessId, new TrackedObjectId(100));
    private readonly MdToken _field;

    public PublicationClassificationTests()
    {
        _detector = new FastTrackDetector(
            FastTrackPluginConfiguration.Default,
            _metadata,
            TimeProvider.System,
            NullLogger.Instance,
            threadNameResolver: _ => null);

        _detector.RecordThreadCreated(Publisher);
        _detector.RecordThreadCreated(Observer);
        _detector.RecordThreadCreated(Mutator);

        var type = _metadata.AddType("PublishedType");
        _field = _metadata.AddField(type, "State", isStatic: false);
    }

    private static CapturedStackTrace Stack(MdMethodDef method)
        => new(new CapturedStackFrame(TestMetadata.ModuleId, method));

    private void Write(ProcessThreadId thread, MdMethodDef method)
        => _detector.RecordWrite(thread, methodOffset: 0, _field, Value, Stack(method));

    private bool ReadIsRace(ProcessThreadId thread, MdMethodDef method)
        => _detector.RecordRead(thread, methodOffset: 0, _field, Value, Stack(method)) is not null;

    [Fact]
    public void InitWriteThenCrossThreadRead_WithoutPublication_IsReported()
    {
        // Arrange
        var init = _metadata.AddMethod(_metadata.AddType("Initializer"), "Init");
        var read = _metadata.AddMethod(_metadata.AddType("Consumer"), "Consume");

        // Act
        Write(Publisher, init);

        // Assert
        Assert.True(ReadIsRace(Observer, read));
    }

    [Fact]
    public void InitWriteThenPublishThenObserveThenRead_IsNotReported()
    {
        // Arrange
        var init = _metadata.AddMethod(_metadata.AddType("Initializer"), "Init");
        var read = _metadata.AddMethod(_metadata.AddType("Consumer"), "Consume");

        // Act
        Write(Publisher, init);
        _detector.RecordValuePublished(Publisher, Value);
        _detector.RecordValueObserved(Observer, Value);

        // Assert
        Assert.False(ReadIsRace(Observer, read));
    }

    [Fact]
    public void ObserveWithoutMatchingPublish_DoesNotCreateHappensBeforeEdge_IsReported()
    {
        // Arrange
        var init = _metadata.AddMethod(_metadata.AddType("Initializer"), "Init");
        var read = _metadata.AddMethod(_metadata.AddType("Consumer"), "Consume");

        // Act
        Write(Publisher, init);
        _detector.RecordValueObserved(Observer, Value);

        // Assert
        Assert.True(ReadIsRace(Observer, read));
    }

    [Fact]
    public void PostPublicationWriteByAnotherThread_IsStillReported()
    {
        // Arrange
        var init = _metadata.AddMethod(_metadata.AddType("Initializer"), "Init");
        var read = _metadata.AddMethod(_metadata.AddType("Consumer"), "Consume");
        var mutate = _metadata.AddMethod(_metadata.AddType("Mutator"), "Mutate");

        // Act & Assert
        Write(Publisher, init);
        _detector.RecordValuePublished(Publisher, Value);
        _detector.RecordValueObserved(Observer, Value);
        Assert.False(ReadIsRace(Observer, read));
        // Unsynchronized post-publication write
        Write(Mutator, mutate);
        Assert.True(ReadIsRace(Observer, read));
    }

    [Fact]
    public void StoreLoadOnBothSides_EstablishesHappensBeforeEdge_IsNotReported()
    {
        // Arrange
        var init = _metadata.AddMethod(_metadata.AddType("Initializer"), "Init");
        var read = _metadata.AddMethod(_metadata.AddType("Consumer"), "Consume");

        // Act
        Write(Publisher, init);
        _detector.RecordValuePublished(Publisher, Value);
        _detector.RecordValueObserved(Publisher, Value);
        _detector.RecordValuePublished(Observer, Value);
        _detector.RecordValueObserved(Observer, Value);

        // Assert
        Assert.False(ReadIsRace(Observer, read));
    }

    [Fact]
    public void PublisherLaterMutatesAfterPublish_RaceWithObserver_IsStillReported()
    {
        // Arrange
        var init = _metadata.AddMethod(_metadata.AddType("Initializer"), "Init");
        var read = _metadata.AddMethod(_metadata.AddType("Consumer"), "Consume");
        var mutate = _metadata.AddMethod(_metadata.AddType("Mutator"), "Mutate");

        // Act
        Write(Publisher, init);
        _detector.RecordValuePublished(Publisher, Value);
        _detector.RecordValueObserved(Observer, Value);
        // Publisher mutates the already-published object synchronization.
        Write(Publisher, mutate);

        // Assert
        Assert.True(ReadIsRace(Observer, read));
    }
}
