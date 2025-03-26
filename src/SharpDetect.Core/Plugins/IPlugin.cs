// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Plugins;

public interface IPlugin
{
    string ReportCategory { get; }
    DirectoryInfo ReportTemplates { get; }
    PluginConfiguration Configuration { get; }
    RecordedEventActionVisitorBase EventsVisitor { get; }

    Summary CreateDiagnostics();
    IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports);
}
