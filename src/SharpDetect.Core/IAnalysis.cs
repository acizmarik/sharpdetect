// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core
{
    public interface IAnalysis
    {
        Task<bool> ExecuteOnlyAnalysisAsync(CancellationToken ct);
        Task<bool> ExecuteAnalysisAndTargetAsync(CancellationToken ct);
    }
}