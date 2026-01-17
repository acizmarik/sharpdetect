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

        Reporter.AddCollectionProperty("Distinct Lock Sets", _detector.GetDistinctLockSetCount().ToString());
        Reporter.AddCollectionProperty("Tracked Fields", _detector.GetTrackedFieldCount().ToString());
        Reporter.AddCollectionProperty("(Potential) Data Races", _detectedRaces.Count.ToString());

        return Reporter.Build();
    }

    private void PrepareNoViolationDiagnostics()
    {
        Reporter.SetTitle("No data races detected");
        Reporter.SetDescription("All analyzed static field accesses appear to be properly synchronized.");
    }

    private void PrepareViolationDiagnostics()
    {
        Reporter.SetTitle(_detectedRaces.Count == 1 
            ? "One potential data race detected" 
            : $"Several ({_detectedRaces.Count}) potential data races detected");
        Reporter.SetDescription("See details below for more information about each potential data race.");

        // Group races by field for better reporting
        var racesByField = _detectedRaces
            .GroupBy(r => r.FieldId)
            .ToList();

        var index = 0;
        foreach (var fieldRaces in racesByField)
        {
            var firstRace = fieldRaces.First();
            var fieldId = fieldRaces.Key;
            var fieldName = GetFieldDisplayName(fieldId);

            var reportBuilder = new ReportBuilder(index++, ReportCategory, firstRace.Timestamp);
            reportBuilder.SetTitle($"Data race {reportBuilder.Identifier}");
            
            var description = $"Potential data race detected on field '{fieldName}'. " +
                              $"Total accesses flagged: {fieldRaces.Count()}.";
            reportBuilder.SetDescription(description);

            // Collect all accesses (both current and last) per thread
            var threadAccesses = new Dictionary<ProcessThreadId, List<(DataRaceInfo Race, AccessInfo Access, bool IsCurrent)>>();
            
            foreach (var race in fieldRaces)
            {
                // Add current access
                if (!threadAccesses.ContainsKey(race.CurrentAccess.ProcessThreadId))
                    threadAccesses[race.CurrentAccess.ProcessThreadId] = [];
                threadAccesses[race.CurrentAccess.ProcessThreadId].Add((race, race.CurrentAccess, true));
                
                // Add last access if present
                if (race.LastAccess != null)
                {
                    if (!threadAccesses.ContainsKey(race.LastAccess.ProcessThreadId))
                        threadAccesses[race.LastAccess.ProcessThreadId] = [];
                    threadAccesses[race.LastAccess.ProcessThreadId].Add((race, race.LastAccess, false));
                }
            }
            
            foreach (var (processThreadId, accesses) in threadAccesses)
            {
                var firstAccess = accesses.First().Access;
                var threadInfo = new ThreadInfo(
                    processThreadId.ThreadId.Value, 
                    firstAccess.ThreadName ?? $"Thread-{processThreadId.ThreadId.Value}");
                reportBuilder.AddThread(threadInfo);
                
                var reasons = new List<string>();
                foreach (var (race, access, isCurrent) in accesses.DistinctBy(a => (a.Access.Timestamp, a.Access.AccessType)))
                {
                    var accessRole = isCurrent ? "current" : "previous";
                    var stateTransition = isCurrent 
                        ? $"{race.PreviousState} -> {race.NewState}"
                        : race.PreviousState.ToString();
                    
                    reasons.Add($"{access.AccessType} access ({accessRole}, state: {stateTransition})");
                }
                
                var reason = string.Join("; ", reasons.Distinct());
                reportBuilder.AddReportReason(threadInfo, reason);

                // Create a pseudo-stack trace with method information
                var stackFrames = ImmutableArray.CreateBuilder<StackFrame>();
                var distinctMethods = accesses
                    .Select(a => a.Access)
                    .DistinctBy(a => a.MethodToken.Value);
                
                foreach (var access in distinctMethods)
                {
                    var methodName = ResolveMethodName(processThreadId.ProcessId, access.ModuleId, access.MethodToken);
                    var moduleName = ResolveModuleName(processThreadId.ProcessId, access.ModuleId);
                    var frame = new StackFrame(
                        MethodName: methodName ?? $"0x{access.MethodToken.Value:X8}",
                        SourceMapping: moduleName ?? "<unknown>",
                        MethodToken: access.MethodToken.Value);
                    stackFrames.Add(frame);
                }
                
                if (stackFrames.Count > 0)
                {
                    var stackTrace = new StackTrace(threadInfo, stackFrames.ToImmutable());
                    reportBuilder.AddStackTrace(stackTrace);
                }
            }

            Reporter.AddReport(reportBuilder.Build());
        }
    }

    private string? ResolveMethodName(uint processId, ModuleId moduleId, MdMethodDef methodToken)
    {
        var resolver = MetadataContext.GetResolver(processId);
        var methodResult = resolver.ResolveMethod(processId, moduleId, methodToken);
        
        if (methodResult.IsError)
            return null;

        return methodResult.Value?.FullName;
    }

    private string? ResolveModuleName(uint processId, ModuleId moduleId)
    {
        var resolver = MetadataContext.GetResolver(processId);
        var moduleResult = resolver.ResolveModule(processId, moduleId);
        
        if (moduleResult.IsError)
            return null;

        return moduleResult.Value?.Location;
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
