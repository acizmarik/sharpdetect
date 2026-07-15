// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Benchmarks.Models;

internal sealed record ValueSpread(double Median, double Min, double Max)
{
    public static ValueSpread From(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        if (sorted.Length == 0)
            return new ValueSpread(0, 0, 0);

        return new ValueSpread(
            Round(MedianOfSorted(sorted)),
            Round(sorted[0]),
            Round(sorted[^1]));
    }

    public static double MedianOf(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted.Length == 0 ? 0 : MedianOfSorted(sorted);
    }

    public static long MedianOf(IEnumerable<long> values)
        => (long)Math.Round(MedianOf(values.Select(static v => (double)v)));

    private static double MedianOfSorted(IReadOnlyList<double> sorted)
    {
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1
            ? sorted[middle]
            : (sorted[middle - 1] + sorted[middle]) / 2.0;
    }

    private static double Round(double value)
        => Math.Round(value, 4);
}
