// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler.Hooks.Platform.Linux
{
    [Flags]
    public enum MmapProtectionType : uint
    {
        PROT_NONE = 0x0,
        PROT_READ = 0x1,
        PROT_WRITE = 0x2,
        PROT_EXEC = 0x4,
        PROT_GROWSDOWN = 0x01000000,
        PROT_GROWSUP = 0x02000000
    }
}
