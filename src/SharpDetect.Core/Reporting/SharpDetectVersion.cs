// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;

namespace SharpDetect.Core.Reporting;

public static class SharpDetectVersion
{
    private const int CommitHashLength = 12;

    public static string Current { get; } = Format(
        typeof(SharpDetectVersion).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ??
        typeof(SharpDetectVersion).Assembly.GetName().Version?.ToString() ??
        "unknown version");
    
    public static string Format(string informationalVersion)
    {
        var metadataStart = informationalVersion.IndexOf('+');
        if (metadataStart < 0 || informationalVersion.Length - metadataStart - 1 <= CommitHashLength)
            return informationalVersion;

        return informationalVersion[..(metadataStart + 1 + CommitHashLength)];
    }
}
