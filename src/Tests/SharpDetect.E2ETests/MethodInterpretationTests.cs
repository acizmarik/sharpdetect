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
public class MethodInterpretationTests(ITestOutputHelper testOutput)
{
    private const string ConfigurationFolder = "MethodInterpretationTestConfigurations";

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3_Release.json")]
#endif
    public async Task MethodInterpretation_Monitor_EnterExit(string configuration)
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
            .Then(EventuallyEventType(RecordedEventType.MonitorLockRelease))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
    
    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join1_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join2_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join3_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join1_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join2_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join3_Release.json")]
#endif
    public async Task MethodInterpretation_Thread_Join(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.ThreadJoinAttempt))
            .Then(EventuallyEventType(RecordedEventType.ThreadJoinResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback1_Debug.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback2_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback1_Release.json")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback2_Release.json")]
#endif
    public async Task MethodInterpretation_Thread_StartCallback(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.ThreadCreate))
            .Then(EventuallyEventType(RecordedEventType.ThreadStart))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));
        
        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        
        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
    
    [Theory]
#if DEBUG
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_get_CurrentThread_Debug.json")]
#elif RELEASE
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_get_CurrentThread_Debug.json")]
#endif
    public async Task MethodInterpretation_Thread_CurrentThread(string configuration)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.ThreadCreate))
            .Then(EventuallyEventType(RecordedEventType.ThreadStart))
            .Then(EventuallyEventType(RecordedEventType.ThreadMapping))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));
        
        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        
        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
