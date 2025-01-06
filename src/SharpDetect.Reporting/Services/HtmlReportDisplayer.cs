// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using Microsoft.Extensions.Logging;
using SharpDetect.Reporting.Model;
using System.Diagnostics;

namespace SharpDetect.Reporting.Services;

internal sealed class HtmlReportDisplayer : IReportSummaryDisplayer
{
    private readonly IReportSummaryRenderer _reportSummaryRenderer;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger _logger;

    public HtmlReportDisplayer(
        IReportSummaryRenderer reportSummaryRenderer,
        TimeProvider timeProvider,
        ILogger<HtmlReportDisplayer> logger)
    {
        _reportSummaryRenderer = reportSummaryRenderer;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public void Display(Summary summary)
    {
        var html = _reportSummaryRenderer.Render(summary);
        _logger.LogTrace("Rendered summary to HTML. Document length: {Length}.", html.Length);

        var timestamp = _timeProvider.GetUtcNow().UtcDateTime.ToString("s").Replace(":", ".");
        var tmpDirectory = Path.GetTempPath();
        var tmpFileName = Path.Combine(tmpDirectory, $"SharpDetectReport_{timestamp}.html");
        File.WriteAllText(tmpFileName, html);
        _logger.LogInformation("Saved report into file: {File}.", tmpFileName);

        using var process = new Process();
        process.StartInfo.UseShellExecute = true;
        process.StartInfo.FileName = tmpFileName;
        process.Start();
    }
}
