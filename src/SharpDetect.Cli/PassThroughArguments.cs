// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Cli;

public sealed class PassThroughArguments
{
    private const string Separator = "--";

    public static readonly PassThroughArguments None = new([]);

    public ImmutableArray<string> Arguments { get; }
    public bool IsSpecified { get; }

    private PassThroughArguments(ImmutableArray<string> arguments, bool isSpecified = false)
    {
        Arguments = arguments;
        IsSpecified = isSpecified;
    }
    
    public static (IReadOnlyList<string> OwnArguments, PassThroughArguments PassThrough) Split(IReadOnlyList<string> commandLine)
    {
        var separatorIndex = commandLine.ToList().IndexOf(Separator);
        if (separatorIndex < 0)
            return (commandLine, None);

        var ownArguments = commandLine.Take(separatorIndex).ToArray();
        var passThrough = commandLine.Skip(separatorIndex + 1).ToImmutableArray();
        return (ownArguments, new PassThroughArguments(passThrough, isSpecified: true));
    }
}
