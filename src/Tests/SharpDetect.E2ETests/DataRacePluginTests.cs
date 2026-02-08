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
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_ReadWriteRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ReferenceType_Static_ReadWriteRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_ReadWriteRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ValueType_Static_ReadWriteRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Static_WriteReadRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ReferenceType_Static_WriteReadRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Static_WriteReadRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ValueType_Static_WriteReadRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Instance_ReadWriteRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ReferenceType_Instance_ReadWriteRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Instance_ReadWriteRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ValueType_Instance_ReadWriteRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ReferenceType_Instance_WriteReadRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ReferenceType_Instance_WriteReadRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_ValueType_Instance_WriteReadRace)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_ValueType_Instance_WriteReadRace(string configuration, string sdk)
    {
        return AssertDetectsDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Static_ReadReadNoRace)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ReferenceType_Static_ReadReadNoRace(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Static_ReadReadNoRace)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ValueType_Static_ReadReadNoRace(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ReferenceType_Instance_ReadReadNoRace)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ReferenceType_Instance_ReadReadNoRace(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ValueType_Instance_ReadReadNoRace)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ValueType_Instance_ReadReadNoRace(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ReferenceType)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ReferenceType)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ReferenceType)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ThreadStatic_ReferenceType(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ValueType)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ValueType)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ValueType)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ThreadStatic_ValueType(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ReadWrite)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ReadWrite)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_ThreadStatic_ReadWrite)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_ThreadStatic_ReadWrite(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_StaticDelegate_WithSuppression)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_StaticDelegate_WithSuppression)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_NoDataRace_StaticDelegate_WithSuppression)}.json", "net10.0")]
    public Task EraserPlugin_NoDataRace_StaticDelegate_WithSuppression(string configuration, string sdk)
    {
        return AssertDoesNotDetectDataRace(configuration, sdk);
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_StaticDelegate_WithoutSuppression)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_StaticDelegate_WithoutSuppression)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(EraserPlugin_CanDetectDataRace_StaticDelegate_WithoutSuppression)}.json", "net10.0")]
    public Task EraserPlugin_CanDetectDataRace_StaticDelegate_WithoutSuppression(string configuration, string sdk)
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
