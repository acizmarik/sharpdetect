﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Instrumentation
{
    [Flags]
    public enum InstrumentationTarget
    {
        None,
        Method,
        Field
    }
}
