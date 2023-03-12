// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ASSEMBLYMETADATA
{
    // USHORT  usMajorVersion;
    public readonly ushort usMajorVersion;

    // USHORT  usMinorVersion;
    public readonly ushort usMinorVersion;

    // USHORT  usBuildNumber;
    public readonly ushort usBuildNumber;

    // USHORT  usRevisionNumber;
    public readonly ushort usRevisionNumber;

    // LPWSTR  szLocale;
    public readonly IntPtr szLocale;

    // ULONG   cbLocale;
    public readonly uint cbLocale;

    // DWORD*  rdwProcessor[];
    public readonly IntPtr rdwProcessor;

    // ULONG   ulProcessor
    public readonly int ulProcessor;

    // OSINFO* rOS[];
    public readonly IntPtr rOS;

    // ULONG   ulOS;
    public readonly uint ulOS;
}
