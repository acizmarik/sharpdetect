// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public readonly struct MdMethodDef
{
    public static readonly MdMethodDef Nil = new(0x06000000);

    public readonly int Value;

    public MdMethodDef(int value)
    {
        Value = value;
    }
}
