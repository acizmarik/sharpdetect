// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public readonly struct FunctionId
{
    public readonly nuint Value;

    public FunctionId(nuint value)
    {
        Value = value;
    }
}
