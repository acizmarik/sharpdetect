// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CommunityToolkit.Diagnostics;
using SharpDetect.Common.Exceptions;

namespace SharpDetect.Core.Runtime.Threads
{
    internal class OperationContext
    {
        private readonly Stack<ShadowObject?> fieldInstances;
        private readonly Stack<ShadowObject?> arrayInstances;
        private readonly Stack<int> arrayIndices;

        public OperationContext()
        {
            fieldInstances = new();
            arrayInstances = new();
            arrayIndices = new();
        }

        public ShadowObject? GetAndResetLastFieldInstance()
        {
            RuntimeContract.Assert(fieldInstances.Count != 0);
            return fieldInstances.Pop();
        }

        public void SetFieldInstance(ShadowObject? instance)
        {
            fieldInstances.Push(instance);
        }

        public ShadowObject? GetAndResetLastArrayInstance()
        {
            RuntimeContract.Assert(arrayInstances.Count != 0);
            return arrayInstances.Pop();
        }

        public void SetArrayInstance(ShadowObject? instance)
        {
            arrayInstances.Push(instance);
        }

        public int? GetAndResetLastArrayIndex()
        {
            RuntimeContract.Assert(arrayIndices.Count != 0);
            return arrayIndices.Pop();
        }

        public void SetArrayIndex(int index)
        {
            arrayIndices.Push(index);
        }
    }
}
