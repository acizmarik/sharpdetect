// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Runtime.Versioning;
using SharpDetect.Benchmarks.Models;

namespace SharpDetect.Benchmarks.Measurements;

internal static class ProcessResourceSampler
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    public static async Task<RunResourceSamples> SampleUntilCancelledAsync(
        Func<long> getPid,
        CancellationToken cancellationToken)
    {
        Process? target = null;
        var lastTargetSample = default(ProcessResourceSample);
        var hostPeakRssMegabytes = 0.0;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                hostPeakRssMegabytes = Math.Max(hostPeakRssMegabytes, Environment.WorkingSet / (1024.0 * 1024.0));

                target ??= TryAttach(getPid());
                if (target is not null && TrySample(target, out var sample))
                    lastTargetSample = sample;

                try
                {
                    await Task.Delay(PollInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            if (target is not null && OperatingSystem.IsWindows() && TryReadCpuAfterExit(target, out var cpuSeconds))
                lastTargetSample = lastTargetSample with { CpuSeconds = cpuSeconds };

            return new RunResourceSamples(lastTargetSample, hostPeakRssMegabytes);
        }
        finally
        {
            target?.Dispose();
        }
    }

    private static Process? TryAttach(long pid)
    {
        if (pid <= 0)
            return null;

        try
        {
            var process = Process.GetProcessById((int)pid);
            if (OperatingSystem.IsWindows())
                _ = process.SafeHandle;
            return process;
        }
        catch (Exception)
        {
            // The process exited before we could attach
            return null;
        }
    }

    private static bool TrySample(Process process, out ProcessResourceSample sample)
    {
        sample = default;
        try
        {
            process.Refresh();
            if (process.HasExited)
                return false;

            double peakRssMegabytes;
            if (OperatingSystem.IsLinux())
            {
                if (!TryReadLinuxPeakRssMegabytes(process.Id, out peakRssMegabytes))
                    return false;
            }
            else
            {
                peakRssMegabytes = process.PeakWorkingSet64 / (1024.0 * 1024.0);
            }

            sample = new ProcessResourceSample(process.TotalProcessorTime.TotalSeconds, peakRssMegabytes);
            return true;
        }
        catch (Exception)
        {
            // Most likely, the process exited mid-sample
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryReadCpuAfterExit(Process process, out double cpuSeconds)
    {
        try
        {
            // GetProcessTimes stays valid on an exited process while the acquired handle is open
            cpuSeconds = process.TotalProcessorTime.TotalSeconds;
            return true;
        }
        catch (Exception)
        {
            cpuSeconds = 0;
            return false;
        }
    }

    [SupportedOSPlatform("linux")]
    private static bool TryReadLinuxPeakRssMegabytes(int pid, out double peakRssMegabytes)
    {
        // Format: "VmHWM:    123456 kB"
        // Note the line disappears once the process exited
        foreach (var line in File.ReadLines($"/proc/{pid}/status"))
        {
            if (!line.StartsWith("VmHWM:", StringComparison.Ordinal))
                continue;
            
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            peakRssMegabytes = ulong.Parse(parts[1]) / 1024.0;
            return true;
        }

        peakRssMegabytes = 0;
        return false;
    }
}
