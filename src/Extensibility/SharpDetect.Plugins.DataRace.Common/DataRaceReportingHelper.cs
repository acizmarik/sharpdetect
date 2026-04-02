// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace.Common;

public abstract class DataRaceReportingHelper
{
    private readonly string _reportCategory;
    private readonly SummaryBuilder _reporter;
    private readonly IMetadataContext _metadataContext;
    private readonly ISymbolResolver _symbolResolver;
    private readonly List<DataRaceInfo> _detectedRaces;

    protected int DetectedRaceCount => _detectedRaces.Count;

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
    
    protected abstract string GetViolationTitle(int raceCount);
    protected abstract string FormatAccessReason(DataRaceInfo race, AccessInfo access, RaceRole role);
    protected abstract void AddStatisticsToReport(SummaryBuilder reporter);
    
    public Summary CreateDiagnostics()
    {
        if (_detectedRaces.Count != 0)
            PrepareViolationDiagnostics();
        else
            PrepareNoViolationDiagnostics();

        AddStatisticsToReport(_reporter);
        return _reporter.Build();
    }

    private const int MaxObjectLabels = 5;

    public static IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports)
    {
        return reports
            .GroupBy(report => report.Target ?? report.Title)
            .Select(group =>
            {
                var isGrouped = group.Any(r => r.Target != null);
                var children = BuildMergedChildren(group).ToArray();
                return new
                {
                    target = group.Key,
                    shortTarget = ComputeShortTarget(group.Key),
                    isGrouped,
                    instanceCount = group.Count(),
                    children
                };
            });
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

    private static string ComputeChildFingerprint(Report report)
    {
        var parts = report.GetReportedThreads()
            .SelectMany(t =>
            {
                report.TryGetStackTrace(t, out var st);
                return st?.Frames.Select(f => $"{f.MethodToken}@{f.MethodOffset}")
                       ?? [];
            })
            .OrderBy(s => s)
            .Distinct();
        return string.Join("|", parts);
    }

    private static IEnumerable<object> BuildMergedChildren(IEnumerable<Report> reports)
    {
        return reports
            .GroupBy(ComputeChildFingerprint)
            .Select(fg =>
            {
                var representative = fg.First();
                var objectCount = fg.Count();

                var allLabels = fg
                    .Where(r => r.Target is not null)
                    .Select(r =>
                    {
                        var desc = r.Description;
                        var parenIdx = desc.IndexOf('(');
                        return parenIdx > 0 ? desc[..parenIdx].TrimEnd() : desc;
                    })
                    .ToArray();

                var displayLabels = allLabels.Take(MaxObjectLabels).ToArray();
                var extraObjectCount = allLabels.Length - displayLabels.Length;

                var accessCount = representative.GetReportedThreads().Count();
                var summaryText = objectCount > 1
                    ? $"{objectCount} objects · {accessCount} distinct access locations"
                    : representative.Description;

                return (object)new
                {
                    reportId = $"report-{representative.Identifier}",
                    description = representative.Description,
                    summaryText,
                    objectCount,
                    hasObjectLabels = displayLabels.Length > 0,
                    objectLabels = displayLabels,
                    hasExtraObjects = extraObjectCount > 0,
                    extraObjectCount,
                    timestamp = representative.DetectionTime,
                    threads = representative.GetReportedThreads().Select(threadInfo =>
                    {
                        representative.TryGetReportReason(threadInfo, out var reason);
                        representative.TryGetStackTrace(threadInfo, out var st);
                        return new
                        {
                            name = threadInfo.Name,
                            reason = reason ?? "Unknown",
                            stacktrace = st?.Frames.Select(frame => new
                            {
                                metadataName = frame.MethodName,
                                metadataToken = frame.MethodToken,
                                methodOffset = $"IL_{frame.MethodOffset:X4}",
                                instruction = frame.Instruction,
                                sourceFile = frame.SourceMapping,
                                sourceFileName = frame.SourceFileName,
                                sourceLine = frame.SourceLine,
                                sourceCode = frame.SourceCode,
                            }).ToArray() ?? []
                        };
                    }).ToArray()
                };
            });
    }
    
    private void PrepareNoViolationDiagnostics()
    {
        _reporter.SetTitle("No data races detected");
        _reporter.SetDescription("All field accesses appear properly synchronized.");
    }

    private void PrepareViolationDiagnostics()
    {
        _reporter.SetTitle(GetViolationTitle(_detectedRaces.Count));
        _reporter.SetDescription("See details below.");

        var racesByFieldAndObject = _detectedRaces
            .GroupBy(r => (r.FieldId, r.ObjectId));
        CreateReportsForRaces(racesByFieldAndObject);
    }

    private void CreateReportsForRaces(IEnumerable<IGrouping<(FieldId FieldId, ProcessTrackedObjectId? ObjectId), DataRaceInfo>> racesByFieldAndObject)
    {
        var index = 0;
        foreach (var group in racesByFieldAndObject)
        {
            var report = CreateReportForFieldAndObject(index++, group);
            _reporter.AddReport(report);
        }
    }

    private Report CreateReportForFieldAndObject(int index, IGrouping<(FieldId FieldId, ProcessTrackedObjectId? ObjectId), DataRaceInfo> group)
    {
        var firstRace = group.First();
        var fieldTitle = DataRaceLogger.GetFieldTitle(group.Key.FieldId);
        var fieldName = DataRaceLogger.GetFieldDisplayName(group.Key.FieldId);

        var reportBuilder = new ReportBuilder(index, _reportCategory, firstRace.Timestamp);
        if (group.Key.ObjectId is not null)
        {
            reportBuilder.SetTitle(fieldTitle);
            reportBuilder.SetTarget(fieldName);
        }
        else
        {
            reportBuilder.SetTitle(fieldName);
        }

        var accessCollector = CollectThreadAccesses(group);

        // Count distinct access locations (after field-level deduplication)
        var distinctAccessCount = accessCollector.GetThreads()
            .Sum(t => accessCollector.GetDistinctAccesses(t).Count());
        
        reportBuilder.SetDescription(group.Key.ObjectId is { } objectId
            ? $"Object {objectId.ObjectId.Value} ({distinctAccessCount} distinct access locations)"
            : $"{distinctAccessCount} distinct access locations");

        AddThreadsToReport(reportBuilder, accessCollector);

        return reportBuilder.Build();
    }

    private static ThreadAccessCollector CollectThreadAccesses(IEnumerable<DataRaceInfo> races)
    {
        var collector = new ThreadAccessCollector();
        foreach (var race in races)
            collector.AddRace(race);

        return collector;
    }

    private void AddThreadsToReport(ReportBuilder reportBuilder, ThreadAccessCollector accessCollector)
    {
        var accessIndex = 0;
        foreach (var threadId in accessCollector.GetThreads())
        {
            foreach (var (race, access, role) in accessCollector.GetDistinctAccesses(threadId))
            {
                var displayName = access.ThreadName ?? $"Thread-{threadId.ThreadId.Value}";
                var threadInfo = new ThreadInfo(threadId.ThreadId.Value, displayName, accessIndex++);
                reportBuilder.AddThread(threadInfo);

                var reason = FormatAccessReason(race, access, role);
                reportBuilder.AddReportReason(threadInfo, reason);

                var frame = CreateStackFrame(threadId.ProcessId, access);
                var stackTrace = new StackTrace(threadInfo, [frame]);
                reportBuilder.AddStackTrace(stackTrace);
            }
        }
    }

    private StackFrame CreateStackFrame(uint processId, AccessInfo access)
    {
        var resolver = _metadataContext.GetResolver(processId);
        var moduleResolveResult = resolver.ResolveModule(processId, access.ModuleId);
        var methodResolveResult = resolver.ResolveMethod(processId, access.ModuleId, access.MethodToken);
        var moduleName = moduleResolveResult.IsSuccess
            ? moduleResolveResult.Value.Location
            : "<unresolved-module>";
        var methodName = methodResolveResult.IsSuccess
            ? methodResolveResult.Value.FullName
            : $"<unresolved-method>({access.MethodToken.Value})";
        var instruction = methodResolveResult.IsSuccess
            ? methodResolveResult.Value.Body.Instructions.SingleOrDefault(instr => instr.Offset == access.MethodOffset)?.ToString()
            : null;
        instruction ??= $"<unresolved-instruction>(IL_{access.MethodOffset:X4})";
        var symbolInfo = _symbolResolver.ResolveSequencePoint(
            processId, access.ModuleId, access.MethodToken.Value, access.MethodOffset);
        var sourceCode = TryReadSourceLine(symbolInfo?.DocumentUrl, symbolInfo?.StartLine);

        return new StackFrame(
            MethodName: methodName,
            SourceMapping: moduleName,
            MethodToken: access.MethodToken.Value,
            MethodOffset: access.MethodOffset,
            Instruction: instruction,
            SourceFileName: symbolInfo?.DocumentUrl,
            SourceLine: symbolInfo?.StartLine,
            SourceCode: sourceCode);
    }

    private static string? TryReadSourceLine(string? documentUrl, int? line)
    {
        if (documentUrl is null || line is null || line.Value < 1)
            return null;

        try
        {
            if (!File.Exists(documentUrl))
                return null;

            return File.ReadLines(documentUrl)
                .Skip(line.Value - 1)
                .FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}


