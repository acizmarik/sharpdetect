// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

public enum COR_PRF_GC_ROOT_KIND
{
    COR_PRF_GC_ROOT_STACK = 1,
    COR_PRF_GC_ROOT_FINALIZER = 2,
    COR_PRF_GC_ROOT_HANDLE = 3,
    COR_PRF_GC_ROOT_OTHER = 0
}
