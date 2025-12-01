// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting.Model;

public record EnvironmentInfo(
    string OperatingSystem,
    string ProcessorArchitecture,
    int ProcessorCount,
    long TotalPhysicalMemoryBytes,
    string MachineName,
    string UserName,
    string? WorkingDirectory);
