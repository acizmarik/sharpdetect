// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliWrap;
using CliWrap.Buffered;

namespace SharpDetect.Benchmarks.Models;

internal sealed record GitInfo(
    string Commit,
    string ShortCommit,
    string CommitDate,
    string Branch,
    bool Dirty,
    string? RepositoryRoot)
{
    public string Label => Dirty ? $"{ShortCommit}-dirty" : ShortCommit;

    public static async Task<GitInfo> ResolveAsync()
    {
        try
        {
            var commit = await RunGitAsync("rev-parse HEAD") ?? "unknown";
            var shortCommit = await RunGitAsync("rev-parse --short HEAD") ?? "unknown";
            var commitDate = await RunGitAsync("log -1 --format=%cI") ?? "unknown";
            var branch = await RunGitAsync("rev-parse --abbrev-ref HEAD") ?? "unknown";
            var dirty = !string.IsNullOrWhiteSpace(await RunGitAsync("status --porcelain -uno"));
            var repositoryRoot = await RunGitAsync("rev-parse --show-toplevel");
            return new GitInfo(commit, shortCommit, commitDate, branch, dirty, repositoryRoot);
        }
        catch (Exception)
        {
            return new GitInfo("unknown", "unknown", "unknown", "unknown", Dirty: false, RepositoryRoot: null);
        }
    }

    private static async Task<string?> RunGitAsync(string arguments)
    {
        var result = await Cli.Wrap("git")
            .WithArguments(arguments)
            .WithWorkingDirectory(AppContext.BaseDirectory)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();

        return result.ExitCode == 0 ? result.StandardOutput.Trim() : null;
    }
}
