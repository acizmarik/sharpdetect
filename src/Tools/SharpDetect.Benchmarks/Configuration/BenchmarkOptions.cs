// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Benchmarks.Configuration;

internal sealed record BenchmarkOptions(
    string WorkloadPath,
    int Iterations,
    int Threads,
    int Warmup,
    int Runs,
    string? OutputDirectory);
