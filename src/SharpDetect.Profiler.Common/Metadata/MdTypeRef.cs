// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public readonly struct MdTypeRef
{
    public readonly int Value;

    public MdTypeRef(int value)
    {
        Value = value;
    }
}
