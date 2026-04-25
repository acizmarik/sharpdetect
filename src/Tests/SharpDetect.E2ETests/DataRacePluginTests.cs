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
    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Static_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Static_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Static_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Static_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Static_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Static_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Static_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Static_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Instance_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Instance_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Instance_ReadWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Instance_ReadWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Instance_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Instance_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Instance_WriteReadRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Instance_WriteReadRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ReferenceType_Instance_WriteWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ReferenceType_Instance_WriteWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task CanDetectDataRace_ValueType_Instance_WriteWriteRace(string sdk, string plugin)
        => AssertDetectsDataRace("Test_DataRace_ValueType_Instance_WriteWriteRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ReferenceType_Static_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ReferenceType_Static_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ValueType_Static_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ValueType_Static_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ReferenceType_Instance_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ReferenceType_Instance_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ValueType_Instance_ReadReadNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ValueType_Instance_ReadReadNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ThreadStatic_ReferenceType(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ThreadStatic_ReferenceType", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ThreadStatic_ValueType(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ThreadStatic_ValueType", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_ThreadStatic_ReadWrite(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_ThreadStatic_ReadWrite", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileField_Static_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileField_Static_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileField_Instance_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileField_Instance_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileExplicitAccess_Static_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_VolatileExplicitAccess_Instance_ReadWriteNoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Task_WriteInsideTask_ReadAfterTaskJoin", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_Task_SequentialTasks_WriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_Task_SequentialTasks_WriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_SemaphoreSlim_ProtectedWriteRead(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_SemaphoreSlim_ProtectedWriteRead", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public Task NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace(string sdk, string plugin)
        => AssertDoesNotDetectDataRace("Test_NoDataRace_GenericType_Static_DifferentInstantiations_WriteWrite_NoRace", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public async Task CanRenderReport(string sdk, string pluginFullTypeName)
    {
        // Arrange
        var timeProvider = new TestTimeProvider(DateTimeOffset.MinValue);
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_NoDataRace_ReferenceType_Static_CorrectLocks")
            .WithTestPlugin(pluginFullTypeName)
            .WithPluginConfiguration(new { SkipInstrumentationForAssemblies = SkipSystemAssemblies })
            .WithRenderReport()
            .Build(sdk, testOutput, additionalData, timeProvider);
        var plugin = GetTestPlugin(services);
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

    private static readonly string[] SkipSystemAssemblies = ["System.", "Microsoft."];

    private async Task AssertDetectsDataRace(string subjectArgs, string sdk, string pluginFullTypeName)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithTestPlugin(pluginFullTypeName)
            .WithPluginConfiguration(new { SkipInstrumentationForAssemblies = SkipSystemAssemblies })
            .Build(sdk, testOutput, additionalData);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.NotEmpty(subjectReports);
        var report = subjectReports.First();
        Assert.Equal(plugin.ReportCategory, report.Category);
    }

    private async Task AssertDoesNotDetectDataRace(string subjectArgs, string sdk, string pluginFullTypeName)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithTestPlugin(pluginFullTypeName)
            .WithPluginConfiguration(new { SkipInstrumentationForAssemblies = SkipSystemAssemblies })
            .Build(sdk, testOutput, additionalData);
        var plugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.Empty(subjectReports);
    }

    private ITestPlugin GetTestPlugin(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<TestEraserPlugin>() as ITestPlugin
            ?? serviceProvider.GetService<TestFastTrackPlugin>()
            ?? throw new InvalidOperationException($"Plugin must be either {nameof(TestEraserPlugin)} or {nameof(TestFastTrackPlugin)}.");
    }
}
