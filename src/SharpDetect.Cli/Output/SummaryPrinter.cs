// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Formatters;
using SharpDetect.Core.Reporting.Model;
using SharpDetect.Plugins.DataRace.Common;
using SharpDetect.Worker;

namespace SharpDetect.Cli.Output;

internal static class SummaryPrinter
{
    public const int DefaultMaxTargets = 5;
    public const int DefaultMaxReportsPerTarget = 3;

    public static void Print(
        TextWriter writer,
        string pluginName,
        Summary summary,
        AnalysisOutcome outcome,
        string? reportPath,
        Verbosity verbosity,
        int maxTargets = DefaultMaxTargets,
        int maxReportsPerTarget = DefaultMaxReportsPerTarget)
    {
        if (verbosity != Verbosity.Quiet)
            PrintFindings(writer, pluginName, summary, maxTargets, maxReportsPerTarget);

        if (!outcome.IsComplete)
            writer.WriteLine($"Analysis incomplete: {string.Join(" ", outcome.Gaps.Select(gap => gap.Description))}");

        if (reportPath is not null)
            writer.WriteLine($"Report: {reportPath}");
    }

    private static void PrintFindings(
        TextWriter writer,
        string pluginName,
        Summary summary,
        int maxTargets,
        int maxReportsPerTarget)
    {
        writer.WriteLine();
        writer.WriteLine($"{pluginName}: {summary.Title}");

        // Reports without a target are listed one by one
        var targets = summary.GetAllReports()
            .GroupBy(report => (report.ProcessId, report.Target ?? $"#{report.Identifier}"))
            .Select(group => group.ToArray())
            .ToArray();

        foreach (var (index, reports) in targets.Take(maxTargets).Index())
        {
            var first = reports[0];
            var heading = first.Target is null ? first.Title : $"{first.Target}  ({reports.Length} {GetReportNoun(first.Category, reports.Length)})";
            writer.WriteLine($"  {index + 1}. {heading}");
            foreach (var report in reports.Take(maxReportsPerTarget))
                PrintReport(writer, report);
            PrintHidden(writer, "     ", reports.Length - maxReportsPerTarget);
        }

        PrintHidden(writer, "  ", targets.Skip(maxTargets).Sum(reports => reports.Length));
    }

    private static void PrintReport(TextWriter writer, Report report)
    {
        var threads = report.GetReportedThreads().OrderBy(thread => thread.AccessIndex).ToArray();

        // A data race title names the access of each thread: "<earlier> ↔ <later>"
        var accesses = report.Title.Split(" ↔ ");
        var hasAccessPerThread = report.Target is not null && accesses.Length == threads.Length;

        var rows = threads
            .Select((thread, index) => (
                Thread: thread.Name,
                Detail: hasAccessPerThread ? accesses[index]
                    : report.Target is null && report.TryGetReportReason(thread, out var reason) ? reason ?? string.Empty
                    : string.Empty,
                Location: report.TryGetStackTrace(thread, out var stackTrace) ? FormatLocation(stackTrace!, hasAccessPerThread) : "<no stack trace>"))
            .ToArray();
        var threadWidth = rows.Select(row => row.Thread.Length).DefaultIfEmpty(0).Max();
        var detailWidth = rows.Select(row => row.Detail.Length).DefaultIfEmpty(0).Max();

        var lines = new List<string>();
        if (report.Target is not null && !hasAccessPerThread)
            lines.Add(report.Title);
        foreach (var row in rows)
        {
            var detail = detailWidth > 0 ? $"{row.Detail.PadRight(detailWidth)}  " : string.Empty;
            lines.Add($"{row.Thread.PadRight(threadWidth)}  {detail}{row.Location}");
        }
        if (!string.IsNullOrWhiteSpace(report.Description))
            lines.Add(report.Description);

        // Reports grouped under a target start with a marker
        foreach (var (index, line) in lines.Index())
            writer.WriteLine($"{(report.Target is not null && index == 0 ? "     - " : "       ")}{line}".TrimEnd());
    }

    private static void PrintHidden(TextWriter writer, string indent, int count)
    {
        if (count > 0)
            writer.WriteLine($"{indent}… {count} more in the report");
    }

    private static string FormatLocation(StackTrace stackTrace, bool methodIsNamed)
    {
        if (stackTrace.Frames.IsDefaultOrEmpty)
            return "<no stack trace>";

        // The access already names the method, so only its source location is missing
        if (methodIsNamed)
            return stackTrace.Frames[0].Source is { } accessSource ? SourceLocationFormatter.FormatShort(accessSource) : string.Empty;

        // Point at the user code that reached the runtime
        var frame = stackTrace.Frames.FirstOrDefault(frame => !WellKnownModules.IsSystemModule(frame.ModulePath)) ?? stackTrace.Frames[0];
        return frame.Source is { } source ? SourceLocationFormatter.FormatShort(source) : frame.MethodName;
    }

    private static string GetReportNoun(string category, int count)
    {
        var noun = category switch
        {
            "DataRace" => "race",
            "Deadlock" => "deadlock",
            _ => "report"
        };
        return count == 1 ? noun : $"{noun}s";
    }
}
