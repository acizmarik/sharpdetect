// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker;

public enum AnalysisGapKind
{
    ProfilerNotAttached,
    ReceiverNotAttached,
    ProcessNotFinished,
    EventsDiscarded,
    EventsDropped,
    NoTestsRan,
    StoppedByUser
}

public sealed record AnalysisGap(AnalysisGapKind Kind, string Description);

public sealed record AnalysisOutcome(IReadOnlyList<AnalysisGap> Gaps)
{
    public bool IsComplete => Gaps.Count == 0;

    public AnalysisOutcome WithGap(AnalysisGap gap) => this with { Gaps = [.. Gaps, gap] };

    public AnalysisOutcome StoppedByUser() => this with
    {
        Gaps =
        [
            .. Gaps.Where(gap => gap.Kind != AnalysisGapKind.ProcessNotFinished),
            new AnalysisGap(AnalysisGapKind.StoppedByUser, "Analysis was stopped by the user before the target exited.")
        ]
    };
}
