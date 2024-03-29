﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public readonly struct COR_PRF_EX_CLAUSE_INFO
{
    public readonly COR_PRF_CLAUSE_TYPE ClauseType;
    public readonly nuint ProgramCounter;
    public readonly nuint FramePointer;
    public readonly nuint ShadowStackPointer;
}
