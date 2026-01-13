// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Configuration;

public static class PathUtils
{
    public static string? NormalizeDirectorySeparators(string? path)
    {
        return path?.Replace('\\', '/');
    }
}

