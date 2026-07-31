// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Communication;

public readonly record struct EventBatchReadResult(
    int Count,
    int FailedRecords,
    bool Corrupted,
    Exception? LastFailure);
