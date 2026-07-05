// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(DataRacePluginTests.CollectionName)]
public class ExtendedStackTraceTests(ITestOutputHelper testOutput)
{
    private const string DeepStackSubject = "Test_DataRace_DeepStack_HelperWriteRace";
    private const string DeepStackCallerName = "DeepStackCaller";
    private const string RacyFieldPattern =
        "SharpDetect.E2ETests.Subject.Helpers.DataRaces.DataRace.Test_DataRace_ReferenceType_Static";

    private static readonly string[] SkipSystemAssemblies = [ "System.", "Microsoft." ];

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public async Task DeepStack_WhenEnabled_ReportsMultipleFrames(string sdk, string plugin)
    {
        var reports = await RunDeepStackSubjectAsync(sdk, plugin, new
        {
            EnableFieldAccessStackTraces = true,
            FieldAccessStackTracesMaxDepth = 8,
            FieldAccessStackTracesFields = Array.Empty<string>(),
            SkipInstrumentationForAssemblies = SkipSystemAssemblies
        });

        var stackTraces = GetStackTraces(reports).ToList();
        Assert.Contains(stackTraces, st => st.Frames.Length > 1);
        Assert.Contains(
            stackTraces.SelectMany(st => st.Frames),
            frame => frame.MethodName.Contains(DeepStackCallerName));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public async Task DeepStack_WhenDisabledByDefault_ReportsSingleFrame(string sdk, string plugin)
    {
        var reports = await RunDeepStackSubjectAsync(sdk, plugin, new
        {
            SkipInstrumentationForAssemblies = SkipSystemAssemblies
        });

        var stackTraces = GetStackTraces(reports).ToList();
        Assert.NotEmpty(stackTraces);
        Assert.All(stackTraces, st => Assert.Single(st.Frames));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public async Task DeepStack_WithMatchingFieldFilter_ReportsMultipleFrames(string sdk, string plugin)
    {
        var reports = await RunDeepStackSubjectAsync(sdk, plugin, new
        {
            EnableFieldAccessStackTraces = true,
            FieldAccessStackTracesMaxDepth = 8,
            FieldAccessStackTracesFields = new[] { RacyFieldPattern },
            SkipInstrumentationForAssemblies = SkipSystemAssemblies
        });

        Assert.Contains(
            GetStackTraces(reports).SelectMany(st => st.Frames),
            frame => frame.MethodName.Contains(DeepStackCallerName));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net10WithBothDataRacePlugins), MemberType = typeof(SdkVersions))]
    public async Task DeepStack_WithNonMatchingFieldFilter_ReportsSingleFrame(string sdk, string plugin)
    {
        var reports = await RunDeepStackSubjectAsync(sdk, plugin, new
        {
            EnableFieldAccessStackTraces = true,
            FieldAccessStackTracesMaxDepth = 8,
            FieldAccessStackTracesFields = new[] { "NoSuch.Type.NoSuchField" },
            SkipInstrumentationForAssemblies = SkipSystemAssemblies
        });

        var stackTraces = GetStackTraces(reports).ToList();
        Assert.NotEmpty(stackTraces);
        Assert.All(stackTraces, st => Assert.Single(st.Frames));
    }

    private async Task<List<Report>> RunDeepStackSubjectAsync(string sdk, string plugin, object pluginConfiguration)
    {
        using var services = E2ETestBuilder
            .ForSubject(DeepStackSubject)
            .WithTestPlugin(plugin)
            .WithPluginConfiguration(pluginConfiguration)
            .Build(sdk, testOutput);
        var testPlugin = GetTestPlugin(services);
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        await analysisWorker.ExecuteAsync(CancellationToken.None);

        var reports = testPlugin.GetSubjectReports().ToList();
        Assert.NotEmpty(reports);
        return reports;
    }

    private static IEnumerable<StackTrace> GetStackTraces(IEnumerable<Report> reports)
    {
        foreach (var report in reports)
        {
            foreach (var threadInfo in report.GetReportedThreads())
            {
                if (report.TryGetStackTrace(threadInfo, out var stackTrace) && stackTrace is not null)
                    yield return stackTrace;
            }
        }
    }

    private static ITestPlugin GetTestPlugin(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetService<TestEraserPlugin>() as ITestPlugin
            ?? serviceProvider.GetService<TestFastTrackPlugin>()
            ?? throw new InvalidOperationException($"Plugin must be either {nameof(TestEraserPlugin)} or {nameof(TestFastTrackPlugin)}.");
    }
}
