// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
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
    public async Task MethodInterpretation_Monitor_ContendedEnterExit_AcquireReleaseBalancedPerLock(string sdk)
    {
        await LockAcquireReleaseBalancedPerLock("Test_MonitorMethods_ContendedEnterExit", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_Wait3_Reentrancy_AcquireReleaseBalancedPerLock(string sdk)
    {
        await LockAcquireReleaseBalancedPerLock("Test_MonitorMethods_Wait3_Reentrancy", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_LockInSynchronizationContextWaitOfContendedLock_IsReported(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_LockMethods_LockInSynchronizationContextWaitOfContendedLock")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var pendingAttempts = new Dictionary<ProcessThreadId, Stack<ProcessTrackedObjectId>>();
        var isAcquiredWhileAcquiringAnotherLock = false;
        plugin.LockAcquireAttempted += args =>
        {
            if (!pendingAttempts.TryGetValue(args.ProcessThreadId, out var attempts))
                pendingAttempts[args.ProcessThreadId] = attempts = new Stack<ProcessTrackedObjectId>();
            attempts.Push(args.LockId);
        };
        plugin.LockAcquireReturned += args =>
        {
            if (!pendingAttempts.TryGetValue(args.ProcessThreadId, out var attempts) || !attempts.TryPop(out _))
                return;
            if (args.IsSuccess && attempts.TryPeek(out var outerLockId) && outerLockId != args.LockId)
                isAcquiredWhileAcquiringAnotherLock = true;
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(
            isAcquiredWhileAcquiringAnotherLock,
            "The lock taken by the SynchronizationContext wait override during the contended acquisition was not reported.");
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net10AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_SynchronizedMethod(string sdk)
    {
        await MonitorEnterExit("Test_MonitorMethods_SynchronizedMethod", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net10AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_SynchronizedMethodLoop_AcquireReleaseBalanced(string sdk)
    {
        await MonitorAcquireReleaseBalanced("Test_MonitorMethods_SynchronizedMethodLoop", sdk);
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
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_ReleaseReportsNoMethodExit(string sdk)
    {
        await LockReleaseReportsNoMethodExit("Test_MonitorMethods_EnterExitLoop", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net10Only), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Monitor_ExitIfLockTakenReportsNoMethodExit(string sdk)
    {
        await LockReleaseReportsNoMethodExit("Test_MonitorMethods_ExitIfLockTaken", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_ReleaseReportsNoMethodExit(string sdk)
    {
        await LockReleaseReportsNoMethodExit("Test_LockMethods_EnterExit1", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_ReaderWriterLockSlim_ReleaseReportsNoMethodExit(string sdk)
    {
        await LockReleaseReportsNoMethodExit("Test_NoDataRace_ReaderWriterLockSlim_WriteThenRead", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Thread_StartCallback_ReportsStartedThreadObject(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_ThreadMethods_StartCallback1")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var requestedThreads = new HashSet<ProcessTrackedObjectId>();
        var startedThreads = new List<ProcessTrackedObjectId>();
        plugin.ForkRequested += args =>
        {
            if (args.Kind == ForkKind.Thread)
                requestedThreads.Add(args.ChildId);
        };
        plugin.ForkStarted += args =>
        {
            if (args.Kind == ForkKind.Thread)
                startedThreads.Add(args.ChildId);
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.NotEmpty(startedThreads);
        Assert.All(startedThreads, threadObjectId => Assert.Contains(threadObjectId, requestedThreads));
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
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_TryEnterExit3(string sdk)
    {
        await LockEnterExit("Test_LockMethods_TryEnterExit3", sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net9AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lock_PolyfillOutsideCoreLibIsNotHooked(string sdk)
    {
        // Arrange
        const string polyfillModuleFileName = "SharpDetect.E2ETests.Subject.IL.dll";
        using var services = E2ETestBuilder
            .ForSubject("Test_LockMethods_PolyfillOutsideCoreLib")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var modulePaths = new Dictionary<ModuleId, string>();
        var lockEventModules = new HashSet<ModuleId>();
        plugin.ModuleLoaded += e => modulePaths[e.Args.ModuleId] = e.Args.Path;
        plugin.LockAcquireAttempted += e => lockEventModules.Add(e.ModuleId);
        plugin.LockAcquireReturned += e => lockEventModules.Add(e.ModuleId);
        plugin.LockReleased += e => lockEventModules.Add(e.ModuleId);

        // Execute
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Empty(outcome.Gaps);
        Assert.Contains(modulePaths.Values, path => Path.GetFileName(path) == polyfillModuleFileName);
        Assert.DoesNotContain(lockEventModules, moduleId => Path.GetFileName(modulePaths[moduleId]) == polyfillModuleFileName);
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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskSchedule))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_TaskCompletionSource_TrySetResult(string sdk)
        => AssertPromiseCompletionIsInterpreted("Test_TaskCompletionSource_TrySetResult", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_TaskCompletionSource_TrySetResultNonGeneric(string sdk)
        => AssertPromiseCompletionIsInterpreted("Test_TaskCompletionSource_TrySetResultNonGeneric", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_TaskCompletionSource_TrySetException(string sdk)
        => AssertPromiseCompletionIsInterpreted("Test_TaskCompletionSource_TrySetException", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_TaskCompletionSource_TrySetCanceled(string sdk)
        => AssertPromiseCompletionIsInterpreted("Test_TaskCompletionSource_TrySetCanceled", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_TaskContinueWith(string sdk)
        => AssertContinuationRegistrationIsInterpreted("Test_TaskMethods_ContinueWith1", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_TaskContinueWithResult(string sdk)
        => AssertContinuationRegistrationIsInterpreted("Test_TaskMethods_ContinueWith2", sdk);

    private async Task AssertContinuationRegistrationIsInterpreted(string subject, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subject)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskContinuationRegister))
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task AssertPromiseCompletionIsInterpreted(string subject, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subject)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskPromiseComplete))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_AsyncMethodBuilder_SuspendResume1(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_AsyncMethodBuilder_SuspendResume1")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.AsyncStateMachineSuspend))
            .Then(EventuallyEventType(RecordedEventType.AsyncStateMachineResume))
            .Then(EventuallyEventType(RecordedEventType.AsyncStateMachineComplete))
            .Then(EventuallyEventType(RecordedEventType.AsyncStateMachineSegmentComplete));
        var lifetimeAssert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.True(AssertStatus.Satisfied == lifetimeAssert.Evaluate(events), lifetimeAssert.GetDiagnosticInfo());
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net11AndAbove), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_RuntimeAsync_SuspendResume_ReportedOnReturnedTask(string sdk)
        => RuntimeAsyncSuspendResumeReportedOnReturnedTask("Test_RuntimeAsync_SuspendResume", sdk);

    [Theory]
    [MemberData(nameof(SdkVersions.Net11AndAbove), MemberType = typeof(SdkVersions))]
    public Task MethodInterpretation_RuntimeAsync_SuspendResumeWithAsyncDebugger_ReportedOnReturnedTask(string sdk)
        => RuntimeAsyncSuspendResumeReportedOnReturnedTask("Test_RuntimeAsync_SuspendResume_WithAsyncDebugger", sdk);

    private async Task RuntimeAsyncSuspendResumeReportedOnReturnedTask(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var suspensions = new List<ForkRequestArgs>();
        var resumptions = new List<ForkStartArgs>();
        var completedSegments = new List<ForkFinishArgs>();
        var joins = new List<ForkJoinArgs>();
        plugin.ForkRequested += args =>
        {
            if (args.Kind == ForkKind.AsyncResumption)
                suspensions.Add(args);
        };
        plugin.ForkStarted += args =>
        {
            if (args.Kind == ForkKind.AsyncResumption)
                resumptions.Add(args);
        };
        plugin.ForkFinished += args =>
        {
            if (args.Kind == ForkKind.AsyncResumption && args.FinishKind == ForkFinishKind.SegmentCompleted)
                completedSegments.Add(args);
        };
        plugin.ForkJoined += joins.Add;

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Contains(suspensions, suspension =>
            resumptions.Any(r => r.ChildId == suspension.ChildId && r.ProcessThreadId != suspension.ProcessThreadId) &&
            completedSegments.Any(s => s.ChildId == suspension.ChildId) &&
            joins.Any(j => j.ChildId == suspension.ChildId && j.ProcessThreadId == suspension.ProcessThreadId && j.IsSuccess));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net11AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_RuntimeAsync_CallerContinuingOnCapturedContext_ReleasedBeforeHandOver(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_RuntimeAsync_CallerContinuesOnCapturedContext")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var resumptionEvents = new List<(bool IsResume, ProcessThreadId Thread, ProcessTrackedObjectId Task)>();
        plugin.ForkRequested += args =>
        {
            if (args.Kind == ForkKind.AsyncResumption)
                resumptionEvents.Add((false, args.ProcessThreadId, args.ChildId));
        };
        plugin.ForkStarted += args =>
        {
            if (args.Kind == ForkKind.AsyncResumption)
                resumptionEvents.Add((true, args.ProcessThreadId, args.ChildId));
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var isReleasedBeforeHandOver = resumptionEvents
            .Select((evt, index) => (evt, index))
            .Where(e => e.evt.IsResume)
            .Any(first => resumptionEvents
                .Skip(first.index + 1)
                .TakeWhile(e => !(e.IsResume && e.Task == first.evt.Task))
                .Any(e => !e.IsResume && e.Task == first.evt.Task && e.Thread == first.evt.Thread) &&
                resumptionEvents
                    .Skip(first.index + 1)
                    .Any(e => e.IsResume && e.Task == first.evt.Task && e.Thread != first.evt.Thread));
        Assert.True(isReleasedBeforeHandOver, "The hand-over of the resumed frames to the captured context was not released.");
    }

    [Theory]
    [MemberData(nameof(SdkVersions.Net11AndAbove), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_RuntimeAsync_AwaitedTaskReturningMethods_OnlyOrdinaryCallsReported(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_RuntimeAsync_HookedTaskReturningMethods")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var enters = new Dictionary<string, int>();
        var exits = new Dictionary<string, int>();
        var returnValueSizes = new HashSet<int>();
        string? GetHookedMethodName(RecordedEventMetadata metadata, Core.Events.Profiler.ModuleId moduleId, Core.Events.Profiler.MdMethodDef methodToken)
        {
            var method = plugin.Resolve(metadata, moduleId, methodToken);
            return !method.IsError && method.Value.Name.StartsWith("RuntimeAsyncHooked_", StringComparison.Ordinal)
                ? method.Value.Name.String
                : null;
        }
        plugin.MethodEntered += args =>
        {
            if (GetHookedMethodName(args.Metadata, args.Args.ModuleId, args.Args.MethodToken) is { } name)
                enters[name] = enters.GetValueOrDefault(name) + 1;
        };
        plugin.MethodExitedWithArguments += args =>
        {
            if (GetHookedMethodName(args.Metadata, args.Args.ModuleId, args.Args.MethodToken) is not { } name)
                return;
            exits[name] = exits.GetValueOrDefault(name) + 1;
            returnValueSizes.Add(args.Args.ReturnValue.Length);
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var expected = new Dictionary<string, int>
        {
            ["RuntimeAsyncHooked_TaskReturning"] = 1,
            ["RuntimeAsyncHooked_RuntimeAsync"] = 1,
            ["RuntimeAsyncHooked_GenericTaskReturning"] = 2
        };
        Assert.Equal(expected, enters);
        Assert.Equal(expected, exits);
        Assert.Equal([nint.Size], returnValueSizes);
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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyEventType(RecordedEventType.TaskComplete))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskSchedule))
            .Then(EventuallyEventType(RecordedEventType.TaskStart))
            .Then(EventuallyEventType(RecordedEventType.TaskComplete))
            .Then(EventuallyEventType(RecordedEventType.TaskJoinFinish))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Await1(string sdk)
    {
        await TaskAwait("Test_TaskMethods_Await1", expectedTasks: 1, sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Task_Await2(string sdk)
    {
        await TaskAwait("Test_TaskMethods_Await2", expectedTasks: 2, sdk);
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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleSet))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleReset))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleSet))
            .Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.ValuePublicationMaybeStoreLoad))
            .Then(EventuallyEventType(RecordedEventType.ValuePublicationLoad))
            .Then(EventuallyEventType(RecordedEventType.ValuePublicationLoad))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act && Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        var stores = ValuePublications(events, RecordedEventType.ValuePublicationMaybeStoreLoad, args.Target.Args.Single(), plugin);
        Assert.Single(stores);
        var lazyContainer = stores[0].Container;
        var loads = ValuePublications(events, RecordedEventType.ValuePublicationLoad, args.Target.Args.Single(), plugin)
            .Where(load => load.Container == lazyContainer)
            .ToList();
        Assert.True(loads.Count >= 2);
        var publications = stores.Concat(loads).ToList();
        var publishedValues = publications.Select(publication => publication.Value).Distinct().ToList();
        Assert.True(publishedValues.Count == 1);
        AssertPublishedThroughASingleContainer(publications);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_Lazy_ValueConstructor(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_LazyMethods_ValueConstructor")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.ValuePublicationStore))
            .Then(EventuallyEventType(RecordedEventType.ValuePublicationLoad))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act & Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        var stores = ValuePublications(events, RecordedEventType.ValuePublicationStore, args.Target.Args.Single(), plugin);
        Assert.Single(stores);
        var loads = ValuePublications(events, RecordedEventType.ValuePublicationLoad, args.Target.Args.Single(), plugin)
            .Where(load => load.Container == stores[0].Container)
            .ToList();
        Assert.NotEmpty(loads);
        Assert.Empty(ValuePublications(events, RecordedEventType.ValuePublicationMaybeStoreLoad, args.Target.Args.Single(), plugin));
        Assert.All(loads, load => Assert.Equal(stores[0].Value, load.Value));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_TypeInitializer_Completes(string sdk)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_TypeInitializer_Completes")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput, additionalData);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.TypeInitializerEnter, "StaticReadonlyHierarchyInit", plugin))
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.TypeInitializerExit, "StaticReadonlyHierarchyInit", plugin))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act & Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.Single(TypeInitializerEventTypeNames(events, RecordedEventType.TypeInitializerExit, plugin), name => name == "StaticReadonlyHierarchyInit");
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_TypeInitializer_Throws_RaisesNoCompletion(string sdk)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_TypeInitializer_Throws")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput, additionalData);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.TypeInitializerEnter, "ThrowingTypeInitializer", plugin))
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.MethodUnwound, "ThrowingTypeInitializer", plugin))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act & Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.DoesNotContain("ThrowingTypeInitializer", TypeInitializerEventTypeNames(events, RecordedEventType.TypeInitializerExit, plugin));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_TypeInitializer_CatchesRethrownException_Completes(string sdk)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_TypeInitializer_CatchesRethrownException")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput, additionalData);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.TypeInitializerEnter, "TypeInitializerCatchingRethrownException", plugin))
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.TypeInitializerExit, "TypeInitializerCatchingRethrownException", plugin))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act & Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.DoesNotContain("TypeInitializerCatchingRethrownException", TypeInitializerEventTypeNames(events, RecordedEventType.MethodUnwound, plugin));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_TypeInitializer_CatchesExceptionEscapingFinally_Completes(string sdk)
    {
        // Arrange
        var additionalData = TestPluginAdditionalData.CreateWithFieldsAccessInstrumentationEnabled();
        using var services = E2ETestBuilder
            .ForSubject("Test_TypeInitializer_CatchesExceptionEscapingFinally")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput, additionalData);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.TypeInitializerEnter, "TypeInitializerCatchingExceptionEscapingFinally", plugin))
            .Then(EventuallyTypeInitializerEvent(RecordedEventType.TypeInitializerExit, "TypeInitializerCatchingExceptionEscapingFinally", plugin))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act & Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.DoesNotContain("TypeInitializerCatchingExceptionEscapingFinally", TypeInitializerEventTypeNames(events, RecordedEventType.MethodUnwound, plugin));
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_TypeInitializer_NotHookedWithoutFieldInstrumentation(string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject("Test_TypeInitializer_Completes")
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var args = services.GetRequiredService<RunCommandArgs>();
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var events = new TestEventsEnumerable(plugin);
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act & Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        Assert.DoesNotContain(events, e => e.Type is RecordedEventType.TypeInitializerEnter or RecordedEventType.TypeInitializerExit);
    }

    private static List<string> TypeInitializerEventTypeNames(
        TestEventsEnumerable events,
        RecordedEventType type,
        TestPerThreadOrderingPlugin plugin)
    {
        return events
            .Where(e => e.Type == type)
            .Select(e => e.Get<(RecordedEventMetadata Metadata, TypeInitializerArgs Args)>())
            .Select(e => plugin.Resolve(e.Metadata, e.Args.ModuleId, e.Args.TypeInitializerToken))
            .Where(result => !result.IsError)
            .Select(result => result.Value.DeclaringType.Name.String)
            .ToList();
    }

    private static List<ValuePublicationArgs> ValuePublications(
        TestEventsEnumerable events,
        RecordedEventType type,
        string subjectMethod,
        IMetadataResolver plugin)
    {
        var ordered = events.OrderBy(e => e.Id).ToList();
        var enter = ordered.FindIndex(e => Matches(EventMatchers.MethodEnter(subjectMethod, plugin), e));
        var exit = ordered.FindLastIndex(e => Matches(EventMatchers.MethodExit(subjectMethod, plugin), e));
        if (enter < 0 || exit < enter)
            return [];

        var subjectThread = ordered[enter].Get<(RecordedEventMetadata Metadata, MethodEnterRecordedEvent Args)>().Metadata.Tid;
        return ordered[enter..exit]
            .Where(e => e.Type == type)
            .Select(e => e.Get<(RecordedEventMetadata Metadata, ValuePublicationArgs Args)>())
            .Where(e => e.Metadata.Tid == subjectThread)
            .Select(e => e.Args)
            .ToList();
    }

    private static bool Matches(AtomicPredicate<ulong, RecordedEventType> predicate, IEvent<ulong, RecordedEventType> evt)
    {
        predicate.ProcessEvent(evt);
        return predicate.Status == AssertStatus.Satisfied;
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_ConcurrentDictionary_Store(string sdk)
    {
        // TryAdd, set_Item and TryUpdate each publish through a method-enter store hook
        await AssertPublishesDistinctValues(
            "Test_ConcurrentDictionaryMethods_Store",
            RecordedEventType.ValuePublicationStore,
            expectedCount: 3,
            sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_ConcurrentDictionary_LoadByRef(string sdk)
    {
        // TryGetValue and TryRemove both observe the same value through an out parameter
        await AssertPublishesDistinctValues(
            "Test_ConcurrentDictionaryMethods_LoadByRef",
            RecordedEventType.ValuePublicationLoad,
            expectedCount: 1,
            sdk);
    }

    [Theory]
    [MemberData(nameof(SdkVersions.All), MemberType = typeof(SdkVersions))]
    public async Task MethodInterpretation_ConcurrentDictionary_AddOrUpdate(string sdk)
    {
        // Both AddOrUpdate overloads return the resulting value, so they publish and observe it
        await AssertPublishesDistinctValues(
            "Test_ConcurrentDictionaryMethods_AddOrUpdate",
            RecordedEventType.ValuePublicationStoreLoad,
            expectedCount: 1,
            sdk);
    }

    private async Task AssertPublishesDistinctValues(
        string subjectArgs,
        RecordedEventType eventType,
        int expectedCount,
        string sdk)
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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(eventType))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Act && Assert
        await analysisWorker.ExecuteAsync(CancellationToken.None);
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
        var publications = ValuePublications(events, eventType, args.Target.Args.Single(), plugin);
        var publishedValues = publications.Select(publication => publication.Value).Distinct().ToList();
        Assert.True(
            publishedValues.Count >= expectedCount,
            $"Expected at least {expectedCount} distinct {eventType} value(s), observed {publishedValues.Count}.");
        AssertPublishedThroughASingleContainer(publications);
    }

    private static void AssertPublishedThroughASingleContainer(IReadOnlyList<ValuePublicationArgs> publications)
    {
        var containers = publications.Select(args => args.Container).Distinct().ToList();
        Assert.All(containers, container => Assert.NotEqual(0ul, (ulong)container.ObjectId.Value));
        Assert.Single(containers);
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

    private async Task LockReleaseReportsNoMethodExit(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var releases = 0;
        var releaseMethods = new HashSet<(ModuleId, MdMethodDef)>();
        var exits = new List<(ModuleId, MdMethodDef)>();
        plugin.LockReleased += e =>
        {
            releases++;
            releaseMethods.Add((e.ModuleId, e.MethodToken));
        };
        plugin.MethodExited += e => exits.Add((e.Args.ModuleId, e.Args.MethodToken));
        plugin.MethodExitedWithArguments += e => exits.Add((e.Args.ModuleId, e.Args.MethodToken));

        // Execute
        var outcome = await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.Empty(outcome.Gaps);
        Assert.NotEqual(0, releases);
        Assert.DoesNotContain(exits, exit => releaseMethods.Contains(exit));
    }

    private async Task LockAcquireReleaseBalancedPerLock(string subjectArgs, string sdk)
    {
        // Arrange
        using var services = E2ETestBuilder
            .ForSubject(subjectArgs)
            .WithPlugin<TestPerThreadOrderingPlugin>()
            .Build(sdk, testOutput);
        var plugin = services.GetRequiredService<TestPerThreadOrderingPlugin>();
        var analysisWorker = services.GetRequiredService<IAnalysisWorker>();
        var heldCounts = new Dictionary<ProcessTrackedObjectId, int>();
        var acquiredLocks = new HashSet<ProcessTrackedObjectId>();
        plugin.LockAcquireReturned += args =>
        {
            if (!args.IsSuccess || args.IsShared)
                return;

            acquiredLocks.Add(args.LockId);
            heldCounts[args.LockId] = heldCounts.GetValueOrDefault(args.LockId) + 1;
        };
        plugin.LockReleased += args =>
        {
            if (!args.IsShared)
                heldCounts[args.LockId] = heldCounts.GetValueOrDefault(args.LockId) - 1;
        };

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        var acquiredLockCounts = heldCounts.Where(pair => acquiredLocks.Contains(pair.Key)).ToArray();
        Assert.NotEmpty(acquiredLockCounts);
        Assert.All(acquiredLockCounts, pair => Assert.True(
            pair.Value == 0,
            $"Lock {pair.Key.ObjectId.Value} has {pair.Value} unmatched acquisition(s) (negative = releases without acquisition)."));
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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.MonitorWaitAttempt))
            .Then(EventuallyEventType(RecordedEventType.MonitorWaitResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.ThreadJoinAttempt))
            .Then(EventuallyEventType(RecordedEventType.ThreadJoinResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.ThreadStartCore))
            .Then(EventuallyEventType(RecordedEventType.ThreadCreate))
            .Then(EventuallyEventType(RecordedEventType.ThreadStartCallback))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.TaskJoinFinish))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }

    private async Task TaskAwait(string subjectArgs, int expectedTasks, string sdk)
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
        AssertBase<ulong, RecordedEventType> assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin);
        for (var i = 0; i < expectedTasks; i++)
            assert = assert.Then(EventuallyEventType(RecordedEventType.TaskStart));
        for (var i = 0; i < expectedTasks; i++)
            assert = assert.Then(EventuallyEventType(RecordedEventType.TaskJoinFinish));
        assert = assert.Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquire))
            .Then(EventuallyEventType(RecordedEventType.SemaphoreAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.SemaphoreReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.LockAcquire))
            .Then(EventuallyEventType(RecordedEventType.LockAcquireResult))
            .Then(EventuallyEventType(RecordedEventType.LockReleaseResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleCreate))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleSet))
            .Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleCreate))
            .Then(EventuallyEventType(RecordedEventType.EventWaitHandleCreate));
        for (var i = 0; i < expectedWaitResults; i++)
            assert = assert.Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult));
        assert = assert.Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

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
        var assert = EventuallyMethodEnter(args.Target.Args.Single(), plugin)
            .Then(EventuallyEventType(signalHalfEventType))
            .Then(EventuallyEventType(RecordedEventType.WaitHandleWaitResult))
            .Then(EventuallyMethodExit(args.Target.Args.Single(), plugin));

        // Execute
        await analysisWorker.ExecuteAsync(CancellationToken.None);

        // Assert
        Assert.True(AssertStatus.Satisfied == assert.Evaluate(events), assert.GetDiagnosticInfo());
    }
}
