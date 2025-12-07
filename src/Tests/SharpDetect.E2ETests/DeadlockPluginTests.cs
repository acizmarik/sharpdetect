// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class DeadlockPluginTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "DeadlockPluginTestConfigurations";
    private const string SynchronizationMutexName = "SharpDetect_E2E_Tests";
    
    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_NoDeadlock)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_NoDeadlock)}_Release.json")]
#endif
    public async Task DeadlockPlugin_NoDeadlock(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<IPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var eventsDeliveryContext = services.GetRequiredService<IRecordedEventsDeliveryContext>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.Null(report);
        Assert.False(eventsDeliveryContext.HasBlockedThreads());
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMonitorDeadlock)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMonitorDeadlock)}_Release.json")]
#endif
    public async Task DeadlockPlugin_CanDetectMonitorDeadlock(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        using var mutex = new Mutex(initiallyOwned: true, SynchronizationMutexName);
        var plugin = services.GetRequiredService<TestDeadlockPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        plugin.StackTraceSnapshotsCreated += _ => mutex.ReleaseMutex();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(plugin.ReportCategory, report.Category);
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectThreadJoinDeadlock)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectThreadJoinDeadlock)}_Release.json")]
#endif
    public async Task DeadlockPlugin_CanDetectThreadJoinDeadlock(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        using var mutex = new Mutex(initiallyOwned: true, SynchronizationMutexName);
        var plugin = services.GetRequiredService<TestDeadlockPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        plugin.StackTraceSnapshotsCreated += _ => mutex.ReleaseMutex();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(plugin.ReportCategory, report.Category);
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMixedDeadlock)}_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMixedDeadlock)}_Release.json")]
#endif
    public async Task DeadlockPlugin_CanDetectMixedDeadlock(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        using var mutex = new Mutex(initiallyOwned: true, SynchronizationMutexName);
        var plugin = services.GetRequiredService<TestDeadlockPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        plugin.StackTraceSnapshotsCreated += _ => mutex.ReleaseMutex();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.NotNull(report);
        Assert.Equal(plugin.ReportCategory, report.Category);
    }
}
