// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Runtime.Arguments
{
    public interface IValueOrPointer
    {
        bool HasValue();
        bool HasPointer();

        object? BoxedValue { get; }
        UIntPtr? Pointer { get; }
    }
}
