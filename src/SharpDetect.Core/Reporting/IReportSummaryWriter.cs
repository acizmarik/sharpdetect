// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Reporting;

public interface IReportSummaryWriter
{
    Task<string> Write(
        SummaryRenderingContext context,
        IReportSummaryRenderer renderer,
        CancellationToken cancellationToken);
    
    Task<string> Write(
        string? fileName,
        string? directory,
        SummaryRenderingContext context,
        IReportSummaryRenderer renderer,
        CancellationToken cancellationToken);
}