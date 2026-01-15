// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class ObjectTrackingTests(ITestOutputHelper testOutput)
{
    private const string TestMethodName = "Main";
    private const string ConfigurationFolder = "ObjectTrackingTestConfigurations";

    [Theory]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_SingleGC.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_SingleGC.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_SingleGC.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Compacting.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Compacting.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Compacting.json", "net10.0")]
    public async Task ObjectsTracking(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enteredTest = false;
        var exitedTest = false;
        var insideTestMethod = false;
        var lockObjects = new HashSet<ShadowLock>();
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
                lockObjects.Add(e.LockObj);
        };
        plugin.LockAcquireReturned += e =>
        {
            if (insideTestMethod)
                lockObjects.Add(e.LockObj);
        };
        plugin.LockReleased += e =>
        {
            if (insideTestMethod)
                lockObjects.Add(e.LockObj);
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(enteredTest);
        Assert.True(exitedTest);
        Assert.Single(lockObjects);
    }
}
