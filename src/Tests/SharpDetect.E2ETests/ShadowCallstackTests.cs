// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Metadata;
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
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_TryEnter_LockNotTaken.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_TryEnter_LockNotTaken.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_TryEnter_LockNotTaken.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Pulse.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Pulse.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Pulse.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_PulseAll.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_PulseAll.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_PulseAll.json", "net10.0")]
    public async Task ShadowCallstack_IntegrityTest(string configuration, string sdk)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, sdk, testOutput);
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var eventsDeliveryContext = services.GetRequiredService<IRecordedEventsDeliveryContext>();
        var metadataContext = services.GetRequiredService<IMetadataContext>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        
        // Assert
        AssertRuntimeStateIsClean(plugin, eventsDeliveryContext, metadataContext);
    }
    
    private static void AssertRuntimeStateIsClean(
        TestHappensBeforePlugin plugin,
        IRecordedEventsDeliveryContext eventsDeliveryContext,
        IMetadataContext metadataContext)
    {
        Assert.False(eventsDeliveryContext.HasBlockedThreads());
        Assert.False(eventsDeliveryContext.HasUnblockedThreads());
        Assert.False(eventsDeliveryContext.HasAnyUndeliveredEvents());
        
        var runtimeState = plugin.DumpRuntimeState();
        
        // Check shadow callstacks are empty
        var threadsWithLeakedFrames = new List<string>();
        foreach (var (threadId, callstack) in runtimeState.Callstacks.Where(kvp => kvp.Value.Count > 0))
        {
            var resolver = metadataContext.GetResolver(threadId.ProcessId);
            var frameDescriptions = (from frame in callstack
                let methodResolveResult = resolver.ResolveMethod(threadId.ProcessId, frame.ModuleId, frame.MethodToken)
                select methodResolveResult.IsSuccess
                    ? methodResolveResult.Value.FullName
                    : $"<unknown method: module = {frame.ModuleId.Value}, token = {frame.MethodToken.Value}>"
                into methodName
                select $"\tat {methodName}").ToList();
            
            var frames = string.Join(Environment.NewLine, frameDescriptions);
            if (!ShouldReportThread(threadId, plugin))
            {
                // Ignore not our threads
                continue;
            }

            var threadName = plugin.GetThreadName(threadId);
            var leakInfo = $"Thread \"{threadName}\": {callstack.Count} leaked frame(s):{Environment.NewLine}{frames}";
            threadsWithLeakedFrames.Add(leakInfo);
        }
        
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
            .Where(l => ShouldReportThread(l.Owner!.Value, plugin))
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

    private static bool ShouldReportThread(ProcessThreadId processThreadId, TestHappensBeforePlugin plugin)
    {
        return plugin.GetThreadName(processThreadId).StartsWith("TEST_");
    }
}

