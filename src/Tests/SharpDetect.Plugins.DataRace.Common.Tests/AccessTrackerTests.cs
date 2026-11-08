// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

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

    private AccessTracker.AccessSlots _slots;

    [Fact]
    public void OrderedReads_KeepOnlyTheLastReader()
    {
        // Act
        Read(First, trackReadsByThread: false);
        Read(Second, trackReadsByThread: false);

        // Assert
        Assert.False(AccessTracker.TryGetLastReadAccess(ref _slots, First, out _));
        Assert.True(AccessTracker.TryGetLastReadAccess(ref _slots, Second, out _));
    }

    [Fact]
    public void UnorderedRead_KeepsTheEarlierReaderAsWell()
    {
        // Act
        Read(First, trackReadsByThread: false);
        Read(Second, trackReadsByThread: true);
        Read(Third, trackReadsByThread: true);

        // Assert
        Assert.True(AccessTracker.TryGetLastReadAccess(ref _slots, First, out var first));
        Assert.Equal(First, first.ProcessThreadId);
        Assert.True(AccessTracker.TryGetLastReadAccess(ref _slots, Second, out _));
        Assert.True(AccessTracker.TryGetLastReadAccess(ref _slots, Third, out _));
    }

    [Fact]
    public void Write_ForgetsEarlierReaders()
    {
        // Act
        Read(First, trackReadsByThread: false);
        Read(Second, trackReadsByThread: true);
        AccessTracker.RecordAccess(ref _slots, Third, methodOffset: 0, AccessType.Write, Stack);
        Read(Second, trackReadsByThread: false);

        // Assert
        Assert.False(AccessTracker.TryGetLastReadAccess(ref _slots, First, out _));
        Assert.True(AccessTracker.TryGetLastReadAccess(ref _slots, Second, out _));
    }

    [Fact]
    public void AtomicAccess_IsNotTakenForAPlainRead()
    {
        // Act
        AccessTracker.RecordAtomicAccess(ref _slots, First, methodOffset: 0, stored: false, Stack);
        Read(Second, trackReadsByThread: true);

        // Assert
        Assert.False(AccessTracker.TryGetLastReadAccess(ref _slots, First, out _));
        Assert.True(AccessTracker.TryGetLastReadAccess(ref _slots, Second, out _));
        Assert.True(AccessTracker.TryGetLastAccess(ref _slots, out var last));
        Assert.Equal(Second, last.ProcessThreadId);
    }

    [Fact]
    public void AtomicAccesses_KeepTheLastStoreOfEachThread()
    {
        // Act
        AccessTracker.RecordAtomicAccess(ref _slots, First, methodOffset: 1, stored: true, Stack);
        AccessTracker.RecordAtomicAccess(ref _slots, First, methodOffset: 2, stored: false, Stack);
        AccessTracker.RecordAtomicAccess(ref _slots, Second, methodOffset: 3, stored: false, Stack);

        // Assert
        Assert.True(AccessTracker.TryGetLastAtomicAccess(ref _slots, First, storesOnly: false, out var lastOfFirst));
        Assert.Equal((2u, AccessType.AtomicRead), (lastOfFirst.MethodOffset, lastOfFirst.AccessType));
        Assert.True(AccessTracker.TryGetLastAtomicAccess(ref _slots, First, storesOnly: true, out var storeOfFirst));
        Assert.Equal((1u, AccessType.AtomicWrite), (storeOfFirst.MethodOffset, storeOfFirst.AccessType));
        Assert.True(AccessTracker.TryGetLastAtomicAccess(ref _slots, Second, storesOnly: false, out _));
        Assert.False(AccessTracker.TryGetLastAtomicAccess(ref _slots, Second, storesOnly: true, out _));
        Assert.False(AccessTracker.TryGetLastAccess(ref _slots, out _));
    }

    [Fact]
    public void Write_ForgetsEarlierAtomicAccesses()
    {
        // Act
        AccessTracker.RecordAtomicAccess(ref _slots, First, methodOffset: 0, stored: true, Stack);
        AccessTracker.RecordAccess(ref _slots, Second, methodOffset: 0, AccessType.Write, Stack);

        // Assert
        Assert.False(AccessTracker.TryGetLastAtomicAccess(ref _slots, First, storesOnly: false, out _));
    }

    private void Read(ProcessThreadId thread, bool trackReadsByThread)
        => AccessTracker.RecordAccess(ref _slots, thread, methodOffset: 0, AccessType.Read, Stack, trackReadsByThread);
}
