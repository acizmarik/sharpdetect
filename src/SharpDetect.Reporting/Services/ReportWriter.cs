// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting;

namespace SharpDetect.Reporting.Services;

internal sealed class ReportWriter(TimeProvider timeProvider) : IReportSummaryWriter
{
    public Task<string> Write(
        SummaryRenderingContext context,
        IReportSummaryRenderer renderer,
        CancellationToken cancellationToken)
    {
        return Write(fileName: null, directory: null, context, renderer, cancellationToken);
    }

    public async Task<string> Write(
        string? fileName,
        string? directory,
        SummaryRenderingContext context,
        IReportSummaryRenderer renderer,
        CancellationToken cancellationToken)
    {
        directory ??= Directory.GetCurrentDirectory();
        fileName ??= $"SharpDetect_Report_{timeProvider.GetUtcNow().DateTime:yyyyMMdd_HHmmss}.html";
        
        // Ensure reports folder exists
        Directory.CreateDirectory(directory);
        
        var reportContents = renderer.Render(context);
        var fullPath = Path.Combine(directory, fileName);
        await File.WriteAllTextAsync(fullPath, reportContents, cancellationToken).ConfigureAwait(false);
        return fullPath;
    }
}