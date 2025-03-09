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
            builder.SetTitle($"#{index}");
            builder.SetDescription($"Leaked instance of {info.MethodDef.DeclaringType.FullName}.");
            builder.AddThread(threadInfo);
            builder.AddStackTrace(new StackTrace(threadInfo, [new StackFrame(
                info.MethodDef.FullName,
                info.MethodDef.Module.Location,
                (int)info.MethodDef.MDToken.Raw)]));
            builder.AddReportReason(threadInfo, "Allocated leaked object");
            Reporter.AddReport(builder.Build());
        }
    }
}
