﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

[StructLayout(LayoutKind.Sequential, Pack = 8)]
public readonly struct COR_PRF_GC_GENERATION_RANGE
{
    public readonly COR_PRF_GC_GENERATION Generation;
    public readonly ObjectId RangeStart;
    public readonly nuint RangeLength;
    public readonly nuint RangeLengthReserved;

    public COR_PRF_GC_GENERATION_RANGE(COR_PRF_GC_GENERATION generation, ObjectId rangeStart, nuint rangeLength, nuint rangeLengthReserved)
    {
        Generation = generation;
        RangeStart = rangeStart;
        RangeLength = rangeLength;
        RangeLengthReserved = rangeLengthReserved;
    }
}
