// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins;
using SharpDetect.Core.Plugins.Models;
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
        Reporter.SetDescription("All analyzed synchronizations events were correctly ordered.");
    }

    private void PrepareViolationDiagnostics()
    {
        Reporter.SetTitle(_deadlocks.Count == 1 ? "One violation found" : $"Several ({_deadlocks.Count}) violations found");
        Reporter.SetDescription("See details below for more information.");

        var index = 0;
        foreach (var deadlock in _deadlocks)
        {
            var processId = deadlock.Key.Pid;
            var requestId = deadlock.Key.RequestId;
            var cycle = deadlock.Value.Cycle;
            
            var reportBuilder = new ReportBuilder(index++, ReportCategory);
            var threadNames = cycle.ToDictionary(c => c.ThreadId, c => c.ThreadName);
            var reportedThreads = cycle
                .Select(t => new ThreadInfo(t.ThreadId.Value, t.ThreadName))
                .ToDictionary(t => t.Id, t => t);
            var stackTraces = new Dictionary<ThreadInfo, StackTrace>();
            
            var callstacks = _deadlockStackTraces[(processId, requestId)];
            foreach (var threadInfo in cycle)
            {
                var processThreadId = new ProcessThreadId(processId, threadInfo.ThreadId);
                var callstack = new Callstack(processThreadId);
                if (callstacks.Snapshots.SingleOrDefault(s => s.ThreadId == threadInfo.ThreadId) is { } snapshot)
                {
                    for (var frameIndex = snapshot.MethodTokens.Length - 1; frameIndex >= 0; frameIndex--)
                        callstack.Push(snapshot.ModuleIds[frameIndex], snapshot.MethodTokens[frameIndex]);
                }
                
                var reportedThreadInfo = reportedThreads[threadInfo.ThreadId.Value];
                var stackTrace = _callStackResolver.Resolve(reportedThreadInfo, callstack);
                
                // Generate appropriate reason based on blocking type
                var reason = threadInfo.BlockedOnType switch
                {
                    BlockedOnType.Lock => $"Blocked - waiting for lock object: {threadInfo.LockId!.Value.Value} owned by thread {threadNames[threadInfo.BlockedOn]}",
                    BlockedOnType.Thread => $"Blocked - waiting for thread {threadNames[threadInfo.BlockedOn]} to complete (Thread.Join)",
                    _ => throw new InvalidOperationException($"Unknown blocking type: {threadInfo.BlockedOnType}")
                };
                
                reportBuilder.AddReportReason(reportedThreadInfo, reason);
                stackTraces.Add(reportedThreadInfo, stackTrace);
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
