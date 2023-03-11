// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Runtime.Scheduling
{
    [Flags]
    internal enum JobFlags
    {
        None = 0,
        Concurrent = 1,
        OverrideEpoch = 2,
        OverrideSuspend = 4,
        Poison = 8
    }
}
