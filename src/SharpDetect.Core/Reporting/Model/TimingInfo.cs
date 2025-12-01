// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting.Model;

public record TimingInfo(
    DateTime AnalysisStartTime,
    DateTime AnalysisEndTime,
    TimeSpan AnalysisDuration);

