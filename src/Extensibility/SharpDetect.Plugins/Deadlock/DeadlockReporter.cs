// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
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
        Reporter.SetTitle("No violations found");
        Reporter.SetDescription("All analyzed lock acquires were correctly ordered.");
    }

    private void PrepareViolationDiagnostics()
    {
        Reporter.SetTitle(_deadlocks.Count == 1 ? "One violation found" : $"Several ({_deadlocks.Count}) violations found");
        Reporter.SetDescription("See details below for more information.");

        var index = 0;
        foreach (var (processId, cycle) in _deadlocks)
        {
            var reportBuilder = new ReportBuilder(index++, ReportCategory);
            var threadNames = cycle.ToDictionary(c => c.ThreadId, c => c.ThreadName);
            var reportedThreads = cycle
                .Select(t => new ThreadInfo(t.ThreadId.Value, t.ThreadName))
                .ToDictionary(t => t.Id, t => t);
            var stackTraces = new Dictionary<ThreadInfo, StackTrace>();
            foreach (var (threadId, threadName, blockedOnThreadId, lockId) in cycle)
            {
                var threadInfo = reportedThreads[threadId.Value];
                var stackTrace = _callStackResolver.Resolve(threadInfo, Callstacks[threadId]);
                reportBuilder.AddReportReason(threadInfo, $"Blocked - waiting for object: {lockId.Value} owned by thread {threadNames[blockedOnThreadId]}");
                stackTraces.Add(threadInfo, stackTrace);
            }

            reportBuilder.SetTitle($"Deadlock {reportBuilder.Identifier}");
            reportBuilder.SetDescription($"Multiple threads ({reportedThreads.Count}) are blocked in a cycle. See details below for more information.");
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
            description = report.Description,
            threads = report.GetReportedThreads().Select(threadInfo =>
            {
                report.TryGetStackTrace(threadInfo, out var st);
                report.TryGetReportReason(threadInfo, out var reason);
                return new
                {
                    name = threadInfo.Name,
                    reason = reason!,
                    stacktrace = st!.Frames.Select(frame =>
                    {
                        return new
                        {
                            metadataName = frame.MethodName,
                            metadataToken = frame.MethodToken,
                            sourceFile = frame.SourceMapping,
                        };
                    }).ToArray()
                };
            }).ToArray()
        });
    }
}
