// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Plugins.DataRace;

public partial class DataRacePlugin
{
    public Summary CreateDiagnostics()
    {
        throw new NotImplementedException();
    }

    public IEnumerable<object> CreateReportDataContext(IEnumerable<Report> reports)
    {
        throw new NotImplementedException();
    }
}