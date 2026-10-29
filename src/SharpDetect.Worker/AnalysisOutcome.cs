// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker;

public enum AnalysisGapKind
{
    ProfilerNotAttached,
    ReceiverNotAttached,
    ProcessNotFinished,
    EventsDiscarded,
    NoTestsRan
}

public sealed record AnalysisGap(AnalysisGapKind Kind, string Description);

public sealed record AnalysisOutcome(IReadOnlyList<AnalysisGap> Gaps)
{
    public bool IsComplete => Gaps.Count == 0;
}
