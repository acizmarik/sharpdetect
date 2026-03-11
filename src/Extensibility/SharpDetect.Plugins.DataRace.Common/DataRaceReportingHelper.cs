// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace.Common;

public abstract class DataRaceReportingHelper
{
    private readonly string _reportCategory;
    private readonly SummaryBuilder _reporter;
    private readonly IMetadataContext _metadataContext;
    private readonly List<DataRaceInfo> _detectedRaces;

    protected int DetectedRaceCount => _detectedRaces.Count;

    protected DataRaceReportingHelper(
        SummaryBuilder reporter,
        IMetadataContext metadataContext,
        string reportCategory,
        List<DataRaceInfo> detectedRaces)
    {
        _reporter = reporter;
        _metadataContext = metadataContext;
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

    public static IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports)
    {
        return reports.Select(report => new
        {
            title = report.Title,
            reportId = $"report-{report.Identifier}",
            description = report.Description,
            timestamp = report.DetectionTime,
            threads = report.GetReportedThreads().Select(threadInfo =>
            {
                report.TryGetReportReason(threadInfo, out var reason);
                report.TryGetStackTrace(threadInfo, out var st);
                return new
                {
                    name = threadInfo.Name,
                    reason = reason ?? "Unknown",
                    stacktrace = st?.Frames.Select(frame => new
                    {
                        metadataName = frame.MethodName,
                        metadataToken = frame.MethodToken,
                        methodOffset = $"IL_{frame.MethodOffset:X4}",
                        sourceFile = frame.SourceMapping,
                        sourceFileName = frame.SourceFileName,
                        sourceLine = frame.SourceLine,
                    }).ToArray() ?? []
                };
            }).ToArray()
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

        var racesByField = _detectedRaces.GroupBy(r => r.FieldId);
        CreateReportsForRaces(racesByField);
    }

    private void CreateReportsForRaces(IEnumerable<IGrouping<FieldId, DataRaceInfo>> racesByField)
    {
        var index = 0;
        foreach (var fieldRaces in racesByField)
        {
            var report = CreateReportForField(index++, fieldRaces);
            _reporter.AddReport(report);
        }
    }

    private Report CreateReportForField(int index, IGrouping<FieldId, DataRaceInfo> fieldRaces)
    {
        var firstRace = fieldRaces.First();
        var fieldName = DataRaceLogger.GetFieldDisplayName(fieldRaces.Key);
        var category = DataRaceLogger.GetRaceCategory(firstRace);

        var reportBuilder = new ReportBuilder(index, _reportCategory, firstRace.Timestamp);
        reportBuilder.SetTitle($"Data race {reportBuilder.Identifier}");
        reportBuilder.SetDescription($"Data race ({category}) on '{fieldName}' ({fieldRaces.Count()} flagged accesses).");

        var accessCollector = CollectThreadAccesses(fieldRaces);
        AddThreadsToReport(reportBuilder, accessCollector);

        return reportBuilder.Build();
    }

    private static ThreadAccessCollector CollectThreadAccesses(IEnumerable<DataRaceInfo> fieldRaces)
    {
        var collector = new ThreadAccessCollector();
        foreach (var race in fieldRaces)
            collector.AddRace(race);

        return collector;
    }

    private void AddThreadsToReport(ReportBuilder reportBuilder, ThreadAccessCollector accessCollector)
    {
        foreach (var threadId in accessCollector.GetThreads())
        {
            var threadInfo = CreateThreadInfo(threadId, accessCollector);
            reportBuilder.AddThread(threadInfo);

            var reason = BuildReasonString(threadId, accessCollector);
            reportBuilder.AddReportReason(threadInfo, reason);

            var stackTrace = BuildStackTrace(threadId, threadInfo, accessCollector);
            if (stackTrace != null)
                reportBuilder.AddStackTrace(stackTrace);
        }
    }

    private static ThreadInfo CreateThreadInfo(ProcessThreadId threadId, ThreadAccessCollector accessCollector)
    {
        var firstAccess = accessCollector.GetFirstAccess(threadId);
        var displayName = firstAccess.ThreadName ?? $"Thread-{threadId.ThreadId.Value}";
        return new ThreadInfo(
            threadId.ThreadId.Value,
            displayName);
    }

    private string BuildReasonString(ProcessThreadId threadId, ThreadAccessCollector accessCollector)
    {
        var reasons = new List<string>();

        foreach (var (race, access, role) in accessCollector.GetDistinctAccesses(threadId))
        {
            reasons.Add(FormatAccessReason(race, access, role));
        }

        return string.Join("; ", reasons.Distinct());
    }

    private StackTrace? BuildStackTrace(
        ProcessThreadId threadId,
        ThreadInfo threadInfo,
        ThreadAccessCollector accessCollector)
    {
        var stackFrames = ImmutableArray.CreateBuilder<StackFrame>();

        foreach (var access in accessCollector.GetDistinctMethods(threadId))
        {
            var frame = CreateStackFrame(threadId.ProcessId, access);
            stackFrames.Add(frame);
        }

        return stackFrames.Count > 0
            ? new StackTrace(threadInfo, stackFrames.ToImmutable())
            : null;
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
        var sequencePoint = methodResolveResult.Value.Body.Instructions
            .FirstOrDefault(e => e.Offset == access.MethodOffset)?.SequencePoint;

        return new StackFrame(
            MethodName: methodName,
            SourceMapping: moduleName,
            MethodToken: access.MethodToken.Value,
            MethodOffset: access.MethodOffset,
            SourceFileName: sequencePoint?.Document.Url,
            SourceLine: sequencePoint?.StartLine);
    }
}


