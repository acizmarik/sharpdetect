// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[NativeObject]
public interface IUnknown
{
    int QueryInterface(in Guid guid, out IntPtr ptr);
    int AddRef();
    int Release();
}
