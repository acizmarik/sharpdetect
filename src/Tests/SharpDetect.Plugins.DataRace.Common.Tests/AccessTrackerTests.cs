// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using Xunit;

namespace SharpDetect.Plugins.DataRace.Common.Tests;

public class AccessTrackerTests
{
    private static readonly ProcessThreadId First = new(1, new ThreadId(1));
    private static readonly ProcessThreadId Second = new(1, new ThreadId(2));
    private static readonly ProcessThreadId Third = new(1, new ThreadId(3));
    private static readonly CapturedStackTrace Stack = new(new CapturedStackFrame(new ModuleId(1), new MdMethodDef(0x06000001)));

    private readonly AccessTracker _tracker = new(_ => null);
    private readonly FieldId _field;

    public AccessTrackerTests()
    {
        var module = new ModuleDefUser("TestModule");
        var type = new TypeDefUser("TestNamespace", "Holder", module.CorLibTypes.Object.TypeDefOrRef);
        module.Types.Add(type);
        var field = new FieldDefUser("Counter", new FieldSig(module.CorLibTypes.Int32), FieldAttributes.Public);
        type.Fields.Add(field);
        _field = new FieldId(1, new ModuleId(1), new MdToken(0x04000001), field);
    }

    [Fact]
    public void OrderedReads_KeepOnlyTheLastReader()
    {
        // Act
        Read(First, trackReadsByThread: false);
        Read(Second, trackReadsByThread: false);

        // Assert
        Assert.False(_tracker.TryGetLastReadAccess(_field, null, First, out _));
        Assert.True(_tracker.TryGetLastReadAccess(_field, null, Second, out _));
    }

    [Fact]
    public void UnorderedRead_KeepsTheEarlierReaderAsWell()
    {
        // Act
        Read(First, trackReadsByThread: false);
        Read(Second, trackReadsByThread: true);
        Read(Third, trackReadsByThread: true);

        // Assert
        Assert.True(_tracker.TryGetLastReadAccess(_field, null, First, out var first));
        Assert.Equal(First, first.ProcessThreadId);
        Assert.True(_tracker.TryGetLastReadAccess(_field, null, Second, out _));
        Assert.True(_tracker.TryGetLastReadAccess(_field, null, Third, out _));
    }

    [Fact]
    public void Write_ForgetsEarlierReaders()
    {
        // Act
        Read(First, trackReadsByThread: false);
        Read(Second, trackReadsByThread: true);
        _tracker.RecordAccess(_field, null, Third, methodOffset: 0, AccessType.Write, Stack);
        Read(Second, trackReadsByThread: false);

        // Assert
        Assert.False(_tracker.TryGetLastReadAccess(_field, null, First, out _));
        Assert.True(_tracker.TryGetLastReadAccess(_field, null, Second, out _));
    }

    [Fact]
    public void CollectedObject_ForgetsItsReaders()
    {
        // Arrange
        var instance = new ProcessTrackedObjectId(1, new TrackedObjectId(42));
        _tracker.RecordAccess(_field, instance, First, methodOffset: 0, AccessType.Read, Stack);
        _tracker.RecordAccess(_field, instance, Second, methodOffset: 0, AccessType.Read, Stack, trackReadsByThread: true);

        // Act
        _tracker.RemoveTrackedObjects(1, [instance.ObjectId]);

        // Assert
        Assert.False(_tracker.TryGetLastReadAccess(_field, instance, First, out _));
        Assert.False(_tracker.TryGetLastReadAccess(_field, instance, Second, out _));
    }

    private void Read(ProcessThreadId thread, bool trackReadsByThread)
        => _tracker.RecordAccess(_field, null, thread, methodOffset: 0, AccessType.Read, Stack, trackReadsByThread);
}
