// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.InteropServices;

namespace SharpDetect.Profiler;

[NativeObject]
internal unsafe interface IMethodAlloc : IUnknown
{
    public IntPtr Alloc(
        [In] ulong cb);
}
