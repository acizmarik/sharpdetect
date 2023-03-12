// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public readonly struct MdMemberRef
{
    public readonly int Value;

    public MdMemberRef(int value)
    {
        Value = value;
    }
}
