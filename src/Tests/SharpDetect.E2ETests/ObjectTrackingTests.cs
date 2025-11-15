// Copyright 2025 Andrej Čižmárik and Contributors
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
#if DEBUG
    [InlineData($"{ConfigurationFolder}/ObjectTracking_SingleGC_Debug.json")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Debug.json")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Compacting_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/ObjectTracking_SingleGC_Release.json")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Release.json")]
    [InlineData($"{ConfigurationFolder}/ObjectTracking_MultiGC_Compacting_Release.json")]
#endif
    public async Task ObjectsTracking(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enteredTest = false;
        var exitedTest = false;
        var insideTestMethod = false;
        var lockObjects = new HashSet<Lock>();
        plugin.MethodEntered += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
            {
                insideTestMethod = true;
                enteredTest = true;
            }
        };
        plugin.MethodExited += e =>
        {
            var method = plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.MethodToken);
            if (method.Name.StartsWith("Test_"))
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
