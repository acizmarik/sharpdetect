// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using SharpDetect.Benchmarks.Configuration;
using SharpDetect.Benchmarks.Handlers;

namespace SharpDetect.Benchmarks.Commands;

[Command(Description = "Measures a performance baseline for the current commit against provided workload")]
public sealed partial class BenchmarkCommand : ICommand
{
    [CommandOption("workload", Description = "Path to workload DLL")]
    public required string WorkloadPath { get; set; }

    [CommandOption("iterations", Description = "Workload iterations per thread")]
    private int Iterations { get; set; } = 125_000;

    [CommandOption("threads", Description = "Workload thread count")]
    private int Threads { get; set; } = 4;

    [CommandOption("warmup", Description = "Number of discarded warmup runs per measured configuration")]
    private int Warmup { get; set; } = 1;

    [CommandOption("runs", Description = "Number of repetitions of each measured configuration")]
    private int Runs { get; set; } = 5;

    [CommandOption("output", Description = "Directory for the baseline JSON (default: <repository root>/docs/benchmarks)")]
    private string? OutputDirectory { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var options = ValidateOptions(WorkloadPath, Iterations, Threads, Warmup, Runs, OutputDirectory);
        var handler = new BenchmarkCommandHandler(options);
        var cancellationToken = console.RegisterCancellationHandler();
        await handler.ExecuteAsync(console, cancellationToken);
    }

    private static BenchmarkOptions ValidateOptions(
        string workloadPath,
        int iterations,
        int threads,
        int warmup,
        int runs,
        string? outputDirectory)
    {
        var fullWorkloadPath = Path.GetFullPath(workloadPath);
        if (!File.Exists(fullWorkloadPath))
            throw new CommandException($"Workload not found: {fullWorkloadPath}.");

        RequirePositive(iterations, "--iterations");
        RequirePositive(threads, "--threads");
        RequireNonNegative(warmup, "--warmup");
        RequirePositive(runs, "--runs");

        return new BenchmarkOptions(fullWorkloadPath, iterations, threads, warmup, runs, outputDirectory);
    }

    private static void RequirePositive(int value, string name)
    {
        if (value <= 0)
            throw new CommandException($"{name} must be greater than zero, but was {value}.");
    }

    private static void RequireNonNegative(int value, string name)
    {
        if (value < 0)
            throw new CommandException($"{name} must not be negative, but was {value}.");
    }
}
