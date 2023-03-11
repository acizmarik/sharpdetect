// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Runtime.Threads;

namespace SharpDetect.Common.Runtime
{
    public interface ISyncBlock
    {
        UIntPtr? LockOwnerId { get; }
    }
}
