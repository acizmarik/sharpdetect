// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using System.Runtime.Versioning;
using SharpDetect.Benchmarks.Models;

namespace SharpDetect.Benchmarks.Measurements;

internal readonly struct ChildCpuAccounting
{
    public static string Mode => OperatingSystem.IsLinux() ? "exact-reaped-children" : "post-exit-process-times";
    // USER_HZ is a fixed kernel ABI constant for /proc timing fields
    private const double UserHz = 100.0;

    private readonly double _baselineSeconds;

    private ChildCpuAccounting(double baselineSeconds)
    {
        _baselineSeconds = baselineSeconds;
    }

    public static ChildCpuAccounting Begin()
        => new(OperatingSystem.IsLinux() ? ReadReapedChildrenCpuSeconds() : 0);

    public ProcessResourceSample Complete(ProcessResourceSample sampled)
        => OperatingSystem.IsLinux()
            ? sampled with { CpuSeconds = ReadReapedChildrenCpuSeconds() - _baselineSeconds }
            : sampled;

    [SupportedOSPlatform("linux")]
    private static double ReadReapedChildrenCpuSeconds()
        => ParseReapedChildrenTicks(File.ReadAllText("/proc/self/stat")) / UserHz;

    internal static long ParseReapedChildrenTicks(string statLine)
    {
        // Format: "pid (comm) state ppid ..."
        var afterComm = statLine[(statLine.LastIndexOf(')') + 1)..];
        var fields = afterComm.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return long.Parse(fields[13], CultureInfo.InvariantCulture)
            + long.Parse(fields[14], CultureInfo.InvariantCulture);
    }
}
