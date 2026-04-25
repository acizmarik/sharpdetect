// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class MultiProcessTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_MultiProcessTests";
    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MultiProcess_ChildExitsBeforeParent(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_MultiProcess_ChildExitsBeforeParent")
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var initializedPids = new System.Collections.Concurrent.ConcurrentBag<uint>();
        plugin.ProfilerInitialized += e => initializedPids.Add(e.Metadata.Pid);
        var loadedPids = new System.Collections.Concurrent.ConcurrentBag<uint>();
        plugin.ProfilerLoaded += e => loadedPids.Add(e.Metadata.Pid);

        // Act
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var distinctLoaded = loadedPids.Distinct().ToList();
        Assert.Equal(2, distinctLoaded.Count);
        var distinctInitialized = initializedPids.Distinct().ToList();
        Assert.Equal(2, distinctInitialized.Count);
    }
}
