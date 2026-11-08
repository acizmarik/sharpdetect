// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Reporting.Formatters;

public static class SourceLocationFormatter
{
    private const string OutOfDateMarker = "(modified since build)";

    public static string FormatShort(SourceLocation source)
    {
        return Format(Path.GetFileName(source.DocumentPath), source);
    }

    public static string FormatFull(SourceLocation source)
    {
        return Format(source.DocumentPath, source);
    }

    private static string Format(string document, SourceLocation source)
    {
        var location = $"{document}:{source.Line}";
        return source.Snippet.IsOutOfDate ? $"{location} {OutOfDateMarker}" : location;
    }
}
