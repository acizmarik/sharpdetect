// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Benchmarks.Models;
using SharpDetect.Core.Events;

namespace SharpDetect.Benchmarks.Measurements;

internal static class RunValidation
{
    private const double WallToleranceSeconds = 0.05;
    
    private static readonly Type[] ExpectedEventTypes =
    [
        typeof(ProfilerInitializeRecordedEvent),
        typeof(ModuleLoadRecordedEvent),
        typeof(JitCompilationRecordedEvent),
        typeof(MethodEnterWithArgumentsRecordedEvent),
        typeof(MethodExitWithArgumentsRecordedEvent),
        typeof(MethodExitRecordedEvent),
        typeof(ThreadCreateRecordedEvent),
        typeof(FieldAccessInstrumentationRecordedEvent),
        typeof(MethodBodyRewriteRecordedEvent),
    ];

    public static IReadOnlyList<string> Validate(BareRunResult run)
    {
        var violations = new List<string>();
        if (run.WallSeconds <= 0)
            violations.Add($"bare wall time is {run.WallSeconds:F4} s");
        if (run.Resources.CpuSeconds <= 0)
            violations.Add($"bare target CPU is {run.Resources.CpuSeconds:F4} s");

        return violations;
    }

    public static IReadOnlyList<string> Validate(AnalyzedRunResult run)
    {
        var violations = new List<string>();
        if (run.Metrics.EventsReceived <= 0)
            violations.Add("no events were received from the profiler.");
        var unprocessedEvents = run.Metrics.EventsReceived - run.Metrics.EventsProcessed;
        if (unprocessedEvents is < 0 or > 1)
            violations.Add($"events processed ({run.Metrics.EventsProcessed}) does not account for events received ({run.Metrics.EventsReceived})");
        if (run.Metrics.TargetWallSeconds <= 0)
            violations.Add("target wall time was not measured (0)");
        if (run.Metrics.TargetWallSeconds > run.WallSeconds + WallToleranceSeconds)
            violations.Add($"target wall ({run.Metrics.TargetWallSeconds:F4} s) exceeds the whole analysis wall ({run.WallSeconds:F4} s)");
        if (run.Metrics.ProcessTailSeconds <= 0)
            violations.Add("process tail was not measured (0)");
        if (run.TargetResources.CpuSeconds <= 0)
            violations.Add($"instrumented target CPU is {run.TargetResources.CpuSeconds:F4} s");
        if (run.ReportedIssues != 0)
            violations.Add($"analysis reported {run.ReportedIssues} issue(s) on a race-free workload");

        foreach (var expectedType in ExpectedEventTypes)
        {
            if (!run.Metrics.ReceivedByType.ContainsKey(expectedType.Name))
                violations.Add($"expected event type {expectedType.Name} was never received");
        }

        return violations;
    }
}
