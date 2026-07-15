// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Benchmarks.Models;

internal sealed record MachineInfo(string Os, int Cores, string Rid, string CpuAccounting);
