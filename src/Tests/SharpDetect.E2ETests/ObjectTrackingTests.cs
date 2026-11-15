// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class ObjectTrackingTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_ObjectTrackingTests";

    // Keep in sync with Program.ShortLivedBoxRounds and Program.ShortLivedBoxesPerRound in SharpDetect.E2ETests.Subject
    private const int ShortLivedBoxRounds = 10;
    private const int ShortLivedBoxesPerRound = 500;

    private static readonly string[] SkipSystemAssemblies = ["System.", "Microsoft."];

    public static TheoryData<string, string> AllSdksWithEveryGarbageCollector
    {
        get
        {
            var data = new TheoryData<string, string>();
            foreach (var sdk in (string[])["net8.0", "net9.0", "net10.0"])
            {
                data.Add(sdk, "0");
                data.Add(sdk, "1");
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ObjectTracking_SingleGC(string sdk)
    {
        return ObjectsTracking("Test_SingleGarbageCollection_ObjectTracking_Simple", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ObjectTracking_MultiGC(string sdk)
    {
        return ObjectsTracking("Test_MultipleGarbageCollection_ObjectTracking_Simple", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ObjectTracking_MultiGC_Compacting(string sdk)
    {
        return ObjectsTracking("Test_SingleGarbageCollection_ObjectTracking_MovedLockedObject", sdk);
    }

    [Theory]
    [MemberData(nameof(AllSdksWithEveryGarbageCollector))]
    public async Task ObjectTracking_GarbageCollectionReleasesStateOfDeadObjects(string sdk, string serverGarbageCollector)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_GarbageCollection_ReleasesStateOfDeadObjects")
            .WithPlugin<TestFastTrackPlugin>()
            .WithPluginConfiguration(new { SkipInstrumentationForAssemblies = SkipSystemAssemblies })
            .WithTargetEnvironmentVariables(new KeyValuePair<string, string>("DOTNET_gcServer", serverGarbageCollector))
            .Build(sdk, testOutput, additionalData);
        var plugin = services.GetRequiredService<TestFastTrackPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();

        // Execute
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);
        var properties = plugin.CreateDiagnostics().GetCollectionProperties();
        var garbageCollections = int.Parse(properties.Single(p => p.Key == "Garbage Collections Count").Value);
        var shadowVariables = int.Parse(properties.Single(p => p.Key == "Tracked Shadow Variables").Value);

        // Assert
        Assert.Empty(outcome.Gaps);
        Assert.True(garbageCollections >= ShortLivedBoxRounds);
        Assert.True(shadowVariables < ShortLivedBoxesPerRound);
    }

    private async Task ObjectsTracking(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enteredTest = false;
        var exitedTest = false;
        var insideTestMethod = false;
        var lockObjects = new HashSet<ProcessTrackedObjectId>();
        plugin.MethodEntered += e =>
        {
            var resolveResult = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (resolveResult.IsSuccess && resolveResult.Value.Name.StartsWith("Test_"))
            {
                insideTestMethod = true;
                enteredTest = true;
            }
        };
        plugin.MethodExited += e =>
        {
            var resolveResult = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (resolveResult.IsSuccess && resolveResult.Value.Name.StartsWith("Test_"))
            {
                insideTestMethod = false;
                exitedTest = true;
            }
        };
        plugin.LockAcquireAttempted += e =>
        {
            if (insideTestMethod)
                lockObjects.Add(e.LockId);
        };
        plugin.LockAcquireReturned += e =>
        {
            if (insideTestMethod)
                lockObjects.Add(e.LockId);
        };
        plugin.LockReleased += e =>
        {
            if (insideTestMethod)
                lockObjects.Add(e.LockId);
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(enteredTest);
        Assert.True(exitedTest);
        Assert.Single(lockObjects);
    }
}
