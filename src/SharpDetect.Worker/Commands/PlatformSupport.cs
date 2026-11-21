// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;
using OperationResult;
using static OperationResult.Helpers;

namespace SharpDetect.Worker.Commands;

public static partial class PlatformSupport
{
    public static readonly Version MinimumGlibcVersion = new(2, 34);

    private static readonly Lazy<Result<SupportedPlatform, string>> CurrentPlatform = new(DetectCurrentPlatform);

    public static Result<SupportedPlatform, string> CheckCurrentPlatform() => CurrentPlatform.Value;

    private static Result<SupportedPlatform, string> DetectCurrentPlatform()
    {
        var runtimeIdentifier = RuntimeInformation.RuntimeIdentifier;
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            return Error(GetUnsupportedMachineMessage(runtimeIdentifier));

        if (OperatingSystem.IsWindows())
            return Ok(SupportedPlatform.WindowsX64);

        if (OperatingSystem.IsLinux())
        {
            var linuxStatus = CheckLinux(GetGlibcVersion());
            return linuxStatus.IsError ? Error(linuxStatus.Error) : Ok(SupportedPlatform.LinuxX64);
        }

        return Error(GetUnsupportedMachineMessage(runtimeIdentifier));
    }

    public static Status<string> CheckLinux(Version? glibcVersion)
    {
        if (glibcVersion is null)
            return Error($"SharpDetect needs a Linux distribution with glibc {MinimumGlibcVersion} or newer.");

        if (glibcVersion < MinimumGlibcVersion)
            return Error($"SharpDetect needs glibc {MinimumGlibcVersion} or newer, but this machine has glibc {glibcVersion}.");

        return Ok();
    }

    private static string GetUnsupportedMachineMessage(string runtimeIdentifier)
        => $"SharpDetect runs on Windows x64 and Linux x64, but this machine is {runtimeIdentifier}.";

    private static Version? GetGlibcVersion()
    {
        try
        {
            return Version.TryParse(Marshal.PtrToStringUTF8(GnuGetLibcVersion()), out var version) ? version : null;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    [LibraryImport("libc", EntryPoint = "gnu_get_libc_version")]
    private static partial nint GnuGetLibcVersion();
}
