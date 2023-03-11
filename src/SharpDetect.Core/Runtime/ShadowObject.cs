// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Runtime;

namespace SharpDetect.Core.Runtime
{
    internal sealed class ShadowObject : IShadowObject
    {
        public SyncBlock SyncBlock { get; internal set; }
        public bool IsAlive { get; internal set; }
        public UIntPtr ShadowPointer { get; internal set; }

        ISyncBlock IShadowObject.SyncBlock => SyncBlock;

        public ShadowObject()
        {
            IsAlive = true;
            SyncBlock = new SyncBlock();
        }

        public bool Equals(IShadowObject? other)
        {
            if (other == null || !GetType().Equals(other.GetType()))
                return false;

            return ShadowPointer == other.ShadowPointer;
        }

        public override int GetHashCode()
        {
            return ShadowPointer.GetHashCode();
        }

        public override string ToString()
        {
            return $"{{ShadowObj -> {ShadowPointer}}}";
        }
    }
}
