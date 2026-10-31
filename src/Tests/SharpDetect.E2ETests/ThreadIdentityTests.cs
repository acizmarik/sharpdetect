// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public partial class ThreadIdentityTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_ThreadIdentityTests";

    // Keep in sync with Program.ThreadIdentityRacyIterations in SharpDetect.E2ETests.Subject
    private const int RacyIterations = 40;
    // Keep in sync with Program.ThreadIdentitySequentialThreads in SharpDetect.E2ETests.Subject
    private const int SequentialThreads = 300;

    private static readonly string[] SkipSystemAssemblies = ["System.", "Microsoft."];

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectEveryDataRace_GarbageCollectionBetweenRacingWrites(string sdk, string plugin)
        => AssertDetectsEveryIterationRace("Test_DataRace_ThreadIdentity_GarbageCollectionBetweenRacingWrites", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectEveryDataRace_GarbageCollectionBetweenRacingWrites_NoThreadExits(string sdk, string plugin)
        => AssertDetectsEveryIterationRace("Test_DataRace_ThreadIdentity_GarbageCollectionBetweenRacingWrites_NoThreadExits", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.AllWithFastTrackOnly), MemberType = typeof(SdkVersions))]
    public Task CanDetectEveryDataRace_ExitedThreadNotJoined(string sdk, string plugin)
        => AssertDetectsEveryIterationRace("Test_DataRace_ThreadIdentity_ExitedThreadNotJoined", sdk, plugin);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task ThreadsHaveDistinctIds_WhenRuntimeReusesThreadIds(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_ThreadIdentity_SequentialThreadsWithGarbageCollections")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var created = new List<ThreadId>();
        var destroyed = new List<ThreadId>();
        plugin.ThreadCreated += e => created.Add(e.Args.ThreadId);
        plugin.ThreadDestroyed += e => destroyed.Add(e.Args.ThreadId);

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(created.Count >= SequentialThreads);
        Assert.Equal(created.Count, created.Distinct().Count());
        Assert.NotEmpty(destroyed);
        Assert.All(destroyed, id => Assert.Contains(id, created));
        Assert.Equal(destroyed.Count, destroyed.Distinct().Count());
    }

    private async Task AssertDetectsEveryIterationRace(string subjectArgs, string sdk, string pluginFullTypeName)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithTestPlugin(pluginFullTypeName)
            .WithPluginConfiguration(new { SkipInstrumentationForAssemblies = SkipSystemAssemblies })
            .Build(sdk, testOutput, additionalData);
        var plugin = services.GetService<TestFastTrackPlugin>()
            ?? throw new InvalidOperationException($"Plugin must be a {nameof(TestFastTrackPlugin)}.");
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);
        var summary = plugin.CreateDiagnostics();
        var subjectReports = plugin.GetSubjectReports().ToList();

        // Assert
        Assert.Empty(outcome.Gaps);
        var occurrences = summary.GetCollectionProperties().Single(p => p.Key == "Raw Race Occurrences").Value;
        Assert.Equal(RacyIterations.ToString(), occurrences);
        Assert.NotEmpty(subjectReports);
        Assert.All(subjectReports, report => Assert.EndsWith("ThreadIdentityBox.Value", report.Target));
        Assert.All(
            subjectReports.SelectMany(report => report.GetReportedThreads()),
            thread => Assert.Matches(ThreadNameRegex(), thread.Name));
    }

    [GeneratedRegex(@"^T\d+( \(.+\))?$")]
    private static partial Regex ThreadNameRegex();
}
