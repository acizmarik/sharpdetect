// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public readonly struct MdToken
{
    public static readonly MdToken Nil = new(0);

    public readonly int Value;

    public MdToken(int value)
    {
        Value = value;
    }
}
