// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler.Hooks.Platform.Linux
{
    [Flags]
    public enum MmapFlags : uint
    {
        MAP_FILE = 0,
        MAP_SHARED = 0x01,
        MAP_PRIVATE = 0x02,
        MAP_TYPE = 0x0f,
        MAP_FIXED = 0x10,
        MAP_ANONYMOUS = 0x20
    }
}
