// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Benchmarks.Models;

internal readonly record struct ProcessResourceSample(double CpuSeconds, double PeakRssMegabytes);
