// Copyright 2026 Andrej Čižmárik and Contributors
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
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_EnterExit2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_TryEnterExit3.json", "net10.0")]
    public async Task MethodInterpretation_Monitor_EnterExit(string configuration, string sdk)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait3_Reentrancy.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait3_Reentrancy.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_Wait3_Reentrancy.json", "net10.0")]
    public async Task MethodInterpretation_Monitor_Wait(string configuration, string sdk)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.MonitorWaitAttempt))
            .Then(EventuallyEventType(RecordedEventType.MonitorWaitResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join3.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join3.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_Join3.json", "net10.0")]
    public async Task MethodInterpretation_Thread_Join(string configuration, string sdk)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, sdk, testOutput);
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
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_StartCallback2.json", "net10.0")]
    public async Task MethodInterpretation_Thread_StartCallback(string configuration, string sdk)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, sdk, testOutput);
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
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_get_CurrentThread.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_get_CurrentThread.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Thread_get_CurrentThread.json", "net10.0")]
    public async Task MethodInterpretation_Thread_CurrentThread(string configuration, string sdk)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, sdk, testOutput);
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
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_EnterExit1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_EnterExit1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_EnterExit2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_EnterExit2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_TryEnterExit1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_TryEnterExit1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_TryEnterExit2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Lock_TryEnterExit2.json", "net10.0")]
    public async Task MethodInterpretation_Lock_EnterExit(string configuration, string sdk)
    {
        // Arrange
        using var services = TestContextFactory.CreateServiceProvider(configuration, sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestHappensBeforePlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
