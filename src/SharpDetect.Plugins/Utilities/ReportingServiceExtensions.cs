﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Diagnostics;
using SharpDetect.Common.Services.Reporting;

namespace SharpDetect.Plugins.Utilities
{
    public static class ReportingServiceExtensions
    {
        public static void CreateReport(
            this IReportingService reporter, 
            string plugin, 
            string messageFormat,
            object[]? arguments,
            string category, 
            ReportDataEntry[]? entries)
        {
            var report = new WarningReport(plugin, category, messageFormat, arguments, entries);
            reporter.Report(report);
        }
    }
}
