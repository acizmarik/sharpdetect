// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.PluginHost.Services.Strategies;
using Xunit;

namespace SharpDetect.PluginHost.Tests;

// .NET 11+
public class NestedLockOperationFilterPluginHostTests
{
    private const uint T1 = 1;
    private const uint T2 = 2;
    private const nuint Monitor1 = 0x1000;
    private const nuint InternalLock1 = 0x1001;
    private const nuint Semaphore1 = 0x1800;
    private const nuint WaitHandle1 = 0x1900;

    private static NestedLockOperationFilterPluginHost Build(out RecordingPluginHost recorder, ushort runtimeMajorVersion = 11)
    {
        recorder = new RecordingPluginHost();
        var host = new NestedLockOperationFilterPluginHost(recorder);
        host.ProcessEvent(ProfilerLoad(SyncEventBuilder.Pid, runtimeMajorVersion));
        recorder.Dispatched.Clear();
        return host;
    }

    private static RecordedEvent ProfilerLoad(uint pid, ushort runtimeMajorVersion)
        => new(
            new RecordedEventMetadata(pid, new ThreadId(T1)),
            new ProfilerLoadRecordedEvent(COR_PRF_RUNTIME_TYPE.COR_PRF_CORE_CLR, runtimeMajorVersion, 0, 0, 0));

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    public void NestedLockOperationFilter_RuntimeBefore11_ForwardsNestedLockOperations(ushort runtimeMajorVersion)
    {
        // Arrange
        var host = Build(out var recorder, runtimeMajorVersion);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.Unwound(T1, RecordedEventType.MonitorWaitResult));

        // Assert
        Assert.Equal(4, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_OtherProcessOnRuntime11_DoesNotEnableFiltering()
    {
        // Arrange
        var host = Build(out var recorder, runtimeMajorVersion: 10);
        host.ProcessEvent(ProfilerLoad(SyncEventBuilder.Pid + 1, runtimeMajorVersion: 11));
        recorder.Dispatched.Clear();

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));

