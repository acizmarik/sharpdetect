// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace PerfWorkload;

internal static class WorkloadOptions
{
    public static int Parse(string[] args, string name, int defaultValue)
    {
        var tokens = args.SelectMany(static a => a.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToArray();
        for (var index = 0; index < tokens.Length; index++)
        {
            if (tokens[index] != name)
                continue;
            
            if (index == tokens.Length - 1 || !int.TryParse(tokens[index + 1], out var value) || value <= 0)
            {
                var actual = index == tokens.Length - 1 ? "<missing>" : tokens[index + 1];
                throw new ArgumentException($"Option {name} requires a positive integer value, but got: {actual}.");
            }

            return value;
        }

        return defaultValue;
    }
}
