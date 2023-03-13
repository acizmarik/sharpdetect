// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.Profiler.Hooks.Platform.Linux
{
    internal static partial class LinuxNativeFunctions
    {
        private const string Library = "libc.so";

        [LibraryImport(Library)]
        public static partial IntPtr mmap(
            IntPtr addr,
            UIntPtr length,
            MmapProtectionType prot,
            MmapFlags flags,
            int fd,
            UIntPtr offset);

        [LibraryImport(Library)]
        public static partial int munmap(
            IntPtr addr,
            UIntPtr length);
    }
}
