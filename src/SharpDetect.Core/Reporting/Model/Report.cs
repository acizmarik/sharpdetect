// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace SharpDetect.Core.Reporting.Model;

public class Report
{
    public readonly int Identifier;
    public readonly string Title;
    public readonly string Category;
    public readonly string Description;
    public readonly DateTime DetectionTime;
    private readonly ImmutableArray<ThreadInfo> _reportedThreads;
    private readonly ImmutableDictionary<ThreadInfo, string> _reportReasons;
    private readonly ImmutableDictionary<ThreadInfo, StackTrace> _stackTraces;

    public Report(
        int identifier,
        string category,
        string title,
        string description,
        IEnumerable<ThreadInfo> reportedThreads,
        IEnumerable<KeyValuePair<ThreadInfo, StackTrace>> stackTraces,
        IEnumerable<KeyValuePair<ThreadInfo, string>> reportReasons,
        DateTime detectionTime)
    {
        Identifier = identifier;
        Category = category;
        Title = title;
        Description = description;
        DetectionTime = detectionTime;
        _reportedThreads = reportedThreads.ToImmutableArray();
        _stackTraces = stackTraces.ToImmutableDictionary();
        _reportReasons = reportReasons.ToImmutableDictionary();
    }

    public IEnumerable<ThreadInfo> GetReportedThreads()
        => _reportedThreads;

    public bool TryGetStackTrace(ThreadInfo threadInfo, out StackTrace? stackTrace)
        => _stackTraces.TryGetValue(threadInfo, out stackTrace);

    public bool TryGetReportReason(ThreadInfo threadInfo, out string? reason)
        => _reportReasons.TryGetValue(threadInfo, out reason);
}
