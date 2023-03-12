// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Profiler;

namespace SharpDetect.Common.Runtime
{
    public interface IShadowCLR
    {
        int ProcessId { get; }
        ShadowRuntimeState State { get; }
        COR_PRF_SUSPEND_REASON? SuspensionReason { get; }
    }
}
