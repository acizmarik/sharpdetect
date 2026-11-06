// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class JoinableThreadsTests
{
    private readonly TestMetadata _metadata = new();
    private readonly FastTrackDetector _detector;
    private readonly MdToken _field;

    private static readonly ProcessThreadId Main = new(TestMetadata.ProcessId, new ThreadId(1));
    private static readonly ProcessThreadId Worker1 = new(TestMetadata.ProcessId, new ThreadId(2));
    private static readonly ProcessThreadId Worker2 = new(TestMetadata.ProcessId, new ThreadId(3));
    private static readonly ProcessTrackedObjectId Worker1Object = new(TestMetadata.ProcessId, new TrackedObjectId(10));
    private static readonly ProcessTrackedObjectId Worker2Object = new(TestMetadata.ProcessId, new TrackedObjectId(11));

    public JoinableThreadsTests()
    {
        _detector = new FastTrackDetector(
            FastTrackPluginConfiguration.Default,
            _metadata,
            TimeProvider.System,
            NullLogger.Instance,
            _ => null);

        var type = _metadata.AddType("Test");
        _field = _metadata.AddField(type, "Field", isStatic: true);
        _detector.RecordThreadCreated(Main);
    }

    private CapturedStackTrace CreateStack()
    {
        var type = _metadata.AddType($"Holder{Guid.NewGuid():N}");
        return new CapturedStackTrace(new CapturedStackFrame(TestMetadata.ModuleId, _metadata.AddMethod(type, "Run")));
    }

    private DataRaceInfo? Write(ProcessThreadId threadId)
        => _detector.RecordWrite(threadId, methodOffset: 0, _field, objectId: null, declaringTypeHandle: 0, CreateStack());

    private void Start(ProcessThreadId threadId, ProcessTrackedObjectId threadObjectId)
    {
        _detector.RecordThreadForkRequested(Main, threadObjectId);
        _detector.RecordThreadCreated(threadId);
        _detector.RecordThreadFork(threadObjectId, threadId);
    }

    private void Collect(ProcessTrackedObjectId objectId)
        => _detector.RecordGarbageCollectedObjects(objectId.ProcessId, [objectId.ObjectId]);

    [Fact]
    public void ExitedThread_IsForgottenOnceItsThreadObjectIsCollected()
    {
        Start(Worker1, Worker1Object);
        _detector.RecordThreadDestroyed(Worker1);
        Assert.Equal(2, _detector.GetTrackedThreadCount());

        Collect(Worker1Object);

        Assert.Equal(1, _detector.GetTrackedThreadCount());
    }

    [Fact]
    public void ExitedThread_CanStillBeJoinedWhileItsThreadObjectIsAlive()
    {
        Start(Worker1, Worker1Object);
        Assert.Null(Write(Worker1));
        _detector.RecordThreadDestroyed(Worker1);

        _detector.RecordThreadJoin(Main, Worker1);

        Assert.Null(Write(Main));
        Assert.Equal(2, _detector.GetTrackedThreadCount());
    }

    [Fact]
    public void RunningThread_IsKeptWhenItsThreadObjectIsCollected()
    {
        Start(Worker1, Worker1Object);

        Collect(Worker1Object);
        _detector.RecordThreadDestroyed(Worker1);

        Assert.Equal(2, _detector.GetTrackedThreadCount());
    }

    [Fact]
    public void ForgottenThread_StillRacesWithUnorderedWrite()
    {
        _detector.RecordThreadForkRequested(Main, Worker1Object);
        _detector.RecordThreadForkRequested(Main, Worker2Object);
        _detector.RecordThreadCreated(Worker1);
        _detector.RecordThreadFork(Worker1Object, Worker1);
        Assert.Null(Write(Worker1));
        _detector.RecordThreadDestroyed(Worker1);
        Collect(Worker1Object);

        _detector.RecordThreadCreated(Worker2);
        _detector.RecordThreadFork(Worker2Object, Worker2);
        var race = Write(Worker2);

        Assert.NotNull(race);
        Assert.Equal(Worker1, race.LastAccess.ProcessThreadId);
        Assert.Equal(Worker2, race.CurrentAccess.ProcessThreadId);
    }

    [Fact]
    public void ForgottenThread_JoinedBeforeCollection_StaysOrdered()
    {
        Start(Worker1, Worker1Object);
        Assert.Null(Write(Worker1));
        _detector.RecordThreadJoin(Main, Worker1);
        _detector.RecordThreadDestroyed(Worker1);
        Collect(Worker1Object);

        Assert.Null(Write(Main));
    }

    [Fact]
    public void JoinOfForgottenThread_DoesNotRecreateItsClock()
    {
        Start(Worker1, Worker1Object);
        _detector.RecordThreadDestroyed(Worker1);
        Collect(Worker1Object);

        _detector.RecordThreadJoin(Main, Worker1);

        Assert.Equal(1, _detector.GetTrackedThreadCount());
    }
}
