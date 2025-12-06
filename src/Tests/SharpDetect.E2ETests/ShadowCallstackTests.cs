// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events;
using SharpDetect.E2ETests.Utils;
using SharpDetect.TemporalAsserts;
using SharpDetect.TemporalAsserts.TemporalOperators;
using SharpDetect.Worker;
using SharpDetect.Worker.Commands.Run;
using Xunit;
using Xunit.Abstractions;
using static SharpDetect.E2ETests.TemporalAssertionBuilders;

namespace SharpDetect.E2ETests;

[Collection("E2E")]
public class ShadowCallstackTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "ShadowCallstackTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/ShadowCallstack_Monitor_Wait_ReentrancyWithPulse_Release.json")]
#endif
    public async Task ShadowCallstack_Monitor_Wait_ReentrancyWithPulse(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.MonitorLockAcquire))
            .Then(EventuallyEventType(RecordedEventType.MonitorLockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.MonitorWaitAttempt))
            .Then(EventuallyEventType(RecordedEventType.MonitorWaitResult))
            .Then(EventuallyEventType(RecordedEventType.MonitorLockRelease))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}

