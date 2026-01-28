// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting;
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
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_NoDeadlock)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_NoDeadlock)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_NoDeadlock)}.json", "net10.0")]
    public async Task DeadlockPlugin_NoDeadlock(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var plugin = services.GetRequiredService<IPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.Null(report);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMonitorDeadlock)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMonitorDeadlock)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMonitorDeadlock)}.json", "net10.0")]
    public Task DeadlockPlugin_CanDetectMonitorDeadlock(string configuration, string sdk)
    {
        return CanDetectDeadlock(configuration, sdk);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectLockDeadlock)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectLockDeadlock)}.json", "net10.0")]
    public Task DeadlockPlugin_CanDetectLockDeadlock(string configuration, string sdk)
    {
        return CanDetectDeadlock(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectThreadJoinDeadlock)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectThreadJoinDeadlock)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectThreadJoinDeadlock)}.json", "net10.0")]
    public Task DeadlockPlugin_CanDetectThreadJoinDeadlock(string configuration, string sdk)
    {
        return CanDetectDeadlock(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMixedDeadlock)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMixedDeadlock)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanDetectMixedDeadlock)}.json", "net10.0")]
    public Task DeadlockPlugin_CanDetectMixedDeadlock(string configuration, string sdk)
    {
        return CanDetectDeadlock(configuration, sdk);
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(DeadlockPlugin_CanRenderReport)}.json", "net10.0")]
    public async Task DeadlockPlugin_CanRenderReport(string configuration, string sdk)
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.MinValue);
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput, timeProvider);
        var plugin = services.GetRequiredService<IPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var reportRenderer = services.GetRequiredService<IReportSummaryRenderer>();
        var reportWriter = services.GetRequiredService<IReportSummaryWriter>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var summary = plugin.CreateDiagnostics();
        var context = new SummaryRenderingContext(summary, plugin, plugin.ReportTemplates);
        var exception = await Record.ExceptionAsync(() => reportWriter.Write(context, reportRenderer, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }
    
    private async Task CanDetectDeadlock(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
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
