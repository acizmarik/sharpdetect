// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using CommunityToolkit.Diagnostics;
using SharpDetect.Reporting.Model;

namespace SharpDetect.Reporting
{
    public class ReportBuilder
    {
        public readonly string Category;
        public readonly int Identifier;
        private readonly List<ThreadInfo> _threads = [];
        private readonly Dictionary<ThreadInfo, StackTrace> _stackTraces = [];
        private readonly Dictionary<ThreadInfo, string> _reportReasons = [];
        private string? _title;
        private string? _description;

        public ReportBuilder(int identifier, string category)
        {
            Identifier = identifier;
            Category = category;
        }

        public ReportBuilder AddThread(ThreadInfo threadInfo)
        {
            _threads.Add(threadInfo);
            return this;
        }

        public ReportBuilder AddStackTrace(StackTrace stacktrace)
        {
            _stackTraces.Add(stacktrace.ThreadInfo, stacktrace);
            return this;
        }

        public ReportBuilder AddReportReason(ThreadInfo threadInfo, string reason)
        {
            _reportReasons.Add(threadInfo, reason);
            return this;
        }

        public ReportBuilder SetTitle(string title)
        {
            _title = title;
            return this;
        }

        public ReportBuilder SetDescription(string description)
        {
            _description = description;
            return this;
        }

       public Report Build()
        {
            Guard.IsNotNullOrWhiteSpace(_title);
            Guard.IsNotNullOrWhiteSpace(_description);

            return new Report(
                identifier: Identifier,
                category: Category,
                title: _title,
                description: _description,
                reportedThreads: _threads,
                stackTraces: _stackTraces,
                reportReasons: _reportReasons);
        }
    }
}
