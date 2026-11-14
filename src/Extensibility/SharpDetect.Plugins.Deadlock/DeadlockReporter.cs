// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
using SharpDetect.Core.Reporting.Formatters;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.Deadlock;

public partial class DeadlockPlugin
{
    public Summary CreateDiagnostics()
    {
        if (_deadlocks.Count != 0)
            PrepareViolationDiagnostics();
        else
            PrepareNoViolationDiagnostics();

        return Reporter.Build();
    }

    private void PrepareNoViolationDiagnostics()
    {
        Reporter.SetTitle("No deadlocks detected");
        Reporter.SetDescription("No circular wait dependencies were found.");
    }

    private void PrepareViolationDiagnostics()
    {
        Reporter.SetTitle(_deadlocks.Count == 1 ? "1 deadlock" : $"{_deadlocks.Count} deadlocks");
        Reporter.SetDescription("Each deadlock is a cycle of threads that wait for each other.");

        var index = 0;
        foreach (var deadlock in _deadlocks)
        {
            var processId = deadlock.Key.Pid;
            var requestId = deadlock.Key.RequestId;
            var cycle = deadlock.Value.Cycle;
            var timestamp = deadlock.Value.TimeStamp;
            
            var reportBuilder = new ReportBuilder(index++, ReportCategory, processId, timestamp);
            var reportedThreads = cycle
                .Select(t => new ThreadInfo(t.ProcessThreadId.ThreadId.Value, t.ThreadName))
                .ToDictionary(t => t.Id, t => t);
            var stackTraces = new Dictionary<ThreadInfo, StackTrace>();
            
            // Check if stack traces are available (they might not be if the program terminated early)
            var hasStackTraces = _deadlockStackTraces.TryGetValue((processId, requestId), out var callstacks);
            
            foreach (var threadInfo in cycle)
            {
                var id = threadInfo.ProcessThreadId;
                var callstack = new Callstack(id);
                if (hasStackTraces && callstacks!.Snapshots.SingleOrDefault(s => s.ThreadId == id.ThreadId) is { } snapshot)
                {
                    for (var frameIndex = snapshot.MethodTokens.Length - 1; frameIndex >= 0; frameIndex--)
                        callstack.Push(snapshot.ModuleIds[frameIndex], snapshot.MethodTokens[frameIndex]);
                }
                
                var reportedThreadInfo = reportedThreads[id.ThreadId.Value];
                var stackTrace = _callStackResolver.Resolve(reportedThreadInfo, callstack);
                
                reportBuilder.AddReportReason(reportedThreadInfo, DeadlockFormatter.FormatReason(deadlock.Value, threadInfo));
                stackTraces.Add(reportedThreadInfo, stackTrace);
            }

            reportBuilder.SetTitle(DeadlockFormatter.FormatTitle(deadlock.Value));
            var description = DeadlockFormatter.FormatDescription(deadlock.Value);
            if (!hasStackTraces)
                description += " Stack traces are unavailable because the program terminated before they could be captured.";
            reportBuilder.SetDescription(description);
            foreach (var threadInfo in reportedThreads.Values)
                reportBuilder.AddThread(threadInfo);
            foreach (var (_, stackTrace) in stackTraces)
                reportBuilder.AddStackTrace(stackTrace);

            Reporter.AddReport(reportBuilder.Build());
        }
    }

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports)
    {
        return reports.Select(report => new
        {
            title = report.Title,
            reportId = $"report-{report.Identifier}",
            processId = report.ProcessId.ToString(),
            description = report.Description,
            timestamp = report.DetectionTime,
            threads = report.GetReportedThreads().Select(threadInfo =>
            {
                report.TryGetStackTrace(threadInfo, out var st);
                report.TryGetReportReason(threadInfo, out var reason);
                return new
                {
                    name = threadInfo.Name,
                    reason = reason!,
                    stacktrace = st!.Frames.Select(frame => new
                    {
                        metadataName = frame.MethodName,
                        metadataToken = TokenFormatters.FormatMethodToken(frame.MethodToken),
                        methodOffset = frame.Il is { } il ? InstructionsFormatter.FormatIlOffset(il.Offset) : null,
                        assemblyPath = frame.ModulePath,
                        sourceFileName = frame.Source?.DocumentPath,
                        sourceLine = frame.Source?.Line,
                    }).ToArray()
                };
            }).ToArray()
        });
    }
}
