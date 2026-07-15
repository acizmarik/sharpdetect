// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace SharpDetect.Benchmarks.Models;

internal static class BuildInfo
{
    public static string Configuration { get; } = typeof(BuildInfo).Assembly
        .GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "unknown";
}
