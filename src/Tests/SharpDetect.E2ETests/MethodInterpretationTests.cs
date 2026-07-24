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
public class MethodInterpretationTests(ITestOutputHelper testOutput)
{
    public const string CollectionName = "E2E_MethodInterpretationTests";
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
    public async Task MethodInterpretation_Monitor_EnterExitLoop_AcquireReleaseBalanced(string sdk)
    {
        await MonitorAcquireReleaseBalanced("Test_MonitorMethods_EnterExitLoop", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_TryEnterExitLoop_AcquireReleaseBalanced(string sdk)
    {
        await MonitorAcquireReleaseBalanced("Test_MonitorMethods_TryEnterExitLoop", sdk);
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Mutex_WaitOneRelease1(string sdk)
    {
        await MutexWaitOneRelease("Test_MutexMethods_WaitOneRelease1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Mutex_WaitOneRelease2(string sdk)
    {
        await MutexWaitOneRelease("Test_MutexMethods_WaitOneRelease2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Semaphore_WaitOneRelease1(string sdk)
    {
        // A kernel semaphore raises the same semantic events as SemaphoreSlim
        await SemaphoreSlimWaitRelease("Test_SemaphoreMethods_WaitOneRelease1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Semaphore_WaitOneRelease2(string sdk)
    {
        // A kernel semaphore raises the same semantic events as SemaphoreSlim
        await SemaphoreSlimWaitRelease("Test_SemaphoreMethods_WaitOneRelease2", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_EventWaitHandle_AutoReset_SetWaitOne(string sdk)
    {
        await EventWaitHandleSetWaitOne("Test_EventWaitHandleMethods_AutoReset_SetWaitOne", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_EventWaitHandle_ManualReset_SetWaitOne(string sdk)
    {
        await EventWaitHandleSetWaitOne("Test_EventWaitHandleMethods_ManualReset_SetWaitOne", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SignalAndWait_Events(string sdk)
    {
        await SignalAndWait(
            "Test_SignalAndWaitMethods_Events",
            RecordedEventType.EventWaitHandleSet,
            sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_SignalAndWait_MutexSignal(string sdk)
    {
        await SignalAndWait(
            "Test_SignalAndWaitMethods_MutexSignal",
            RecordedEventType.LockReleaseResult,
            sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_AbandonedMutexException_Construct(string sdk)
    {
        await MutexWaitOneRelease("Test_AbandonedMutexExceptionMethods_Construct", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_WaitMultiple_WaitAll(string sdk)
    {
        await WaitMultiple("Test_WaitMultipleMethods_WaitAll", expectedWaitResults: 2, sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_WaitMultiple_WaitAny(string sdk)
    {
        await WaitMultiple("Test_WaitMultipleMethods_WaitAny", expectedWaitResults: 1, sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_EventWaitHandle_ManualReset_SetResetSet(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_EventWaitHandleMethods_ManualReset_SetResetSet")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleSet))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleReset))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleSet))
            .Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lazy_GetValue(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_LazyMethods_GetValue")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args!, plugin)
            .Then(EventuallyEventType(RecordedEventType.ValuePublicationStoreLoad))
            .Then(EventuallyEventType(RecordedEventType.ValuePublicationStoreLoad))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Act && Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        var publishedValues = events
            .Where(e => e.Type == RecordedEventType.ValuePublicationStoreLoad)
            .Select(e => e.Get<(RecordedEventMetadata Metadata, ValuePublicationArgs Args)>().Args.Value)
            .ToList();
        Assert.True(
            publishedValues.GroupBy(value => value).Any(group => group.Count() >= 2),
            $"Expected a value published by both accesses, observed {publishedValues.Count} publication(s).");
    }

    private async Task MonitorAcquireReleaseBalanced(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        var acquireResults = events.Count(e => e.Type == RecordedEventType.LockAcquireResult);
        var releaseResults = events.Count(e => e.Type == RecordedEventType.LockReleaseResult);

        Assert.True(
            acquireResults >= Subject.Program.MonitorBalanceLoopIterations,
            $"Expected at least {Subject.Program.MonitorBalanceLoopIterations} acquire results, observed {acquireResults}.");
        Assert.Equal(releaseResults, acquireResults);
    }

    private async Task MonitorEnterExit(string subjectArgs, string sdk)
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
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

    private async Task MutexWaitOneRelease(string subjectArgs, string sdk)
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
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task EventWaitHandleSetWaitOne(string subjectArgs, string sdk)
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
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleCreate))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleSet))
            .Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task WaitMultiple(string subjectArgs, int expectedWaitResults, string sdk)
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
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleCreate))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleCreate));
        for (var i = 0; i < expectedWaitResults; i++)
            assert = assert.Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult));
        assert = assert.Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task SignalAndWait(string subjectArgs, RecordedEventType signalHalfEventType, string sdk)
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
            .Then(EventuallyEventType(signalHalfEventType))
            .Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult))
            .Then(EventuallyMethodExit(args.Target.Args!, plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
