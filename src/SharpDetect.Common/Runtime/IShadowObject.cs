// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Runtime
{
    public interface IShadowObject : IEquatable<IShadowObject>
    {
        bool IsAlive { get; }
        UIntPtr ShadowPointer { get; }
        ISyncBlock SyncBlock { get; }
    }
}
