// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Reporting;
using SharpDetect.Reporting.Services;
using Xunit;

namespace SharpDetect.Reporting.Tests;

public class ReportWriterTests : IDisposable
{
    private readonly DirectoryInfo _directory = Directory.CreateTempSubdirectory("sharpdetect-reports-");

    private sealed class FixedTimeProvider(DateTimeOffset now, TimeZoneInfo localTimeZone) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now.ToUniversalTime();
        public override TimeZoneInfo LocalTimeZone => localTimeZone;
    }

    private sealed class FixedRenderer(string contents) : IReportSummaryRenderer
    {
        public string Render(SummaryRenderingContext context) => contents;
    }

    private static readonly TimeZoneInfo PlusTwoHours =
        TimeZoneInfo.CreateCustomTimeZone("UTC+2", TimeSpan.FromHours(2), "UTC+2", "UTC+2");

    private static readonly DateTimeOffset Now = new(2026, 11, 5, 11, 41, 54, 123, TimeSpan.Zero);

    private Task<string> Write(string contents, string? fileName = null)
    {
        var writer = new ReportWriter(new FixedTimeProvider(Now, PlusTwoHours));
        return writer.Write(fileName, _directory.FullName, context: null!, new FixedRenderer(contents), CancellationToken.None);
    }

    [Fact]
    public async Task DefaultName_UsesLocalTimeWithMilliseconds()
    {
        var path = await Write("report");

        Assert.Equal("SharpDetect_Report_20261105_134154_123.html", Path.GetFileName(path));
        Assert.Equal("report", await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task DefaultName_NeverReplacesAnExistingReport()
    {
        var first = await Write("first");
        var second = await Write("second");
        var third = await Write("third");

        Assert.Equal("SharpDetect_Report_20261105_134154_123_2.html", Path.GetFileName(second));
        Assert.Equal("SharpDetect_Report_20261105_134154_123_3.html", Path.GetFileName(third));
        Assert.Equal("first", await File.ReadAllTextAsync(first));
        Assert.Equal("second", await File.ReadAllTextAsync(second));
        Assert.Equal("third", await File.ReadAllTextAsync(third));
    }

    [Fact]
    public async Task ExplicitName_ReplacesAnExistingReport()
    {
        await Write("first", "report.html");
        var path = await Write("second", "report.html");

        Assert.Equal(Path.Combine(_directory.FullName, "report.html"), path);
        Assert.Equal("second", await File.ReadAllTextAsync(path));
    }

    public void Dispose()
    {
        _directory.Delete(recursive: true);
    }
}
