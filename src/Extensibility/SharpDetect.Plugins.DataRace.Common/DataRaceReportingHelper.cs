// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting;
using SharpDetect.Core.Reporting.Formatters;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace.Common;

public abstract class DataRaceReportingHelper
{
    private readonly string _reportCategory;
    private readonly SummaryBuilder _reporter;
    private readonly IMetadataContext _metadataContext;
    private readonly ISymbolResolver _symbolResolver;
    private readonly List<DataRaceInfo> _detectedRaces;

    protected int RaceOccurrenceCount => _detectedRaces.Count;

    protected DataRaceReportingHelper(
        SummaryBuilder reporter,
        IMetadataContext metadataContext,
        ISymbolResolver symbolResolver,
        string reportCategory,
        List<DataRaceInfo> detectedRaces)
    {
        _reporter = reporter;
        _metadataContext = metadataContext;
        _symbolResolver = symbolResolver;
        _reportCategory = reportCategory;
        _detectedRaces = detectedRaces;
    }

    protected abstract string GetViolationTitle(int raceCount, int fieldCount);
    protected abstract string FormatAccessReason(DataRaceInfo race, AccessInfo access, RaceRole role);
    protected abstract void AddStatisticsToReport(SummaryBuilder reporter, int raceCount, int fieldCount);

    public Summary CreateDiagnostics()
    {
        var (raceCount, fieldCount) = PrepareViolationDiagnostics();
        AddStatisticsToReport(_reporter, raceCount, fieldCount);
        return _reporter.Build();
    }

    private sealed record ResolvedRace(
        RaceGroup Group,
        IReadOnlyList<StackFrame> EarlierFrames,
        IReadOnlyList<StackFrame> LaterFrames)
    {
        public bool HasUserCode =>
            EarlierFrames.Concat(LaterFrames).Any(frame => !WellKnownModules.IsSystemModule(frame.ModulePath));
    }

    private (int RaceCount, int FieldCount) PrepareViolationDiagnostics()
    {
        if (_detectedRaces.Count == 0)
        {
            _reporter.SetTitle("No data races detected");
            _reporter.SetDescription("All analyzed field accesses appear properly synchronized.");
            return (0, 0);
        }

        var (races, fieldCount) = Rank([.. RaceAggregator.Aggregate(_detectedRaces).Select(Resolve)]);

        _reporter.SetTitle(GetViolationTitle(races.Count, fieldCount));
        _reporter.SetDescription(races.Count == RaceOccurrenceCount
            ? "Each race is a pair of conflicting accesses on one field."
            : $"Each race is a pair of conflicting accesses on one field, folded from {RaceOccurrenceCount} raw detector events.");

        var index = 0;
        foreach (var race in races)
            _reporter.AddReport(CreateReport(index++, race));

        return (races.Count, fieldCount);
    }

    private ResolvedRace Resolve(RaceGroup group)
    {
        return new ResolvedRace(
            group,
            ResolveFrames(group.Representative.ProcessId, group.Earlier),
            ResolveFrames(group.Representative.ProcessId, group.Later));
    }

    private IReadOnlyList<StackFrame> ResolveFrames(uint processId, AccessInfo access)
        => DataRaceStackTraceResolver.ResolveFrames(processId, access, _metadataContext, _symbolResolver);

    private static (List<ResolvedRace> Races, int FieldCount) Rank(List<ResolvedRace> races)
    {
        var fields = races
            .GroupBy(GetFieldKey)
            .Select(group => new
            {
                Name = group.Key.Name,
                HasUserCode = group.Any(race => race.HasUserCode),
                RaceCount = group.Count(),
                Races = group.OrderByDescending(race => race.Group.OccurrenceCount).ToArray()
            })
            .OrderByDescending(field => field.HasUserCode)
            .ThenByDescending(field => field.RaceCount)
            .ThenBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();

        return ([.. fields.SelectMany(field => field.Races)], fields.Length);
    }

    private static (uint ProcessId, string Name) GetFieldKey(ResolvedRace race)
        => (race.Group.Representative.ProcessId, DataRaceLogger.GetFieldDisplayName(race.Group.FieldId));

    private Report CreateReport(int index, ResolvedRace race)
    {
        var group = race.Group;
        var representative = group.Representative;
        var reportBuilder = new ReportBuilder(index, _reportCategory, representative.ProcessId, representative.Timestamp);

        reportBuilder.SetTarget(DataRaceLogger.GetFieldDisplayName(group.FieldId));
        reportBuilder.SetTitle(FormatRaceTitle(race));
        reportBuilder.SetDescription(FormatEvidenceSummary(group));

        AddAccessToReport(reportBuilder, race, RaceRole.Earlier);
        AddAccessToReport(reportBuilder, race, RaceRole.Later);

        return reportBuilder.Build();
    }

    private void AddAccessToReport(ReportBuilder reportBuilder, ResolvedRace race, RaceRole role)
    {
        var isEarlier = role == RaceRole.Earlier;
        var access = isEarlier ? race.Group.Earlier : race.Group.Later;
        var frames = isEarlier ? race.EarlierFrames : race.LaterFrames;

        var threadInfo = new ThreadInfo(
            access.ProcessThreadId.ThreadId.Value,
            DataRaceLogger.GetThreadDisplayName(access),
            isEarlier ? 0 : 1);

        reportBuilder.AddThread(threadInfo);
        reportBuilder.AddReportReason(threadInfo, FormatAccessReason(race.Group.Representative, access, role));
        reportBuilder.AddStackTrace(new StackTrace(threadInfo, [.. frames]));
    }

