// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;
using System.Collections.Immutable;

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
                var stackTrace = CreateStackTrace(processId, threadId, threadName);
                var threadInfo = reportedThreads[threadId.Value];
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

    private StackTrace CreateStackTrace(uint pid, ThreadId threadId, string threadName)
    {
        var resolver = _metadataContext.GetResolver(pid);
        var resolvedFrames = ImmutableArray.CreateBuilder<StackFrame>();
        var rawCallStack = _callstacks[threadId].CreateSnapshot().CallStack.Reverse();
        foreach (var (moduleId, methodToken) in rawCallStack)
        {
            var methodResolveResult = resolver.ResolveMethod(pid, moduleId, methodToken);
            var methodDef = methodResolveResult.Value;
            var methodName = methodDef?.FullName ?? "<unable-to-resolve-method>";
            var modulePath = methodDef?.Module?.Location ?? "<unable-to-resolve-module>";
            resolvedFrames.Add(new StackFrame(
                MethodName: methodName,
                SourceMapping: modulePath,
                MethodToken: methodToken.Value));
        }

        return new StackTrace(new ThreadInfo(threadId.Value, threadName), resolvedFrames.ToImmutableArray());
    }
}
