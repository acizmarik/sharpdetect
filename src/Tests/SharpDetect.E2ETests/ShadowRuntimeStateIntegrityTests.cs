// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events;
using SharpDetect.Core.Plugins;
using SharpDetect.E2ETests.Utils;
using SharpDetect.TemporalAsserts;
using SharpDetect.TemporalAsserts.TemporalOperators;
using SharpDetect.Worker;
using SharpDetect.Worker.Commands.Run;
using Xunit;
using Xunit.Abstractions;
using static SharpDetect.E2ETests.TemporalAssertionBuilders;

namespace SharpDetect.E2ETests;

[Collection(CollectionName)]
public class ShadowRuntimeStateIntegrityTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_ShadowRuntimeStateIntegrityTests";

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_SyncMethodThrowsInsideTaskBody(string sdk)
    {
        return ShadowStateIntegrityTest("Test_ShadowCallstack_SyncMethodThrowsInsideTaskBody", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_FaultedTaskJoinThrows(string sdk)
    {
        return ShadowStateIntegrityTest("Test_ShadowCallstack_FaultedTaskJoinThrows", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task ShadowCallstack_ReflectionInvokedMethodThrowsInsideTaskBody(string sdk)
    {
        return ShadowStateIntegrityTest("Test_ShadowCallstack_ReflectionInvokedMethodThrowsInsideTaskBody", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task ShadowCallstack_SemaphoreWaitCanceledWhileBlocked(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_ShadowCallstack_SemaphoreWaitCanceledWhileBlocked")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquire))
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquireResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        var result = Assert.Single(events
            .Where(e => e.Type == RecordedEventType.SemaphoreAcquireResult)
            .Select(e => e.Get<(RecordedEventMetadata Metadata, PermitAcquireResultArgs Args)>().Args));
        Assert.False(result.IsSuccess);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task ShadowCallstack_ValueTypeDictionaryMissingKey(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_ShadowCallstack_ValueTypeDictionaryMissingKey")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task ShadowStateIntegrityTest(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyEventType(RecordedEventType.TaskComplete))
            .Then(EventuallyEventType(RecordedEventType.TaskJoinFinish))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
