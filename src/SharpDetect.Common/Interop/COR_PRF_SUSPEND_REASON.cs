// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Interop
{
    public enum COR_PRF_SUSPEND_REASON
    {
        Other = 0,
        GC = 1,
        AppDomainShutdown = 2,
        CodePitching = 3,
        Shutdown = 4,
        /* Unused */
        InprocDebugger = 6,
        GCPreparation = 7,
        ReJIT = 8
    }
}