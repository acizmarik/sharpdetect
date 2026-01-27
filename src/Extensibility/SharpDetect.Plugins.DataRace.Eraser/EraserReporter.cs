// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace.Eraser;

public partial class EraserPlugin
{
    public Summary CreateDiagnostics()
    {
        if (_detectedRaces.Count != 0)
            PrepareViolationDiagnostics();
        else
            PrepareNoViolationDiagnostics();

        AddStatisticsToReport();
        return Reporter.Build();
    }

    private void AddStatisticsToReport()
    {
        Reporter.AddCollectionProperty("Distinct Lock Sets", _detector.GetDistinctLockSetCount().ToString());
        Reporter.AddCollectionProperty("Tracked Fields", _detector.GetTrackedFieldCount().ToString());
        Reporter.AddCollectionProperty("(Potential) Data Races", _detectedRaces.Count.ToString());
    }

    private void PrepareNoViolationDiagnostics()
    {
        Reporter.SetTitle("No data races detected");
        Reporter.SetDescription("All analyzed static field accesses appear to be properly synchronized.");
    }

    private void PrepareViolationDiagnostics()
    {
        var title = _detectedRaces.Count == 1
            ? "One potential data race detected"
            : $"Several ({_detectedRaces.Count}) potential data races detected";
        Reporter.SetTitle(title);
        Reporter.SetDescription("See details below for more information about each potential data race.");

        var racesByField = _detectedRaces.GroupBy(r => r.FieldId);
        CreateReportsForRaces(racesByField);
    }
    
    private void CreateReportsForRaces(IEnumerable<IGrouping<FieldId, DataRaceInfo>> racesByField)
    {
        var index = 0;
        foreach (var fieldRaces in racesByField)
        {
            var report = CreateReportForField(index++, fieldRaces);
            Reporter.AddReport(report);
        }
    }

    private Report CreateReportForField(int index, IGrouping<FieldId, DataRaceInfo> fieldRaces)
    {
        var firstRace = fieldRaces.First();
        var fieldName = GetFieldDisplayName(fieldRaces.Key);

        var reportBuilder = new ReportBuilder(index, ReportCategory, firstRace.Timestamp);
        reportBuilder.SetTitle($"Data race {reportBuilder.Identifier}");
        reportBuilder.SetDescription(
            $"Potential data race detected on field '{fieldName}'. " +
            $"Total accesses flagged: {fieldRaces.Count()}.");

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
        return new ThreadInfo(
            threadId.ThreadId.Value,
            firstAccess.ThreadName ?? $"Thread-{threadId.ThreadId.Value}");
    }

    private static string BuildReasonString(ProcessThreadId threadId, ThreadAccessCollector accessCollector)
    {
        var reasons = new List<string>();

        foreach (var (race, access, isCurrent) in accessCollector.GetDistinctAccesses(threadId))
        {
            var accessRole = isCurrent ? "current" : "previous";
            var stateTransition = isCurrent
                ? $"{race.PreviousState} -> {race.NewState}"
                : race.PreviousState.ToString();

            reasons.Add($"{access.AccessType} access ({accessRole}, state: {stateTransition})");
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
        var methodName = ResolveMethodName(processId, access.ModuleId, access.MethodToken);
        var moduleName = ResolveModuleName(processId, access.ModuleId);

        return new StackFrame(
            MethodName: methodName ?? $"0x{access.MethodToken.Value:X8}",
            SourceMapping: moduleName ?? "<unknown>",
            MethodToken: access.MethodToken.Value);
    }

    private string? ResolveMethodName(uint processId, ModuleId moduleId, MdMethodDef methodToken)
    {
        var resolver = MetadataContext.GetResolver(processId);
        var methodResult = resolver.ResolveMethod(processId, moduleId, methodToken);
        return methodResult.IsError ? null : methodResult.Value?.FullName;
    }

    private string? ResolveModuleName(uint processId, ModuleId moduleId)
    {
        var resolver = MetadataContext.GetResolver(processId);
        var moduleResult = resolver.ResolveModule(processId, moduleId);
        return moduleResult.IsError ? null : moduleResult.Value?.Location;
    }

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports)
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
                        sourceFile = frame.SourceMapping,
                    }).ToArray() ?? []
                };
            }).ToArray()
        });
    }
}
