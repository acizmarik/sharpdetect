// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Communication;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using SharpDetect.Worker.Services;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class AnalysisLifecycleTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_AnalysisLifecycleTests";
    private const string ShortSubject = "Test_MultiProcess_Child_LockAndExit";

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task AnalysisFailure_TerminatesTarget(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_Lifecycle_RunsUntilKilled")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var pluginHost = new FailingPluginHost(services.GetRequiredService<IPluginHost>());
        var analysisWorker = ActivatorUtilities.CreateInstance<AnalysisWorker>(services, pluginHost);

        // Act
        await Assert.ThrowsAsync<AnalysisFailedException>(() => analysisWorker.ExecuteAsync(CancellationToken.None).AsTask());

        // Assert
        Assert.NotNull(pluginHost.FailedPid);
        Assert.False(IsRunning((int)pluginHost.FailedPid.Value), "Target process outlived the failed analysis.");
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task CompletedTarget_AnalysisIsComplete(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(ShortSubject)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Act
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Empty(outcome.Gaps);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task ProfilerDisabled_ReportsProfilerNotAttached(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(ShortSubject)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .WithTargetEnvironmentVariables(new KeyValuePair<string, string>("CORECLR_ENABLE_PROFILING", "0"))
            .Build(sdk, testOutput);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Act
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var gap = Assert.Single(outcome.Gaps);
        Assert.Equal(AnalysisGapKind.ProfilerNotAttached, gap.Kind);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task KilledTarget_ReportsProcessNotFinished(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_Lifecycle_KillsItself")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Act
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var gap = Assert.Single(outcome.Gaps);
        Assert.Equal(AnalysisGapKind.ProcessNotFinished, gap.Kind);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task UnhandledExceptionInTarget_ReportsProcessNotFinished(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_Lifecycle_ThrowsUnhandledException")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Act
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var gap = Assert.Single(outcome.Gaps);
        Assert.Equal(AnalysisGapKind.ProcessNotFinished, gap.Kind);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task ReceiverCannotAttach_ReportsReceiverNotAttached(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(ShortSubject)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var analysisWorker = ActivatorUtilities.CreateInstance<AnalysisWorker>(services, new UnattachableReceiverProvider());

        // Act
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var gap = Assert.Single(outcome.Gaps);
        Assert.Equal(AnalysisGapKind.ReceiverNotAttached, gap.Kind);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task ReceiverFailure_FailsAnalysis(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(ShortSubject)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var receiverProvider = new FailingReceiverProvider(services.GetRequiredService<IProfilerEventReceiverProvider>());
        var analysisWorker = ActivatorUtilities.CreateInstance<AnalysisWorker>(services, receiverProvider);

        // Act
        var exception = await Assert.ThrowsAsync<AnalysisFailedException>(() => analysisWorker.ExecuteAsync(CancellationToken.None).AsTask());

        // Assert
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task DroppedEvents_ReportsEventsDropped(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(ShortSubject)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var receiverProvider = new DroppingReceiverProvider(services.GetRequiredService<IProfilerEventReceiverProvider>(), droppedRecords: 3);
        var analysisWorker = ActivatorUtilities.CreateInstance<AnalysisWorker>(services, receiverProvider);

        // Act
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var gap = Assert.Single(outcome.Gaps);
        Assert.Equal(AnalysisGapKind.EventsDropped, gap.Kind);
        Assert.StartsWith("3 ", gap.Description);
    }

    private static bool IsRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return !process.HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private sealed class FailingPluginHost(IPluginHost inner) : IPluginHost
    {
        public uint? FailedPid { get; private set; }

        public RecordedEventState ProcessEvent(RecordedEvent recordedEvent)
        {
            if (recordedEvent.EventArgs is not ProfilerInitializeRecordedEvent)
                return inner.ProcessEvent(recordedEvent);

            FailedPid = recordedEvent.Metadata.Pid;
            return RecordedEventState.Failed;
        }
    }

    private sealed class UnattachableReceiverProvider : IProfilerEventReceiverProvider
    {
        public IProfilerEventReceiver Create(uint pid)
            => throw new IOException($"Cannot open the event queue of process {pid}.");
    }

    private sealed class FailingReceiverProvider(IProfilerEventReceiverProvider inner) : IProfilerEventReceiverProvider
    {
        public IProfilerEventReceiver Create(uint pid) => new FailingReceiver(inner.Create(pid));
    }

    private sealed class DroppingReceiverProvider(IProfilerEventReceiverProvider inner, ulong droppedRecords) : IProfilerEventReceiverProvider
    {
        public IProfilerEventReceiver Create(uint pid) => new DroppingReceiver(inner.Create(pid), droppedRecords);
    }

    private sealed class DroppingReceiver(IProfilerEventReceiver inner, ulong droppedRecords) : IProfilerEventReceiver, IDisposable
    {
        public int TryReceiveNotifications(Span<RecordedEvent> destination, out int failedRecordsCount)
        {
            var count = inner.TryReceiveNotifications(destination, out failedRecordsCount);
            for (var i = 0; i < count; i++)
            {
                if (destination[i].EventArgs is ProfilerDestroyRecordedEvent)
                    destination[i] = destination[i] with { EventArgs = new ProfilerDestroyRecordedEvent(droppedRecords) };
            }

            return count;
        }

        public void Dispose() => (inner as IDisposable)?.Dispose();
    }

    private sealed class FailingReceiver(IProfilerEventReceiver inner) : IProfilerEventReceiver, IDisposable
    {
        public int TryReceiveNotifications(Span<RecordedEvent> destination, out int failedRecordsCount)
            => throw new InvalidOperationException("Event receiver failed.");

        public void Dispose() => (inner as IDisposable)?.Dispose();
    }
}
