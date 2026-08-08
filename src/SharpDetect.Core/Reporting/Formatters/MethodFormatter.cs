// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting.Formatters;

public static class MethodFormatter
{
    public static string ToDisplayName(string metadataName)
    {
        var name = metadataName;
        var doubleColonIndex = name.IndexOf("::", StringComparison.Ordinal);
        var spaceIndex = name.IndexOf(' ');
        if (spaceIndex >= 0 && spaceIndex < doubleColonIndex)
            name = name[(spaceIndex + 1)..];

        return name.Replace("::", ".").Replace('/', '.');
    }
}
