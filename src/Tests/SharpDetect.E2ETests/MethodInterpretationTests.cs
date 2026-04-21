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
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Monitor_ExitIfLockTaken.json", "net10.0")]
    public async Task MethodInterpretation_Monitor_EnterExit(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
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
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
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
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
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
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.ThreadStartCore))
            .Then(EventuallyEventType(RecordedEventType.ThreadCreate))
            .Then(EventuallyEventType(RecordedEventType.ThreadStartCallback))
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
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
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
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_ScheduleAndStart1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_ScheduleAndStart1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_ScheduleAndStart1.json", "net10.0")]
    public async Task MethodInterpretation_Task_ScheduleAndStart(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskSchedule))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_InnerInvoke1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_InnerInvoke1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_InnerInvoke1.json", "net10.0")]
    public async Task MethodInterpretation_Task_InnerInvoke(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyEventType(RecordedEventType.TaskComplete))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait3.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait3.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait3.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait4.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait4.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait4.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait5.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait5.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Wait5.json", "net10.0")]
    public async Task MethodInterpretation_Task_Wait(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskJoinFinish))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
    
    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Result1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Result1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Result1.json", "net10.0")]
    public async Task MethodInterpretation_Task_Result(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskSchedule))
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyEventType(RecordedEventType.TaskComplete))
            .Then(EventuallyEventType(RecordedEventType.TaskJoinFinish))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease3.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease3.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_WaitRelease3.json", "net10.0")]
    public async Task MethodInterpretation_SemaphoreSlim_WaitRelease(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquire))
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.SemaphoreReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease2.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease3.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease3.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease3.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease4.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease4.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_SemaphoreSlim_TryWaitRelease4.json", "net10.0")]
    public async Task MethodInterpretation_SemaphoreSlim_TryWaitRelease(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquire))
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.SemaphoreReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Await1.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Await1.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Await1.json", "net10.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Await2.json", "net8.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Await2.json", "net9.0")]
    [InlineData($"{ConfigurationFolder}/MethodInterpretation_Task_Await2.json", "net10.0")]
    public async Task MethodInterpretation_Task_Await(string configuration, string sdk)
    {
        // Arrange
        var pluginAdditionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationDisabled();
        using var services = TestContextFactory.CreateServiceProvider(
            configuration, sdk, pluginAdditionalData, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestExecutionOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyEventType(RecordedEventType.TaskJoinFinish))
            .Then(EventuallyEventType(RecordedEventType.TaskJoinFinish))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
