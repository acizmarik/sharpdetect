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
    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_Monitor_Wait_ReentrancyWithPulse(string sdk)
    {
        return IntegrityTest("Test_ShadowCallstack_MonitorWait_ReentrancyWithPulse", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_Monitor_TryEnter_LockNotTaken(string sdk)
    {
        return IntegrityTest("Test_ShadowCallstack_MonitorTryEnter_LockNotTaken", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_Monitor_Pulse(string sdk)
    {
        return IntegrityTest("Test_ShadowCallstack_MonitorPulse", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_Monitor_PulseAll(string sdk)
    {
        return IntegrityTest("Test_ShadowCallstack_MonitorPulseAll", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net10Only), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_Monitor_ExitIfLockTaken(string sdk)
    {
        return IntegrityTest("Test_ShadowCallstack_MonitorExitIfLockTaken", sdk);
    }

    private async Task IntegrityTest(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var eventsDeliveryContext = services.GetRequiredService<IRecordedEventsDeliveryContext>();
        var metadataContext = services.GetRequiredService<IMetadataContext>();

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        
        // Assert
        AssertRuntimeStateIsClean(plugin, eventsDeliveryContext, metadataContext);
    }
    
    private static void AssertRuntimeStateIsClean(
        TestExecutionOrderingPlugin plugin,
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

    private static bool ShouldReportThread(ProcessThreadId processThreadId, TestExecutionOrderingPlugin plugin)
    {
        return plugin.GetThreadName(processThreadId).StartsWith("TEST_");
    }
}
