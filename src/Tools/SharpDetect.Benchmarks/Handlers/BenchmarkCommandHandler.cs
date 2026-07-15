// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using CliFx;
using CliFx.Infrastructure;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpDetect.Benchmarks.Configuration;
using SharpDetect.Benchmarks.Measurements;
using SharpDetect.Benchmarks.Models;
using SharpDetect.Benchmarks.Reporting;
using SharpDetect.Core.Plugins;
using SharpDetect.Plugins.DataRace.FastTrack;
using SharpDetect.Worker;
using SharpDetect.Worker.Commands.Run;
using SharpDetect.Worker.Configuration;
using SharpDetect.Worker.Services;

namespace SharpDetect.Benchmarks.Handlers;

internal sealed class BenchmarkCommandHandler(BenchmarkOptions options)
{
    public async Task ExecuteAsync(IConsole console, CancellationToken cancellationToken)
    {
        var git = await GitInfo.ResolveAsync();
        await console.Output.WriteLineAsync($"SharpDetect benchmark: " +
                                            $"commit={git.Label} " +
                                            $"configuration={BuildInfo.Configuration} " +
                                            $"iterations={options.Iterations} " +
                                            $"threads={options.Threads} " +
                                            $"warmup={options.Warmup} " +
                                            $"runs={options.Runs}");

        var bareRuns = await RepeatAsync(
            console,
            label: "bare",
            RunBareWorkloadAsync,
            static run =>
                $"wall={run.WallSeconds:F2}s " +
                $"cpu={run.Resources.CpuSeconds:F2}s",
            cancellationToken);

        using var listener = new WorkerMetricsListener();
        var instrumentedRuns = await RepeatAsync(
            console,
            label: "instrumented",
            token => RunInstrumentedWorkloadAsync(listener, token),
            static run =>
                $"wall={run.WallSeconds:F2}s " +
                $"targetWall={run.Metrics.TargetWallSeconds:F2}s " +
                $"events={run.Metrics.EventsReceived} " +
                $"drainTail={run.Metrics.DrainSeconds:F3}s " +
                $"targetCpu={run.TargetResources.CpuSeconds:F2}s",
            cancellationToken);

        var report = BaselineReportBuilder.Build(git, options, bareRuns, instrumentedRuns);
        var (baselinePath, latestPath) = await WriteReportAsync(report, git, cancellationToken);

        BaselineSummaryRenderer.Render(console, report);
        await console.Output.WriteLineAsync();
        await console.Output.WriteLineAsync($"Baseline written to: {baselinePath}");
        await console.Output.WriteLineAsync($"Latest baseline:     {latestPath}");
    }

    private async Task<List<TRun>> RepeatAsync<TRun>(
        IConsole console,
        string label,
        Func<CancellationToken, Task<TRun>> runAsync,
        Func<TRun, string> describe,
        CancellationToken cancellationToken)
    {
        for (var index = 0; index < options.Warmup; index++)
        {
            var warmupRun = await runAsync(cancellationToken);
            await console.Output.WriteLineAsync($"  {label} warmup {index + 1}/{options.Warmup}: {describe(warmupRun)} (discarded)");
        }

        var runs = new List<TRun>(options.Runs);
        for (var index = 0; index < options.Runs; index++)
        {
            var run = await runAsync(cancellationToken);
            runs.Add(run);
            await console.Output.WriteLineAsync($"  {label} run {index + 1}/{options.Runs}: {describe(run)}");
        }

        return runs;
    }

    private async Task<BareRunResult> RunBareWorkloadAsync(CancellationToken cancellationToken)
    {
        var cpu = ChildCpuAccounting.Begin();
        var startTimestamp = Stopwatch.GetTimestamp();
        var execution = Cli.Wrap("dotnet")
            .WithArguments([
                options.WorkloadPath,
                "--iterations", options.Iterations.ToString(),
                "--threads", options.Threads.ToString()])
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync(cancellationToken);

        var samples = await SampleWhileAsync(
            () => execution.ProcessId,
            () => execution.Task,
            cancellationToken);
        var wallSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;

        var result = await execution.Task;
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Bare workload run failed with exit code: {result.ExitCode}. Stderr: {result.StandardError}");
        }

