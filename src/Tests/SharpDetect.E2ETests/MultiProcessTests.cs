// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class MultiProcessTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "MultiProcessTestConfigurations";
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/{nameof(MultiProcess_ChildExitsBeforeParent)}.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(MultiProcess_ChildExitsBeforeParent)}.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/{nameof(MultiProcess_ChildExitsBeforeParent)}.json", "net10.0")]
    public async Task MultiProcess_ChildExitsBeforeParent(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
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
