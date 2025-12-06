// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.Worker;
using Xunit;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class ShadowCallstackTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "ShadowCallstackTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse_Debug.json")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_TryEnter_LockNotTaken_Debug.json")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Pulse_Debug.json")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_PulseAll_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse_Release.json")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_TryEnter_LockNotTaken_Release.json")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Pulse_Release.json")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_PulseAll_Release.json")]
#endif
    public async Task ShadowCallstack_IntegrityTest(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var eventsDeliveryContext = services.GetRequiredService<IRecordedEventsDeliveryContext>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        
        // Assert
        AssertRuntimeStateIsClean(plugin, eventsDeliveryContext);
    }
    
    private static void AssertRuntimeStateIsClean(
        TestHappensBeforePlugin plugin,
        IRecordedEventsDeliveryContext eventsDeliveryContext)
    {
        Assert.False(eventsDeliveryContext.HasBlockedThreads());
        Assert.False(eventsDeliveryContext.HasUnblockedThreads());
        Assert.False(eventsDeliveryContext.HasAnyUndeliveredEvents());
        
        var runtimeState = plugin.DumpRuntimeState();
        
        // Check shadow callstacks are empty
        var threadsWithLeakedFrames = runtimeState.Callstacks
            .Where(kvp => kvp.Value.Count > 0)
            .Select(kvp => $"Thread {kvp.Key.ProcessId}:{kvp.Key.ThreadId.Value}: {kvp.Value.Count} leaked frame(s)")
            .ToList();
        
        if (threadsWithLeakedFrames.Count != 0)
        {
            var message =
                $"""
                 Shadow callstack leak detected:
                 {string.Join(Environment.NewLine, threadsWithLeakedFrames)}
                 """;
            Assert.Fail(message);
        }
        
        // Check no locks are still held (unreleased locks)
        var locksStillHeld = runtimeState.Locks
            .Where(l => l.Owner != null)
            .Select(l => $"Lock objectId = {l.LockObjectId.ObjectId.Value} held by thread {l.Owner!.Value.ProcessId}:{l.Owner!.Value.ThreadId.Value} with reentrancy count {l.LocksCount}")
            .ToList();
        
        if (locksStillHeld.Count != 0)
        {
            var message =
                $"""
                 Unreleased locks detected:
                 {string.Join(Environment.NewLine, locksStillHeld)}
                 """;
            Assert.Fail(message);
        }
    }
}

