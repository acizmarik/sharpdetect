// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker;

public interface IAnalysisWorker
{
    ValueTask<AnalysisOutcome> ExecuteAsync(CancellationToken cancellationToken);
}