// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using SharpDetect.Worker.Commands.Run;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class TestProjectIntegrationTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_TestProjectIntegrationTests";

    private const string MtpRelativePath =
        "../../../../../Samples/SimpleDataRaceTestsMtp/bin/%BUILD_CONFIGURATION%/%SDK%/SimpleDataRaceTestsMtp.dll";

    private const string VsTestRelativePath =
        "../../../../../Samples/SimpleDataRaceTestsVSTest/bin/%BUILD_CONFIGURATION%/%SDK%/SimpleDataRaceTestsVSTest.dll";

    private static readonly string[] SkipSystemAssemblies = [ "System.", "Microsoft.", "TUnit.", "xunit.", "Newtonsoft." ];

    [Fact]
    public Task Mtp_RaceFact_Detects_Race()
        => AssertDetectsRace(MtpRelativePath, TestRunner.Mtp, "/*/*/*/RaceFact");

    [Fact]
    public Task Mtp_CleanFact_Detects_No_Race()
        => AssertDoesNotDetectRace(MtpRelativePath, TestRunner.Mtp, "/*/*/*/CleanFact");

    [Fact]
    public Task VSTest_RaceFact_Detects_Race()
        => AssertDetectsRace(VsTestRelativePath, TestRunner.VsTest, "FullyQualifiedName~RaceFact");

    [Fact]
    public Task VSTest_CleanFact_Detects_No_Race()
        => AssertDoesNotDetectRace(VsTestRelativePath, TestRunner.VsTest, "FullyQualifiedName~CleanFact");

    private Task AssertDetectsRace(string relativePath, TestRunner runner, string filter)
        => RunAndAssert(relativePath, runner, filter, expectRace: true);

    private Task AssertDoesNotDetectRace(string relativePath, TestRunner runner, string filter)
        => RunAndAssert(relativePath, runner, filter, expectRace: false);

    private async Task RunAndAssert(string relativePath, TestRunner runner, string filter, bool expectRace)
    {
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs: string.Empty)
            .WithCustomTarget(
                relativeAssemblyPath: relativePath,
                kind: TargetKind.TestAssembly,
                test: new TestTargetConfigurationArgs(runner: runner, filter: filter))
            .WithTestPlugin(typeof(TestFastTrackPlugin).FullName!)
            .WithPluginConfiguration(new { SkipInstrumentationForAssemblies = SkipSystemAssemblies })
            .Build("net10.0", testOutput, additionalData);
        var plugin = services.GetRequiredService<TestFastTrackPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        await analysisWorker.ExecuteAsync(CancellationToken.None);

        var subjectReports = plugin.GetSubjectReports().ToList();
        if (expectRace)
        {
            Assert.NotEmpty(subjectReports);
            Assert.Equal(plugin.ReportCategory, subjectReports[0].Category);
        }
        else
        {
            Assert.Empty(subjectReports);
        }
    }
}
