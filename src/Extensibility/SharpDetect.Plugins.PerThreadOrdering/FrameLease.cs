// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Plugins.PerThreadOrdering;

public readonly ref struct FrameLease
{
    public StackFrame Frame { get; }

    internal FrameLease(StackFrame frame)
    {
        Frame = frame;
    }

    public void Dispose()
    {
        Frame.Arguments?.Dispose();
    }
}