        var run = new BareRunResult(wallSeconds, cpu.Complete(samples.Target));
        EnsureSane("bare", RunValidation.Validate(run));
        return run;
    }

    private async Task<AnalyzedRunResult> RunInstrumentedWorkloadAsync(
        WorkerMetricsListener listener,
        CancellationToken cancellationToken)
    {
        var metricsBefore = listener.Poll();
        var gcBefore = GcSnapshot.Capture();
        var analysis = await ExecuteAnalysisAsync(cancellationToken);
        var gc = GcSnapshot.Capture().Since(gcBefore);

        var run = new AnalyzedRunResult(
            analysis.WallSeconds,
            MetricsSnapshot.Delta(metricsBefore, listener.Poll()),
            gc.AllocatedMegabytes,
            gc.Gen0,
            gc.Gen1,
            gc.Gen2,
            analysis.TargetResources,
            analysis.HostPeakRssMB,
            analysis.ReportedIssues);
        EnsureSane("instrumented", RunValidation.Validate(run));
        return run;
    }

    private async Task<AnalysisExecution> ExecuteAnalysisAsync(CancellationToken cancellationToken)
    {
        var provider = BuildAnalysisServiceProvider();
        try
        {
            var worker = provider.GetRequiredService<IAnalysisWorker>();
            var cpu = ChildCpuAccounting.Begin();
            var startTimestamp = Stopwatch.GetTimestamp();
            var samples = await SampleWhileAsync(
                static () => AnalysisWorkerMetrics.CurrentTargetPid,
                () => worker.ExecuteAsync(cancellationToken).AsTask(),
                cancellationToken);
            var wallSeconds = Stopwatch.GetElapsedTime(startTimestamp).TotalSeconds;

            var plugin = provider.GetRequiredService<IPlugin>();
            return new AnalysisExecution(
                wallSeconds,
                cpu.Complete(samples.Target),
                samples.HostPeakRssMegabytes,
                ReportedIssues: plugin.CreateDiagnostics().GetAllReports().Count());
        }
        finally
        {
            (provider as IDisposable)?.Dispose();
        }
    }

    private static async Task<RunResourceSamples> SampleWhileAsync(
        Func<long> getPid,
        Func<Task> workAsync,
        CancellationToken cancellationToken)
    {
        using var samplerCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var samplerTask = ProcessResourceSampler.SampleUntilCancelledAsync(getPid, samplerCancellation.Token);
        try
        {
            await workAsync();
        }
        finally
        {
            await samplerCancellation.CancelAsync();
        }

        return await samplerTask;
    }

    private static void EnsureSane(string label, IReadOnlyList<string> violations)
    {
        if (violations.Count == 0)
            return;

        var report = string.Join(Environment.NewLine, violations.Select(static v => $"  - {v}"));
        throw new CommandException(
            $"Sanity check failed for the {label} run — refusing to write a baseline:{Environment.NewLine}{report}");
    }

    private async Task<(string BaselinePath, string LatestPath)> WriteReportAsync(
        BaselineReport report,
        GitInfo git,
        CancellationToken cancellationToken)
    {
        var outputDirectory = Path.GetFullPath(options.OutputDirectory ??
            Path.Combine(git.RepositoryRoot ?? Environment.CurrentDirectory, "docs", "benchmarks"));
        Directory.CreateDirectory(outputDirectory);

        var baselinePath = Path.Combine(outputDirectory, $"{git.Label}.json");
        var latestPath = Path.Combine(outputDirectory, "latest.json");
        var json = report.Serialize();
        await File.WriteAllTextAsync(baselinePath, json, cancellationToken);
        await File.WriteAllTextAsync(latestPath, json, cancellationToken);

        return (baselinePath, latestPath);
    }

    private IServiceProvider BuildAnalysisServiceProvider()
    {
        var arguments = new RunCommandArgs(
            Runtime: null,
            Target: new TargetConfigurationArgs(
                path: options.WorkloadPath,
                args: $"--iterations {options.Iterations} --threads {options.Threads}"),
            Analysis: new AnalysisPluginConfigurationArgs(
                pluginName: "FastTrack",
                renderReport: false,
                logLevel: LogLevel.Warning));

        return new AnalysisServiceProviderBuilder(arguments)
            .WithTimeProvider(TimeProvider.System)
            .WithPlugin(typeof(FastTrackPlugin))
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            })
            .Build();
    }

    private sealed record AnalysisExecution(
        double WallSeconds,
        ProcessResourceSample TargetResources,
        double HostPeakRssMB,
        int ReportedIssues);
}
