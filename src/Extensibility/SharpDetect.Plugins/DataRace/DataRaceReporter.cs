// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace;

public partial class DataRacePlugin
{
    public Summary CreateDiagnostics()
    {
        // FIXME: Implement this properly
        Reporter.SetTitle("No data races detected");
        Reporter.SetDescription("Static field access instrumentation is active. Full race detection to be implemented.");
        return Reporter.Build();
    }

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports)
    {
        // FIXME: Implement this properly
        yield break;
    }
}