    private static string FormatRaceTitle(ResolvedRace race)
    {
        var earlier = FormatSite(race.Group.Earlier, race.EarlierFrames);
        var later = FormatSite(race.Group.Later, race.LaterFrames);
        return $"{earlier} ↔ {later}";
    }

    private static string FormatSite(AccessInfo access, IReadOnlyList<StackFrame> frames)
    {
        var methodName = frames.Count > 0 ? frames[0].MethodName : "<unresolved-method>";
        return $"{access.AccessType} {methodName}";
    }

    private static string FormatEvidenceSummary(RaceGroup group)
    {
        var parts = new List<string>
        {
            group.OccurrenceCount == 1 ? "1 occurrence" : $"{group.OccurrenceCount} occurrences"
        };

        if (group.ObjectIds.Length > 0)
            parts.Add(group.ObjectIds.Length == 1 ? "1 object" : $"{group.ObjectIds.Length} objects");

        parts.Add(group.FieldId.FieldDef.IsStatic ? "static field" : "instance field");

        if (group.BothOrderingsObserved)
            parts.Add("both orderings observed");

        return string.Join(" · ", parts);
    }

    public static IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports, int stackTraceMaxDepth)
    {
        return reports
            .GroupBy(report => (report.ProcessId, Key: report.Target ?? report.Title))
            .Select(group => new
            {
                target = group.Key.Key,
                shortTarget = ComputeShortTarget(group.Key.Key),
                processId = group.Key.ProcessId.ToString(),
                raceCountLabel = group.Count() == 1 ? "1 race" : $"{group.Count()} races",
                children = group.Select(report => BuildRaceCard(report, stackTraceMaxDepth)).ToArray()
            });
    }

    private static object BuildRaceCard(Report report, int stackTraceMaxDepth)
    {
        return new
        {
            processId = report.ProcessId.ToString(),
            title = report.Title,
            description = report.Description,
            timestamp = report.DetectionTime,
            threads = report.GetReportedThreads()
                .OrderBy(threadInfo => threadInfo.AccessIndex)
                .Select(threadInfo =>
                {
                    report.TryGetReportReason(threadInfo, out var reason);
                    report.TryGetStackTrace(threadInfo, out var stackTrace);
                    var frameCount = stackTrace?.Frames.Length ?? 0;
                    return new
                    {
                        name = threadInfo.Name,
                        role = threadInfo.AccessIndex == 0 ? "Earlier" : "Later",
                        isEarlier = threadInfo.AccessIndex == 0,
                        reason = reason ?? "Unknown",
                        isStackAtDepthLimit = frameCount > 1 && frameCount >= stackTraceMaxDepth,
                        stackTraceMaxDepth,
                        stacktrace = BuildStackTraceSegments(stackTrace)
                    };
                }).ToArray()
        };
    }

    private static string ComputeShortTarget(string fullTarget)
    {
        var lastDot = fullTarget.LastIndexOf('.');
        if (lastDot <= 0)
            return fullTarget;

        var withoutField = fullTarget[..lastDot];
        var lastSlash = withoutField.LastIndexOf('/');
        if (lastSlash >= 0)
        {
            var nestedType = withoutField[(lastSlash + 1)..];
            var fieldName = fullTarget[(lastDot + 1)..];
            return $"{nestedType}.{fieldName}";
        }

        var secondLastDot = withoutField.LastIndexOf('.');
        return secondLastDot >= 0
            ? fullTarget[(secondLastDot + 1)..]
            : fullTarget;
    }

    public static IReadOnlyList<object> BuildStackTraceSegments(StackTrace? stackTrace)
    {
        if (stackTrace is null || stackTrace.Frames.IsDefaultOrEmpty)
            return [];

        var segments = new List<object>();
        var globalIndex = 0;
        foreach (var run in StackFrameGrouping.GroupSystemFrameRuns(stackTrace.Frames))
        {
            var projected = new object[run.Frames.Count];
            for (var i = 0; i < run.Frames.Count; i++)
            {
                var frame = run.Frames[i];
                var snippet = frame.Source?.Snippet ?? SourceCodeSnippet.None;
                projected[i] = new
                {
                    metadataName = frame.MethodName,
                    metadataToken = TokenFormatters.FormatMethodToken(frame.MethodToken),
                    methodOffset = frame.Il is { } il ? InstructionsFormatter.FormatIlOffset(il.Offset) : null,
                    instruction = frame.Il?.Instruction,
                    assemblyPath = frame.ModulePath,
                    assemblyFileName = Path.GetFileName(frame.ModulePath),
                    sourceFileName = frame.Source?.DocumentPath,
                    sourceLine = frame.Source?.Line,
                    hasSourceLines = snippet.Lines.Length > 0,
                    sourceLines = snippet.Lines
                        .Select(line => new
                        {
                            number = line.LineNumber,
                            text = line.Text,
                            isHighlighted = line.IsHighlighted
                        })
                        .ToArray(),
                    isSourceOutOfDate = snippet.IsOutOfDate,
                    isTopFrame = globalIndex == 0,
                    isSystemFrame = WellKnownModules.IsSystemModule(frame.ModulePath)
                };
                globalIndex++;
            }

            segments.Add(new
            {
                isCollapsed = run.Frames.Count > 1,
                count = run.Frames.Count,
                frames = projected
            });
        }

        return segments;
    }
}
