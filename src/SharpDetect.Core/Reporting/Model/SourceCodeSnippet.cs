// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Core.Reporting.Model;

public readonly record struct SourceCodeSnippet
{
    private readonly ImmutableArray<SourceCodeLine> _lines;

    public SourceCodeSnippet(ImmutableArray<SourceCodeLine> lines, bool isOutOfDate)
    {
        _lines = lines;
        IsOutOfDate = isOutOfDate;
    }

    public ImmutableArray<SourceCodeLine> Lines => _lines.IsDefault ? [] : _lines;
    public bool IsOutOfDate { get; }

    public static SourceCodeSnippet None => default;
    public static SourceCodeSnippet OutOfDate { get; } = new([], isOutOfDate: true);
}
