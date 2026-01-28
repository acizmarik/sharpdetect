// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting;

public interface IReportSummaryRenderer
{
    string Render(SummaryRenderingContext context);
}
