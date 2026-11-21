// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Deadlock;

internal static class Cycles
{
    public static bool AreEqualUpToRotation<T>(IReadOnlyList<T> first, IReadOnlyList<T> second)
    {
        if (first.Count != second.Count)
            return false;
        if (first.Count == 0)
            return true;

        return Enumerable
            .Range(0, second.Count)
            .Any(offset =>
                second.Skip(offset).Concat(second.Take(offset)).SequenceEqual(first));
    }
}
