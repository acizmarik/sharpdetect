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
