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
    private const string SynchronizationMutexName = "SharpDetect_E2E_Tests";
    private const string ExternalDeadlockPluginTypeName = "SharpDetect.Plugins.Deadlock.DeadlockPlugin";

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task DeadlockPlugin_NoDeadlock(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_NoDeadlock")
            .WithExternalPlugin(ExternalDeadlockPluginTypeName)
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<IPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var report = plugin.CreateDiagnostics().GetAllReports().FirstOrDefault();

        // Assert
        Assert.Null(report);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task DeadlockPlugin_CanDetectMonitorDeadlock(string sdk)
        => CanDetectDeadlock("Test_Deadlock_SimpleDeadlock_UsingMonitor", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public Task DeadlockPlugin_CanDetectLockDeadlock(string sdk)
        => CanDetectDeadlock("Test_Deadlock_SimpleDeadlock_UsingLock", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task DeadlockPlugin_CanDetectThreadJoinDeadlock(string sdk)
        => CanDetectDeadlock("Test_Deadlock_ThreadJoinDeadlock", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task DeadlockPlugin_CanDetectMixedDeadlock(string sdk)
        => CanDetectDeadlock("Test_Deadlock_MixedMonitorAndThreadJoinDeadlock", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.Net10Only), MemberType = typeof(SdkVersions))]
    public async Task DeadlockPlugin_CanRenderReport(string sdk)
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.MinValue);
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_Deadlock_SimpleDeadlock_UsingLock")
            .WithExternalPlugin(ExternalDeadlockPluginTypeName)
            .WithRenderReport()
            .Build(sdk, testOutput, additionalData, timeProvider);
        var plugin = services.GetRequiredService<IPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var reportRenderer = services.GetRequiredService<IReportSummaryRenderer>();
        var reportWriter = services.GetRequiredService<IReportSummaryWriter>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var summary = plugin.CreateDiagnostics();
        var context = new SummaryRenderingContext(summary, plugin, plugin.ReportTemplates, ConfigurationJson: string.Empty);
        var exception = await Record.ExceptionAsync(() => reportWriter.Write(context, reportRenderer, CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    private async Task CanDetectDeadlock(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestDeadlockPlugin>()
            .Build(sdk, testOutput);
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
