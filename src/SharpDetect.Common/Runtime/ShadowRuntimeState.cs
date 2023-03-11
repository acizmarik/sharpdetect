﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Runtime
{
    public enum ShadowRuntimeState
    {
        Initiated,
        Executing,
        Suspending,
        Suspended,
        Resuming,
        Terminated
    }
}
