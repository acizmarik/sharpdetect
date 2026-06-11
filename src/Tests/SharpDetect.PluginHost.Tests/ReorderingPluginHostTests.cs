// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.PluginHost.Services.Strategies;
using Xunit;

namespace SharpDetect.PluginHost.Tests;

public class ReorderingPluginHostTests
{
    private const uint T1 = 1;
    private const uint T2 = 2;
    private const uint T3 = 3;
    private const uint TReaper = 9;
    private const nuint L1 = 0x1000;
    private const nuint L2 = 0x1001;
    private const nuint S1 = 0x1800;
    private const nuint Task1 = 0x2000;
    private const nuint Task2 = 0x2001;

    private static ReorderingPluginHost Build(out RecordingPluginHost recorder)
    {
        recorder = new RecordingPluginHost();
        return new ReorderingPluginHost(recorder, NullLogger<ReorderingPluginHost>.Instance);
    }

    private static ReorderingPluginHost Build(out RecordingPluginHost recorder, out CapturingLogger<ReorderingPluginHost> logger)
    {
        recorder = new RecordingPluginHost();
        logger = new CapturingLogger<ReorderingPluginHost>();
        return new ReorderingPluginHost(recorder, logger);
    }

    [Fact]
    public void ReorderingPluginHost_AcquireExitBeforeReleaseExit_IsDeferred_IsReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.FieldRead(T2));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult)); // arrives too early
        host.ProcessEvent(SyncEventBuilder.FieldRead(T1)); // arrives too early
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult)); // unblocks
        var kinds = recorder.Dispatched.Select(ClassifyDispatched).ToArray();
        
        // Assert
        Assert.Equal(new[]
        {
            (T1, RecordedEventType.MonitorLockAcquire),
            (T2, RecordedEventType.MonitorLockAcquire),
            (T2, RecordedEventType.MonitorLockAcquireResult),
            (T2, RecordedEventType.InstanceFieldRead),
            (T2, RecordedEventType.MonitorLockRelease),
            (T2, RecordedEventType.MonitorLockReleaseResult),
            (T1, RecordedEventType.MonitorLockAcquireResult),
            (T1, RecordedEventType.InstanceFieldRead),
        }, kinds);
    }

    [Fact]
    public void ReorderingPluginHost_ReentrantLocking_NotDeferred_NotReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));

        // Assert
        Assert.Equal(8, recorder.Dispatched.Count);
        Assert.All(recorder.Dispatched, e => Assert.Equal(T1, e.Metadata.Tid.Value));
    }

    [Fact]
    public void ReorderingPluginHost_FailedTryEnter_NotDeferred_NotReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockTryAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.MonitorLockAcquireResult, success: false));

        // Assert
        Assert.Equal(4, recorder.Dispatched.Count);
        Assert.Equal((T2, RecordedEventType.MonitorLockAcquireResult), ClassifyDispatched(recorder.Dispatched[^1]));
    }

    [Fact]
    public void ReorderingPluginHost_MultipleWaiters_DrainInFifoOrder()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred [1st in wake queue]
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockAcquireResult)); // deferred [2nd in wake queue]
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult)); // wakes T2
        var lastT1 = recorder.Dispatched.FindLastIndex(e => e.Metadata.Tid.Value == T1);
        var firstT2Acq = recorder.Dispatched.FindIndex(e => e.Metadata.Tid.Value == T2 && IsAcquireResult(e));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult));
        
        // Assert
        Assert.True(firstT2Acq > lastT1, "T2's acquire result should dispatch after T1's release");
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_CascadingDefer_DrainedAcquireDefersAgainOnAnotherLock()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act & Assert
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockAcquire, L2));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred [waiting on T1 to release L1]
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L2));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred [waiting on T3 to release L2]
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult)); // wakes T2 to acquire L1
        var t2Acquires = recorder.Dispatched.Where(e => e.Metadata.Tid.Value == T2 && IsAcquireResult(e)).ToList();
        Assert.Single(t2Acquires);
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockRelease, L2));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockReleaseResult)); // wakes T2 to acquire L2
        t2Acquires = recorder.Dispatched.Where(e => e.Metadata.Tid.Value == T2 && IsAcquireResult(e)).ToList();
        Assert.Equal(2, t2Acquires.Count);
    }

    [Fact]
    public void ReorderingPluginHost_TaskJoin_DeferredUntilTaskComplete()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act & Assert
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.TaskJoinStart, Task1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.TaskJoinFinish)); // deferred [wait on task complete]
        host.ProcessEvent(SyncEventBuilder.FieldRead(T1)); // also deferred
        Assert.DoesNotContain(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 &&
            (e.EventArgs as MethodExitRecordedEvent)?.Interpretation == (ushort)RecordedEventType.TaskJoinFinish);
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.TaskComplete)); // wake up
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 &&
            (e.EventArgs as MethodExitRecordedEvent)?.Interpretation == (ushort)RecordedEventType.TaskJoinFinish);
    }

    [Fact]
    public void ReorderingPluginHost_TaskJoin_QueuesSubsequentPlainMethodExitOnSameThread()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.TaskJoinStart, Task1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.TaskJoinFinish)); // deferred
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MethodExit)); // must also defer
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.TaskComplete)); // wake T1

        // Assert
        var t1Exits = recorder.Dispatched
            .Where(e => e.Metadata.Tid.Value == T1 && e.EventArgs is MethodExitRecordedEvent)
            .Select(e => (RecordedEventType)((MethodExitRecordedEvent)e.EventArgs).Interpretation)
            .ToArray();
        Assert.Equal(new[]
        {
            RecordedEventType.TaskJoinFinish,
            RecordedEventType.MethodExit,
        }, t1Exits);
    }

    [Fact]
    public void ReorderingPluginHost_MonitorWait_ReleasesAndReacquires()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, true));

        // Assert
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 &&
            ClassifyDispatched(e) == (T1, RecordedEventType.MonitorWaitResult));
    }
    
    [Fact]
    public void ReorderingPluginHost_MonitorWaitExitBeforeReleaseExit_IsDeferred_IsReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult)); // wake up

        // Assert
        Assert.Equal(8, recorder.Dispatched.Count);
        Assert.Equal((T1, RecordedEventType.MonitorWaitResult), ClassifyDispatched(recorder.Dispatched.Last()));
    }
    
    [Fact]
    public void ReorderingPluginHost_FailedMonitorWait_IsDeferred_IsReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, false)); // deferred — wait timeout still reacquires
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult)); // wakes T1

        // Assert
        Assert.Equal(8, recorder.Dispatched.Count);
        Assert.Equal((T1, RecordedEventType.MonitorWaitResult), ClassifyDispatched(recorder.Dispatched.Last()));
    }

    [Fact]
    public void ReorderingPluginHost_FailedMonitorWait_ReacquiresShadowLockForOwner()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act & Assert
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, false));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockAcquireResult));
        Assert.DoesNotContain(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_ThreadDestroy_AbandonedLock_ReleasesAndWakesWaiters()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred
        host.ProcessEvent(SyncEventBuilder.ThreadDestroy(T1));

        // Assert
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T2 &&
            (e.EventArgs as MethodExitRecordedEvent)?.Interpretation == (ushort)RecordedEventType.MonitorLockAcquireResult);
    }

    [Fact]
    public void ReorderingPluginHost_SemaphoreHappyPath_NotDeferred_NotReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetAndCount(T1, RecordedEventType.SemaphoreRelease, S1, 1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.SemaphoreReleaseResult));

        // Assert
        Assert.Equal(5, recorder.Dispatched.Count);
        Assert.All(recorder.Dispatched, e => Assert.Equal(T1, e.Metadata.Tid.Value));
    }

    [Fact]
    public void ReorderingPluginHost_SemaphoreAcquireExitBeforeReleaseExit_IsDeferred_IsReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, true)); // deferred [no permits]
        host.ProcessEvent(SyncEventBuilder.FieldRead(T2)); // also deferred
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetAndCount(T1, RecordedEventType.SemaphoreRelease, S1, 1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.SemaphoreReleaseResult)); // wakes T2

        // Assert
        var kinds = recorder.Dispatched.Select(ClassifyDispatched).ToArray();
        Assert.Equal(new[]
        {
            (T1, RecordedEventType.SemaphoreCreate),
            (T1, RecordedEventType.SemaphoreAcquire),
            (T1, RecordedEventType.SemaphoreAcquireResult),
            (T2, RecordedEventType.SemaphoreAcquire),
            (T1, RecordedEventType.SemaphoreRelease),
            (T1, RecordedEventType.SemaphoreReleaseResult),
            (T2, RecordedEventType.SemaphoreAcquireResult),
            (T2, RecordedEventType.InstanceFieldRead),
        }, kinds);
    }

    [Fact]
    public void ReorderingPluginHost_SemaphoreMultipleWaiters_DrainInFifoOrder()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act & Assert
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, true)); // deferred [1st]
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T3, RecordedEventType.SemaphoreAcquireResult, true)); // deferred [2nd]
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetAndCount(T1, RecordedEventType.SemaphoreRelease, S1, 1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.SemaphoreReleaseResult)); // wakes T2
        var t2AcqIdx = recorder.Dispatched.FindIndex(e => e.Metadata.Tid.Value == T2 && IsSemaphoreAcquireResult(e));
        var t3AcqIdx = recorder.Dispatched.FindIndex(e => e.Metadata.Tid.Value == T3 && IsSemaphoreAcquireResult(e));
        Assert.True(t2AcqIdx >= 0, "T2 acquire result should have dispatched after T1 release");
        Assert.True(t3AcqIdx < 0, "T3 acquire result should still be deferred");
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetAndCount(T2, RecordedEventType.SemaphoreRelease, S1, 1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.SemaphoreReleaseResult));
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsSemaphoreAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_SemaphoreReleaseWithCount_WakesMultipleWaiters()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 10));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T3, RecordedEventType.SemaphoreAcquireResult, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetAndCount(T1, RecordedEventType.SemaphoreRelease, S1, 2));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.SemaphoreReleaseResult)); // wakes T2 and T3

        // Assert
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T2 && IsSemaphoreAcquireResult(e));
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsSemaphoreAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_FailedSemaphoreTryAcquire_NotDeferred_NotReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreTryAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, false));

        // Assert
        Assert.Equal(5, recorder.Dispatched.Count);
        Assert.Equal((T2, RecordedEventType.SemaphoreAcquireResult), ClassifyDispatched(recorder.Dispatched[^1]));
    }

    [Fact]
    public void ReorderingPluginHost_Semaphore_InitialCountTwo_BothAcquiresSucceedWithoutRelease()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 2, 2));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, true));
        var kinds = recorder.Dispatched.Select(ClassifyDispatched).ToArray();
        
        // Assert
        Assert.Equal(new[]
        {
            (T1, RecordedEventType.SemaphoreCreate),
            (T1, RecordedEventType.SemaphoreAcquire),
            (T1, RecordedEventType.SemaphoreAcquireResult),
            (T2, RecordedEventType.SemaphoreAcquire),
            (T2, RecordedEventType.SemaphoreAcquireResult),
        }, kinds);
    }

    [Fact]
    public void ReorderingPluginHost_MonitorWaitReentrancy_RestoresOwnedDepth()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act & Assert
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockAcquireResult));
        Assert.DoesNotContain(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_MonitorWaitWithoutOwnership_DispatchesWithoutCorruptingShadow()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, false));
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 && ClassifyDispatched(e) == (T1, RecordedEventType.MonitorWaitAttempt));
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 && ClassifyDispatched(e) == (T1, RecordedEventType.MonitorWaitResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockAcquireResult));

        // Assert
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_PulseOne_PassThrough()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorPulseOneAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorPulseOneResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));

        // Assert
        Assert.Equal(6, recorder.Dispatched.Count);
        Assert.Equal((T1, RecordedEventType.MonitorPulseOneAttempt), ClassifyDispatched(recorder.Dispatched[2]));
        Assert.Equal((T1, RecordedEventType.MonitorPulseOneResult), ClassifyDispatched(recorder.Dispatched[3]));
    }

    [Fact]
    public void ReorderingPluginHost_ThreadJoin_PassThrough()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.ThreadJoinAttempt, L1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.ThreadJoinResult, true));

        // Assert
        Assert.Equal(2, recorder.Dispatched.Count);
        Assert.Equal((T1, RecordedEventType.ThreadJoinAttempt), ClassifyDispatched(recorder.Dispatched[0]));
        Assert.Equal((T1, RecordedEventType.ThreadJoinResult), ClassifyDispatched(recorder.Dispatched[1]));
    }

    [Fact]
    public void ReorderingPluginHost_UnobservedTaskJoin_PassThrough()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.TaskJoinStart, Task2));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.TaskJoinFinish, true));

        // Assert
        Assert.Equal(2, recorder.Dispatched.Count);
        Assert.Equal((T1, RecordedEventType.TaskJoinFinish), ClassifyDispatched(recorder.Dispatched[1]));
    }

    [Fact]
    public void ReorderingPluginHost_FailedTaskJoin_NotDeferred()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.TaskJoinStart, Task1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.TaskJoinFinish, false));

        // Assert
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 &&
            (e.EventArgs as MethodExitWithArgumentsRecordedEvent)?.Interpretation == (ushort)RecordedEventType.TaskJoinFinish);
    }

    [Fact]
    public void ReorderingPluginHost_ThreadDestroy_PurgesDestroyedFromLockWaiterQueue()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred [1st in wake queue]
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockAcquireResult)); // deferred [2nd in wake queue]
        host.ProcessEvent(SyncEventBuilder.ThreadDestroy(T2, TReaper)); // dead before being woken; destroy fires from a different thread
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult)); // would have woken T2 — must now wake T3

        // Assert
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
        Assert.DoesNotContain(recorder.Dispatched, e => e.Metadata.Tid.Value == T2 && IsAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_ThreadDestroy_PurgesDestroyedFromSemaphoreWaiterQueue()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T3, RecordedEventType.SemaphoreAcquireResult, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.ThreadDestroy(T2, TReaper));
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetAndCount(T1, RecordedEventType.SemaphoreRelease, S1, 1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.SemaphoreReleaseResult));

        // Assert
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsSemaphoreAcquireResult(e));
        Assert.DoesNotContain(recorder.Dispatched, e => e.Metadata.Tid.Value == T2 && IsSemaphoreAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_ThreadDestroy_AbandonedSemaphorePermit_DoesNotAutoRelease()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.ThreadDestroy(T1));

        // Assert
        Assert.DoesNotContain(recorder.Dispatched, e => e.Metadata.Tid.Value == T2 && IsSemaphoreAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_ThreadDestroy_OwnedTask_CompletesAndDrainsJoiners()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.TaskJoinStart, Task1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.TaskJoinFinish, true)); // deferred
        Assert.DoesNotContain(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 &&
            (e.EventArgs as MethodExitWithArgumentsRecordedEvent)?.Interpretation == (ushort)RecordedEventType.TaskJoinFinish);
        host.ProcessEvent(SyncEventBuilder.ThreadDestroy(T2));

        // Assert
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 &&
            (e.EventArgs as MethodExitWithArgumentsRecordedEvent)?.Interpretation == (ushort)RecordedEventType.TaskJoinFinish);
    }

    [Fact]
    public void ReorderingPluginHost_ThreadDestroy_DrainsAllPendingEventsOfWokenThread()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred
        host.ProcessEvent(SyncEventBuilder.FieldRead(T2)); // also deferred behind T2's BlockedOn
        host.ProcessEvent(SyncEventBuilder.ThreadDestroy(T1));

        // Assert
        var t2Events = recorder.Dispatched
            .Where(e => e.Metadata.Tid.Value == T2)
            .Select(ClassifyDispatched)
            .ToArray();
        Assert.Equal(new[]
        {
            (T2, RecordedEventType.MonitorLockAcquire),
            (T2, RecordedEventType.MonitorLockAcquireResult),
            (T2, RecordedEventType.InstanceFieldRead),
        }, t2Events);
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_CleanLock_RemovesShadowAndForwardsEvent()
    {
        // Arrange
        var host = Build(out var recorder, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        Assert.Equal(1, host.ShadowLockCount);
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, L1));

        // Assert
        Assert.Equal(0, host.ShadowLockCount);
        Assert.Contains(recorder.Dispatched, e => e.EventArgs is GarbageCollectedTrackedObjectsRecordedEvent);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_CleanSemaphore_RemovesShadowAndForwardsEvent()
    {
        // Arrange
        var host = Build(out var recorder, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetAndCount(T1, RecordedEventType.SemaphoreRelease, S1, 1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.SemaphoreReleaseResult));
        Assert.Equal(1, host.ShadowSemaphoreCount);
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, S1));

        // Assert
        Assert.Equal(0, host.ShadowSemaphoreCount);
        Assert.Contains(recorder.Dispatched, e => e.EventArgs is GarbageCollectedTrackedObjectsRecordedEvent);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_CleanTask_RemovesShadowAndForwardsEvent()
    {
        // Arrange
        var host = Build(out var recorder, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.TaskComplete));
        Assert.Equal(1, host.ShadowTaskCount);
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, Task1));

        // Assert
        Assert.Equal(0, host.ShadowTaskCount);
        Assert.Contains(recorder.Dispatched, e => e.EventArgs is GarbageCollectedTrackedObjectsRecordedEvent);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_MixedObjects_RemovesAllMatchingShadows()
    {
        // Arrange
        var host = Build(out _, out _);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.TaskComplete));
        Assert.Equal(1, host.ShadowLockCount);
        Assert.Equal(1, host.ShadowSemaphoreCount);
        Assert.Equal(1, host.ShadowTaskCount);
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, L1, S1, Task1));

        // Assert
        Assert.Equal(0, host.ShadowLockCount);
        Assert.Equal(0, host.ShadowSemaphoreCount);
        Assert.Equal(0, host.ShadowTaskCount);
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_LockWithWaiters_DrainsWaiters()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred
        host.ProcessEvent(SyncEventBuilder.FieldRead(T2)); // also deferred behind T2's BlockedOn
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, L1)); // owner's release was never observed

        // Assert
        var t2Events = recorder.Dispatched
            .Where(e => e.Metadata.Tid.Value == T2)
            .Select(ClassifyDispatched)
            .ToArray();
        Assert.Equal(new[]
        {
            (T2, RecordedEventType.MonitorLockAcquire),
            (T2, RecordedEventType.MonitorLockAcquireResult),
            (T2, RecordedEventType.InstanceFieldRead),
        }, t2Events);
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_SemaphoreWithWaiters_DrainsWaiters()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 0, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T2, RecordedEventType.SemaphoreAcquireResult, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.FieldRead(T2)); // also deferred behind T2's BlockedOn
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, S1)); // releaser's exit was never observed

        // Assert
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T2 && IsSemaphoreAcquireResult(e));
        Assert.Contains(recorder.Dispatched, e => ClassifyDispatched(e) == (T2, RecordedEventType.InstanceFieldRead));
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_TaskWithJoiners_DrainsJoiners()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.TaskJoinStart, Task1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.TaskJoinFinish, true)); // deferred
        host.ProcessEvent(SyncEventBuilder.FieldRead(T1)); // also deferred behind T1's BlockedOn
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, Task1)); // task's completion was never observed

        // Assert
        Assert.Contains(recorder.Dispatched, e =>
            e.Metadata.Tid.Value == T1 &&
            (e.EventArgs as MethodExitWithArgumentsRecordedEvent)?.Interpretation == (ushort)RecordedEventType.TaskJoinFinish);
        Assert.Contains(recorder.Dispatched, e => ClassifyDispatched(e) == (T1, RecordedEventType.InstanceFieldRead));
    }

    [Fact]
    public void ReorderingPluginHost_LargePendingQueue_LogsWarningWithBlockTargetKind()
    {
        // Arrange
        var host = Build(out _, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult)); // deferred [1]
        for (var i = 0; i < 63; i++)
            host.ProcessEvent(SyncEventBuilder.FieldRead(T2)); // deferred [2..64]

        // Assert
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("(lock)"));
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_LockWithOwner_LogsWarning()
    {
        // Arrange
        var host = Build(out _, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, L1));

        // Assert
        Assert.Equal(0, host.ShadowLockCount);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Lock"));
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_SemaphoreWithOutstandingPermits_LogsWarning()
    {
        // Arrange
        var host = Build(out _, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTargetCountAndCapacity(T1, RecordedEventType.SemaphoreCreate, S1, 1, 1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreAcquire, S1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, true));
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, S1));

        // Assert
        Assert.Equal(0, host.ShadowSemaphoreCount);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Semaphore"));
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_TaskNotCompleted_LogsWarning()
    {
        // Arrange
        var host = Build(out _, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.TaskStart, Task1));
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, Task1));

        // Assert
        Assert.Equal(0, host.ShadowTaskCount);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Message.Contains("Task"));
    }

    [Fact]
    public void ReorderingPluginHost_GarbageCollected_UnknownObject_NoOp()
    {
        // Arrange
        var host = Build(out var recorder, out var logger);

        // Act
        host.ProcessEvent(SyncEventBuilder.GarbageCollected(TReaper, L1, S1, Task1));

        // Assert
        Assert.Equal(0, host.ShadowLockCount);
        Assert.Equal(0, host.ShadowSemaphoreCount);
        Assert.Equal(0, host.ShadowTaskCount);
        Assert.DoesNotContain(logger.Entries, e => e.Level >= LogLevel.Warning);
        Assert.Contains(recorder.Dispatched, e => e.EventArgs is GarbageCollectedTrackedObjectsRecordedEvent);
    }

    [Fact]
    public void ReorderingPluginHost_ThreadStartCore_NotBlocked_ThreadStartCallback_ProcessedImmediately()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act & Assert
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.ThreadStartCore, T3));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.ThreadStartCallback, T3));
        Assert.Contains(recorder.Dispatched, e =>
            ClassifyDispatched(e) == (T1, RecordedEventType.ThreadStartCore));
        Assert.Contains(recorder.Dispatched, e =>
            ClassifyDispatched(e) == (T3, RecordedEventType.ThreadStartCallback));
        var coreIdx = recorder.Dispatched.FindIndex(e => ClassifyDispatched(e) == (T1, RecordedEventType.ThreadStartCore));
        var callbackIdx = recorder.Dispatched.FindIndex(e => ClassifyDispatched(e) == (T3, RecordedEventType.ThreadStartCallback));
        Assert.True(coreIdx < callbackIdx);
    }

    [Fact]
    public void ReorderingPluginHost_ThreadStartCallback_DeferredUntilParentThreadStartCoreProcessed()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act & Assert
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.ThreadStartCore, T3));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.ThreadStartCallback, T3));
        Assert.DoesNotContain(recorder.Dispatched, e =>
            ClassifyDispatched(e) == (T2, RecordedEventType.ThreadStartCore));
        Assert.DoesNotContain(recorder.Dispatched, e =>
            ClassifyDispatched(e) == (T3, RecordedEventType.ThreadStartCallback));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        Assert.Contains(recorder.Dispatched, e =>
            ClassifyDispatched(e) == (T2, RecordedEventType.ThreadStartCore));
        Assert.Contains(recorder.Dispatched, e =>
            ClassifyDispatched(e) == (T3, RecordedEventType.ThreadStartCallback));
        var coreIdx = recorder.Dispatched.FindIndex(e => ClassifyDispatched(e) == (T2, RecordedEventType.ThreadStartCore));
        var callbackIdx = recorder.Dispatched.FindIndex(e => ClassifyDispatched(e) == (T3, RecordedEventType.ThreadStartCallback));
        Assert.True(coreIdx < callbackIdx);
    }

    [Fact]
    public void ReorderingPluginHost_FailedTryEnterViaByRef_NotDeferred_NotReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockTryAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.ExitWithByRefSuccess(T2, RecordedEventType.MonitorLockAcquireResult, success: false));

        // Assert
        Assert.Equal(4, recorder.Dispatched.Count);
        Assert.Equal((T2, RecordedEventType.MonitorLockAcquireResult), ClassifyDispatched(recorder.Dispatched[^1]));
    }

    [Fact]
    public void ReorderingPluginHost_FailedTryEnterViaByRef_DoesNotCreatePhantomOwner()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockTryAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.ExitWithByRefSuccess(T2, RecordedEventType.MonitorLockAcquireResult, success: false));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T3, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T3, RecordedEventType.MonitorLockAcquireResult));

        // Assert
        Assert.Contains(recorder.Dispatched, e => e.Metadata.Tid.Value == T3 && IsAcquireResult(e));
    }

    [Fact]
    public void ReorderingPluginHost_SuccessfulTryEnterViaByRef_IsDeferred_IsReordered()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.MonitorLockTryAcquire, L1));
        host.ProcessEvent(SyncEventBuilder.ExitWithByRefSuccess(T2, RecordedEventType.MonitorLockAcquireResult, success: true)); // deferred
        host.ProcessEvent(SyncEventBuilder.FieldRead(T2)); // deferred
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorLockRelease, L1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorLockReleaseResult));

        // Assert
        var kinds = recorder.Dispatched.Select(ClassifyDispatched).ToArray();
        Assert.Equal(new[]
        {
            (T1, RecordedEventType.MonitorLockAcquire),
            (T1, RecordedEventType.MonitorLockAcquireResult),
            (T2, RecordedEventType.MonitorLockTryAcquire),
            (T1, RecordedEventType.MonitorLockRelease),
            (T1, RecordedEventType.MonitorLockReleaseResult),
            (T2, RecordedEventType.MonitorLockAcquireResult),
            (T2, RecordedEventType.InstanceFieldRead),
        }, kinds);
    }

    private static (uint Tid, RecordedEventType Type) ClassifyDispatched(RecordedEvent e)
    {
        var tid = e.Metadata.Tid.Value;
        var type = e.EventArgs switch
        {
            MethodEnterWithArgumentsRecordedEvent enter => (RecordedEventType)enter.Interpretation,
            MethodExitRecordedEvent exit => (RecordedEventType)exit.Interpretation,
            MethodExitWithArgumentsRecordedEvent exit => (RecordedEventType)exit.Interpretation,
            ProfilerDestroyRecordedEvent => RecordedEventType.ProfilerDestroy,
            ThreadDestroyRecordedEvent => RecordedEventType.ThreadDestroy,
            _ => RecordedEventType.NotSpecified
        };
        return ((uint)tid, type);
    }

    private static bool IsAcquireResult(RecordedEvent e)
    {
        switch (e.EventArgs)
        {
            case MethodExitRecordedEvent exit:
            {
                var t = (RecordedEventType)exit.Interpretation;
                return t is RecordedEventType.MonitorLockAcquireResult or RecordedEventType.LockAcquireResult;
            }
            case MethodExitWithArgumentsRecordedEvent exitArg:
            {
                var t = (RecordedEventType)exitArg.Interpretation;
                return t is RecordedEventType.MonitorLockAcquireResult or RecordedEventType.LockAcquireResult;
            }
            default:
                return false;
        }
    }

    private static bool IsSemaphoreAcquireResult(RecordedEvent e)
        => e.EventArgs switch
        {
            MethodExitRecordedEvent exit =>
                (RecordedEventType)exit.Interpretation == RecordedEventType.SemaphoreAcquireResult,
            MethodExitWithArgumentsRecordedEvent exit =>
                (RecordedEventType)exit.Interpretation == RecordedEventType.SemaphoreAcquireResult,
            _ => false,
        };
}
