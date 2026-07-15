// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Text.Json;
using System.Text.Json.Serialization;
using SharpDetect.Benchmarks.Models;

namespace SharpDetect.Benchmarks.Reporting;

internal sealed record BaselineReport(
    string Commit,
    string CommitDate,
    string Branch,
    bool DirtyWorkingTree,
    string CreatedAt,
    MachineInfo Machine,
    string Configuration,
    WorkloadInfo Workload,
    int Warmup,
    int Runs,
    IReadOnlyList<string> Warnings,
    BaselineMetrics Metrics)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public string Serialize()
        => JsonSerializer.Serialize(this, SerializerOptions);
}
