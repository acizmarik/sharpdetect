// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Reporting;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class DataRacePluginTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "DataRacePluginTestConfigurations";

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Static)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Static)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Static)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ReferenceType_Static(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Static)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Static)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Static)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ValueType_Static(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_SimpleRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_SimpleRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_SimpleRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ReferenceType_Static_SimpleRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_SimpleRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_SimpleRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_SimpleRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ValueType_Static_SimpleRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_BadLocking)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_BadLocking)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_BadLocking)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ReferenceType_Static_BadLocking(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_BadLocking)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_BadLocking)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_BadLocking)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ValueType_Static_BadLocking(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanRenderReport)}.json", "net10.0")]
    public async Task EraserPlugin_CanRenderReport(string configuration, string sdk)
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.MinValue);
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput, timeProvider);
        var plugin = services.GetRequiredService<TestEraserPlugin>();
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
    
    private async Task AssertDetectsDataRace(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var plugin = services.GetRequiredService<TestEraserPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.NotEmpty(subjectReports);
        var report = subjectReports.First();
        Assert.Equal(plugin.ReportCategory, report.Category);
    }

    private async Task AssertDoesNotDetectDataRace(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var plugin = services.GetRequiredService<TestEraserPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.Empty(subjectReports);
    }
}
