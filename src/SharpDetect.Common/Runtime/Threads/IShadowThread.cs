// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Runtime.Threads
{
    public interface IShadowThread
    {
        UIntPtr Id { get; }
        int VirtualId { get; }
        string DisplayName { get; }
        ShadowThreadState State { get; }
    }
}
