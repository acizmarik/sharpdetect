// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[NativeObject]
public interface IClassFactory : IUnknown
{
    int CreateInstance(IntPtr outer, in Guid guid, out IntPtr instance);
    int LockServer(bool @lock);
}
