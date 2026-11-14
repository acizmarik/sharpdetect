// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting;

namespace SharpDetect.Reporting.Services;

internal sealed class ReportWriter(TimeProvider timeProvider) : IReportSummaryWriter
{
    private const int MaxNameCollisions = 50;

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
        
        // Ensure reports folder exists
        Directory.CreateDirectory(directory);
        
        var reportContents = renderer.Render(context);
        if (fileName is not null)
        {
            var fullPath = Path.Combine(directory, fileName);
            await File.WriteAllTextAsync(fullPath, reportContents, cancellationToken).ConfigureAwait(false);
            return fullPath;
        }

        // A generated name should not replace an existing report
        await using var stream = CreateNewReportFile(directory, out var reportPath);
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(reportContents.AsMemory(), cancellationToken).ConfigureAwait(false);
        return reportPath;
    }

    private FileStream CreateNewReportFile(string directory, out string reportPath)
    {
        var baseName = $"SharpDetect_Report_{timeProvider.GetLocalNow():yyyyMMdd_HHmmss_fff}";
        for (var attempt = 1; attempt <= MaxNameCollisions; attempt++)
        {
            var fileName = attempt == 1 ? $"{baseName}.html" : $"{baseName}_{attempt}.html";
            reportPath = Path.Combine(directory, fileName);
            try
            {
                return new FileStream(reportPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch (IOException) when (File.Exists(reportPath))
            {
                // Taken by another report
            }
        }

        throw new IOException($"Could not find a free report file name for \"{Path.Combine(directory, baseName)}.html\".");
    }
}