        // Assert
        Assert.Equal(4, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_TopLevelLockOperations_AreForwarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockTryAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.LockAcquireResult, success: true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, Monitor1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, success: true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, Monitor1));

        // Assert
        Assert.Equal(7, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_AcquireNestedInMonitorEnter_IsDiscarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act - Monitor.Enter on an inflated monitor calls Lock.Enter on its internal lock (.NET 11+)
        var states = new[]
        {
            host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1)),
            host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1)),
            host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult)),
            host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult))
        };

        // Assert
        Assert.Equal(
            [RecordedEventState.Executed, RecordedEventState.Discarded, RecordedEventState.Discarded, RecordedEventState.Executed],
            states);
        Assert.Equal(
            [(T1, RecordedEventType.LockAcquire, Monitor1), (T1, RecordedEventType.LockAcquireResult, (nuint)0)],
            recorder.Dispatched.Select(Classify));
    }

    [Fact]
    public void NestedLockOperationFilter_ReleaseNestedInMonitorExit_IsForwarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, InternalLock1));

        // Assert
        Assert.Equal(
            [(T1, RecordedEventType.LockRelease, Monitor1), (T1, RecordedEventType.LockRelease, InternalLock1)],
            recorder.Dispatched.Select(Classify));
    }

    [Fact]
    public void NestedLockOperationFilter_ReleaseNestedInMonitorWait_IsDiscarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, success: true));

        // Assert
        Assert.Equal(
            [(T1, RecordedEventType.MonitorWaitAttempt, Monitor1), (T1, RecordedEventType.MonitorWaitResult, (nuint)0)],
            recorder.Dispatched.Select(Classify));
    }

    [Fact]
    public void NestedLockOperationFilter_OperationsAfterRelease_AreForwarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, Monitor1));

        // Assert
        Assert.Equal(6, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_ReacquireNestedInMonitorWait_IsDiscarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, success: true));

        // Assert
        Assert.Equal(
            [(T1, RecordedEventType.MonitorWaitAttempt, Monitor1), (T1, RecordedEventType.MonitorWaitResult, (nuint)0)],
            recorder.Dispatched.Select(Classify));
    }

    [Fact]
    public void NestedLockOperationFilter_PulseNestedInMonitorPulse_IsDiscarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorPulseAllAttempt, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorPulseAllAttempt, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorPulseAllResult));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.MonitorPulseAllResult));

        // Assert
        Assert.Equal(
            [(T1, RecordedEventType.MonitorPulseAllAttempt, Monitor1), (T1, RecordedEventType.MonitorPulseAllResult, (nuint)0)],
            recorder.Dispatched.Select(Classify));
    }

    [Fact]
    public void NestedLockOperationFilter_NestedOperationUnwound_KeepsOuterOperationForwarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Unwound(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.Unwound(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));

        // Assert
        Assert.Equal(
            [
                (T1, RecordedEventType.LockAcquire, Monitor1),
                (T1, RecordedEventType.LockAcquireResult, (nuint)0),
                (T1, RecordedEventType.LockAcquire, Monitor1),
                (T1, RecordedEventType.LockAcquireResult, (nuint)0)
            ],
            recorder.Dispatched.Select(Classify));
    }

    [Fact]
    public void NestedLockOperationFilter_LockOperationInsideOtherSynchronization_IsForwarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.SemaphoreTryAcquire, Semaphore1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, Monitor1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.SemaphoreAcquireResult, success: true));

        // Assert
        Assert.Equal(5, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_LockOperationInsideWaitOfLockOperation_IsForwarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.WaitHandleWait, WaitHandle1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockRelease, Monitor1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.WaitHandleWaitResult, success: true));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));

        // Assert
        Assert.Equal(7, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_LockOperationAfterWaitOfLockOperation_IsDiscarded()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.MonitorWaitAttempt, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.WaitHandleWait, WaitHandle1));
        host.ProcessEvent(SyncEventBuilder.Unwound(T1, RecordedEventType.WaitHandleWaitResult));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.MonitorWaitResult, success: true));

        // Assert
        Assert.Equal(
            [
                (T1, RecordedEventType.MonitorWaitAttempt, Monitor1),
                (T1, RecordedEventType.WaitHandleWait, WaitHandle1),
                (T1, RecordedEventType.WaitHandleWaitResult, (nuint)0),
                (T1, RecordedEventType.MonitorWaitResult, (nuint)0)
            ],
            recorder.Dispatched.Select(Classify));
    }

    [Fact]
    public void NestedLockOperationFilter_WaitOutsideLockOperation_DoesNotAffectLockOperations()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.WaitHandleWait, WaitHandle1));
        host.ProcessEvent(SyncEventBuilder.ExitWithSuccess(T1, RecordedEventType.WaitHandleWaitResult, success: true));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));

        // Assert
        Assert.Equal(4, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_LockOperationsOfOtherThreads_AreIndependent()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T2, RecordedEventType.LockAcquire, InternalLock1));
        host.ProcessEvent(SyncEventBuilder.Exit(T2, RecordedEventType.LockAcquireResult));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));

        // Assert
        Assert.Equal(4, recorder.Dispatched.Count);
    }

    [Fact]
    public void NestedLockOperationFilter_ThreadDestroyed_ForgetsActiveOperations()
    {
        // Arrange
        var host = Build(out var recorder);

        // Act
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.ThreadDestroy(T1));
        host.ProcessEvent(SyncEventBuilder.EnterWithTarget(T1, RecordedEventType.LockAcquire, Monitor1));
        host.ProcessEvent(SyncEventBuilder.Exit(T1, RecordedEventType.LockAcquireResult));

        // Assert
        Assert.Equal(4, recorder.Dispatched.Count);
    }

    private static (uint Tid, RecordedEventType Type, nuint Target) Classify(RecordedEvent e)
    {
        var tid = (uint)e.Metadata.Tid.Value;
        return e.EventArgs switch
        {
            MethodEnterWithArgumentsRecordedEvent enter =>
                (tid, (RecordedEventType)enter.Interpretation, (nuint)BitConverter.ToUInt64(enter.ArgumentValues)),
            MethodExitRecordedEvent exit => (tid, (RecordedEventType)exit.Interpretation, 0),
            MethodExitWithArgumentsRecordedEvent exit => (tid, (RecordedEventType)exit.Interpretation, 0),
            MethodUnwoundRecordedEvent unwound => (tid, (RecordedEventType)unwound.Interpretation, 0),
            _ => (tid, RecordedEventType.NotSpecified, 0)
        };
    }
}
