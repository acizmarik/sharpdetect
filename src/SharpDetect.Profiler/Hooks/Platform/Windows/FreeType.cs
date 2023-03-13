// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler.Hooks.Platform.Windows;

[Flags]
public enum FreeType
{
    DECOMMIT = 0x4000,
    RELEASE = 0x8000
}
