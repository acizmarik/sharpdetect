// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Worker;

public interface IAnalysisWorker
{
    ValueTask ExecuteAsync(CancellationToken cancellationToken);
}