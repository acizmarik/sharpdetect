// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public struct COR_DEBUG_IL_TO_NATIVE_MAP
{
    public uint ilOffset;
    public uint nativeStartOffset;
    public uint nativeEndOffset;
}