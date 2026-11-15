// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.FastTrack.Tests.Fakes;
using Xunit;

namespace SharpDetect.Plugins.DataRace.FastTrack.Tests;

public class KnownGapTests
{
    private const string ThreadKeyedClocksGap =
        "Known limitation of thread-keyed clocks: work items that share a pool thread are ordered by program order, so races between them are missed";
    private const string ConstructorEscapeGap =
        "Known gap: a constructor write stays exempt when `this` escaped before the constructor touched any field of the object, or through a channel other than a thread or task start or a modeled publication";
    private const string ExitedThreadClockGap =
        "Known gap: the clock of an exited thread that was not started through a tracked Thread object is never forgotten";

    private const int StackEntrySize = sizeof(ulong) + sizeof(uint);

    private readonly DetectorHarness _harness = new();

    [Fact(Skip = ThreadKeyedClocksGap)]
    public void UnjoinedTasksRunOneAfterAnotherOnTheSameWorker_AreUnordered()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var firstTask = _harness.NewObject();
        var secondTask = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act - nothing but the pool's choice of thread orders the two bodies
        _harness.Detector.RecordTaskScheduled(parent, firstTask);
        _harness.Detector.RecordTaskScheduled(parent, secondTask);
        _harness.Detector.RecordTaskStarted(worker, firstTask);
        _harness.Write(worker, field, instance: null);
        _harness.Detector.RecordTaskCompleted(worker, firstTask);
        _harness.Detector.RecordTaskStarted(worker, secondTask);

        // Assert
        Assert.True(_harness.WriteIsRace(worker, field, instance: null));
    }

    [Fact(Skip = ThreadKeyedClocksGap)]
    public void JoiningTask_DoesNotOrderEarlierUnjoinedTaskOfTheSameWorker()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var unjoinedTask = _harness.NewObject();
        var joinedTask = _harness.NewObject();
        var field = _harness.NewStaticField("Shared");

        // Act - the parent joins only the second task, which happened to reuse the first task's worker
        _harness.Detector.RecordTaskScheduled(parent, unjoinedTask);
        _harness.Detector.RecordTaskStarted(worker, unjoinedTask);
        _harness.Write(worker, field, instance: null);
        _harness.Detector.RecordTaskCompleted(worker, unjoinedTask);
        _harness.Detector.RecordTaskScheduled(parent, joinedTask);
        _harness.Detector.RecordTaskStarted(worker, joinedTask);
        _harness.Detector.RecordTaskCompleted(worker, joinedTask);
        _harness.Detector.RecordTaskJoinFinished(parent, joinedTask);

        // Assert
        Assert.True(_harness.WriteIsRace(parent, field, instance: null));
    }

    [Fact]
    public void ObjectConfinedToOneWorker_IsNotReportedAcrossTheTasksItRuns()
    {
        // Arrange
        var parent = _harness.NewThread();
        var worker = _harness.NewThread();
        var firstTask = _harness.NewObject();
        var secondTask = _harness.NewObject();
        var threadConfined = _harness.NewObject();
        var field = _harness.NewInstanceField("Hits");

        // Act - both tasks update an object that only their worker can reach, such as a [ThreadStatic] cache
        _harness.Detector.RecordTaskScheduled(parent, firstTask);
        _harness.Detector.RecordTaskScheduled(parent, secondTask);
        _harness.Detector.RecordTaskStarted(worker, firstTask);
        _harness.Write(worker, field, threadConfined);
        _harness.Detector.RecordTaskCompleted(worker, firstTask);
        _harness.Detector.RecordTaskStarted(worker, secondTask);

        // Assert
        Assert.False(_harness.WriteIsRace(worker, field, threadConfined));
    }

    [Fact(Skip = ConstructorEscapeGap)]
    public void ConstructorWriteAfterThisEscapedToAStartedThread_RacesWithThatThreadsRead()
    {
        // Arrange
        var metadata = new TestMetadata();
        var detector = new FastTrackDetector(
            FastTrackPluginConfiguration.Default,
            metadata,
            TimeProvider.System,
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance,
            _ => null);
        var type = metadata.AddType("Holder");
        var field = metadata.AddField(type, "State", isStatic: false);
        var constructor = metadata.AddInstanceConstructor(type);
        var readerBody = metadata.AddMethod(type, "Run");
        var constructing = new ProcessThreadId(TestMetadata.ProcessId, new ThreadId(1));
        var reader = new ProcessThreadId(TestMetadata.ProcessId, new ThreadId(2));
        var instance = new ProcessTrackedObjectId(TestMetadata.ProcessId, new TrackedObjectId(100));
        var readerThreadObject = new ProcessTrackedObjectId(TestMetadata.ProcessId, new TrackedObjectId(101));
        detector.RecordThreadCreated(constructing);

        // Act - the constructor starts a thread that captures `this`, and only then initializes the field
        detector.RecordThreadForkRequested(constructing, readerThreadObject);
        detector.RecordThreadFork(readerThreadObject, reader);
        detector.RecordWrite(constructing, methodOffset: 0, field, instance, declaringTypeHandle: 0, CreateStack(constructor));
        var race = detector.RecordRead(reader, methodOffset: 0, field, instance, declaringTypeHandle: 0, CreateStack(readerBody));

        // Assert
        Assert.NotNull(race);
    }

    [Fact(Skip = ExitedThreadClockGap)]
    public void ExitedThreadsWithoutATrackedThreadObject_AreForgotten()
    {
        // Arrange
        var baseline = _harness.Detector.GetTrackedThreadCount();

        // Act - thread pool, finalizer and native threads are never started through a hooked Thread object
        for (var i = 0; i < 100; i++)
        {
            var thread = _harness.NewThread();
            _harness.ObserveThread(thread);
            _harness.Detector.RecordThreadDestroyed(thread);
        }

        // Assert
        Assert.Equal(baseline, _harness.Detector.GetTrackedThreadCount());
    }

    private static CapturedStackTrace CreateStack(MdMethodDef top)
    {
        var blob = new byte[StackEntrySize];
        MemoryMarshal.Write(blob.AsSpan(), (ulong)TestMetadata.ModuleId.Value);
        MemoryMarshal.Write(blob.AsSpan(sizeof(ulong)), (uint)top.Value);
        return new CapturedStackTrace(new CapturedStackFrame(TestMetadata.ModuleId, top), blob);
    }
}
