// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Runtime.Threads
{
    public enum ShadowThreadState
    {
        Unknown,
        Running,
        Suspended,
        GarbageCollecting
    }
}
