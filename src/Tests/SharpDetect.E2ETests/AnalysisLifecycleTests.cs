// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
}
