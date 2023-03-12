// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public readonly struct CorILMap
{
    public readonly uint OldOffset;
    public readonly uint NewOffset;
    public readonly bool fAccurate;
}
