// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting.Model;

public record TimingInfo(
    DateTimeOffset AnalysisStartTime,
    DateTimeOffset AnalysisEndTime,
    TimeSpan AnalysisDuration);

