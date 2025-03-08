// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting.Model;

namespace SharpDetect.Core.Reporting;

public interface IReportSummaryDisplayer
{
    void Display(Summary summary, DirectoryInfo additionalPartials);
}
