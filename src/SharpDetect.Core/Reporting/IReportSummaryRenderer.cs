// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Reporting;

public interface IReportSummaryRenderer
{
    string Render(Summary summary, DirectoryInfo additionalPartials);
}
