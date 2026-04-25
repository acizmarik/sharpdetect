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
    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_EnterExit1(string sdk)
    {
        await MonitorEnterExit("Test_MonitorMethods_EnterExit1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_EnterExit2(string sdk)
    {
        await MonitorEnterExit("Test_MonitorMethods_EnterExit2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_TryEnterExit1(string sdk)
    {
        await MonitorEnterExit("Test_MonitorMethods_TryEnterExit1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_TryEnterExit2(string sdk)
    {
        await MonitorEnterExit("Test_MonitorMethods_TryEnterExit2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_TryEnterExit3(string sdk)
    {
        await MonitorEnterExit("Test_MonitorMethods_TryEnterExit3", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net10Only), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_ExitIfLockTaken(string sdk)
    {
        await MonitorEnterExit("Test_MonitorMethods_ExitIfLockTaken", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_Wait1(string sdk)
    {
        await MonitorWait("Test_MonitorMethods_Wait1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_Wait2(string sdk)
    {
        await MonitorWait("Test_MonitorMethods_Wait2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_Wait3_Reentrancy(string sdk)
    {
        await MonitorWait("Test_MonitorMethods_Wait3_Reentrancy", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Thread_Join1(string sdk)
    {
        await ThreadJoin("Test_ThreadMethods_Join1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Thread_Join2(string sdk)
    {
        await ThreadJoin("Test_ThreadMethods_Join2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Thread_Join3(string sdk)
    {
        await ThreadJoin("Test_ThreadMethods_Join3", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Thread_StartCallback1(string sdk)
    {
        await ThreadStartCallback("Test_ThreadMethods_StartCallback1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Thread_StartCallback2(string sdk)
    {
        await ThreadStartCallback("Test_ThreadMethods_StartCallback2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_EnterExit1(string sdk)
    {
        await LockEnterExit("Test_LockMethods_EnterExit1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_EnterExit2(string sdk)
    {
        await LockEnterExit("Test_LockMethods_EnterExit2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_TryEnterExit1(string sdk)
    {
        await LockEnterExit("Test_LockMethods_TryEnterExit1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_TryEnterExit2(string sdk)
    {
        await LockEnterExit("Test_LockMethods_TryEnterExit2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_ScheduleAndStart1(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_TaskMethods_ScheduleAndStart1")
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_InnerInvoke1(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_TaskMethods_InnerInvoke1")
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Wait1(string sdk)
    {
        await TaskWait("Test_TaskMethods_Wait1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Wait2(string sdk)
    {
        await TaskWait("Test_TaskMethods_Wait2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Wait3(string sdk)
    {
        await TaskWait("Test_TaskMethods_Wait3", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Wait4(string sdk)
    {
        await TaskWait("Test_TaskMethods_Wait4", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Wait5(string sdk)
    {
        await TaskWait("Test_TaskMethods_Wait5", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Result1(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_TaskMethods_Result1")
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Await1(string sdk)
    {
        await TaskAwait("Test_TaskMethods_Await1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Await2(string sdk)
    {
        await TaskAwait("Test_TaskMethods_Await2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SemaphoreSlim_WaitRelease1(string sdk)
    {
        await SemaphoreSlimWaitRelease("Test_SemaphoreSlimMethods_WaitRelease1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SemaphoreSlim_WaitRelease2(string sdk)
    {
        await SemaphoreSlimWaitRelease("Test_SemaphoreSlimMethods_WaitRelease2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SemaphoreSlim_WaitRelease3(string sdk)
    {
        await SemaphoreSlimWaitRelease("Test_SemaphoreSlimMethods_WaitRelease3", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SemaphoreSlim_TryWaitRelease1(string sdk)
    {
        await SemaphoreSlimWaitRelease("Test_SemaphoreSlimMethods_TryWaitRelease1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SemaphoreSlim_TryWaitRelease2(string sdk)
    {
        await SemaphoreSlimWaitRelease("Test_SemaphoreSlimMethods_TryWaitRelease2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SemaphoreSlim_TryWaitRelease3(string sdk)
    {
        await SemaphoreSlimWaitRelease("Test_SemaphoreSlimMethods_TryWaitRelease3", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SemaphoreSlim_TryWaitRelease4(string sdk)
    {
        await SemaphoreSlimWaitRelease("Test_SemaphoreSlimMethods_TryWaitRelease4", sdk);
    }

    private async Task MonitorEnterExit(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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

    private async Task LockEnterExit(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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

    private async Task MonitorWait(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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

    private async Task ThreadJoin(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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

    private async Task ThreadStartCallback(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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

    private async Task TaskWait(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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

    private async Task TaskAwait(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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

    private async Task SemaphoreSlimWaitRelease(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestExecutionOrderingPlugin>()
            .Build(sdk, testOutput);
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
}
