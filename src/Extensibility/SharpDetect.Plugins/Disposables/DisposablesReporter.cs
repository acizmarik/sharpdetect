// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging;
using SharpDetect.Core.Plugins;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.Disposables;

public partial class DisposablesPlugin
{
    public Summary CreateDiagnostics()
    {
        if (_notDisposed.Count != 0)
            PrepareViolationDiagnostics();
        else
            PrepareNoViolationDiagnostics();

        return Reporter.Build();
    }

    private void PrepareNoViolationDiagnostics()
    {
        Reporter.SetTitle("No violations found");
        Reporter.SetDescription("All analyzed disposables were correctly disposed.");
    }

    public void PrepareViolationDiagnostics()
    {
        Reporter.SetTitle(_notDisposed.Count == 1 ? "One violation found" : $"Several ({_notDisposed.Count}) violations found");
        Reporter.SetDescription("See details below for more information.");

        if (_notDisposed.Count > 0)
        {
            Logger.LogWarning("[PID={Pid}] {Count} not disposed object(s) detected.",
                _allocationInfos.First().Value.Pid,
                _allocationInfos.Count);
        }

        var index = 0;
        foreach (var allocation in _notDisposed)
        {
            var info = _allocationInfos[allocation];

            var builder = new ReportBuilder(index++, ReportCategory);
            var threadInfo = new ThreadInfo(info.ThreadInfo.Id, info.ThreadInfo.Name);
            builder.SetTitle(info.ThreadInfo.Name);
            builder.SetDescription($"Leaked {info.MethodDef.DeclaringType.FullName} instance.");
            builder.AddThread(threadInfo);
            var stackTrace = _callstackResolver.Resolve(threadInfo, info.Callstack);
            builder.AddStackTrace(stackTrace);
            builder.AddReportReason(threadInfo, "Allocated leaked object");
            Reporter.AddReport(builder.Build());
        }
    }

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports)
    {
        foreach (var threadReports in reports.GroupBy(r => r.GetReportedThreads().Single()))
        {
            var threadInfo = threadReports.Key;
            yield return new
            {
                thread = threadInfo.Name,
                stacktraces = threadReports.Select(r =>
                {
                    r.TryGetStackTrace(threadInfo, out var stacktrace);
                    r.TryGetReportReason(threadInfo, out var reason);
                    return new
                    {
                        identifier = r.Identifier,
                        reason,
                        description = r.Description,
                        stacktrace = stacktrace!.Frames.Select(frame =>
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
            };
        }
    }
}
