// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Runtime.Arguments
{
    public interface IValueOrObject
    {
        bool HasValue();
        bool HasShadowObject();

        object? BoxedValue { get; }
        IShadowObject? ShadowObject { get; }
    }
}
