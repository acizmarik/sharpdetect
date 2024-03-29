﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Profiler;

[Flags]
public enum COR_PRF_MONITOR : uint
{
    COR_PRF_MONITOR_NONE = 0x00000000,
    COR_PRF_MONITOR_FUNCTION_UNLOADS = 0x00000001,
    COR_PRF_MONITOR_CLASS_LOADS = 0x00000002,
    COR_PRF_MONITOR_MODULE_LOADS = 0x00000004,
    COR_PRF_MONITOR_ASSEMBLY_LOADS = 0x00000008,
    COR_PRF_MONITOR_APPDOMAIN_LOADS = 0x00000010,
    COR_PRF_MONITOR_JIT_COMPILATION = 0x00000020,
    COR_PRF_MONITOR_EXCEPTIONS = 0x00000040,
    COR_PRF_MONITOR_GC = 0x00000080,
    COR_PRF_MONITOR_OBJECT_ALLOCATED = 0x00000100,
    COR_PRF_MONITOR_THREADS = 0x00000200,
    COR_PRF_MONITOR_REMOTING = 0x00000400,
    COR_PRF_MONITOR_CODE_TRANSITIONS = 0x00000800,
    COR_PRF_MONITOR_ENTERLEAVE = 0x00001000,
    COR_PRF_MONITOR_CCW = 0x00002000,
    COR_PRF_MONITOR_REMOTING_COOKIE = 0x00004000 |
                                          COR_PRF_MONITOR_REMOTING,
    COR_PRF_MONITOR_REMOTING_ASYNC = 0x00008000 |
                                          COR_PRF_MONITOR_REMOTING,
    COR_PRF_MONITOR_SUSPENDS = 0x00010000,
    COR_PRF_MONITOR_CACHE_SEARCHES = 0x00020000,
    COR_PRF_ENABLE_REJIT = 0x00040000,
    COR_PRF_ENABLE_INPROC_DEBUGGING = 0x00080000,
    COR_PRF_ENABLE_JIT_MAPS = 0x00100000,
    COR_PRF_DISABLE_INLINING = 0x00200000,
    COR_PRF_DISABLE_OPTIMIZATIONS = 0x00400000,
    COR_PRF_ENABLE_OBJECT_ALLOCATED = 0x00800000,
    COR_PRF_MONITOR_CLR_EXCEPTIONS = 0x01000000,
    COR_PRF_MONITOR_ALL = 0x0107FFFF,
    COR_PRF_ENABLE_FUNCTION_ARGS = 0X02000000,
    COR_PRF_ENABLE_FUNCTION_RETVAL = 0X04000000,
    COR_PRF_ENABLE_FRAME_INFO = 0X08000000,
    COR_PRF_ENABLE_STACK_SNAPSHOT = 0X10000000,
    COR_PRF_USE_PROFILE_IMAGES = 0x20000000,
    COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST
                                        = 0x40000000,
    COR_PRF_DISABLE_ALL_NGEN_IMAGES = 0x80000000,
    COR_PRF_ALL = 0x8FFFFFFF,
    COR_PRF_REQUIRE_PROFILE_IMAGE = COR_PRF_USE_PROFILE_IMAGES |
                                          COR_PRF_MONITOR_CODE_TRANSITIONS |
                                          COR_PRF_MONITOR_ENTERLEAVE,
    COR_PRF_ALLOWABLE_AFTER_ATTACH = COR_PRF_MONITOR_THREADS |
                                          COR_PRF_MONITOR_MODULE_LOADS |
                                          COR_PRF_MONITOR_ASSEMBLY_LOADS |
                                          COR_PRF_MONITOR_APPDOMAIN_LOADS |
                                          COR_PRF_ENABLE_STACK_SNAPSHOT |
                                          COR_PRF_MONITOR_GC |
                                          COR_PRF_MONITOR_SUSPENDS |
                                          COR_PRF_MONITOR_CLASS_LOADS |
                                          COR_PRF_MONITOR_JIT_COMPILATION,
    COR_PRF_MONITOR_IMMUTABLE = COR_PRF_MONITOR_CODE_TRANSITIONS |
                                          COR_PRF_MONITOR_REMOTING |
                                          COR_PRF_MONITOR_REMOTING_COOKIE |
                                          COR_PRF_MONITOR_REMOTING_ASYNC |
                                          COR_PRF_ENABLE_REJIT |
                                          COR_PRF_ENABLE_INPROC_DEBUGGING |
                                          COR_PRF_ENABLE_JIT_MAPS |
                                          COR_PRF_DISABLE_OPTIMIZATIONS |
                                          COR_PRF_DISABLE_INLINING |
                                          COR_PRF_ENABLE_OBJECT_ALLOCATED |
                                          COR_PRF_ENABLE_FUNCTION_ARGS |
                                          COR_PRF_ENABLE_FUNCTION_RETVAL |
                                          COR_PRF_ENABLE_FRAME_INFO |
                                          COR_PRF_USE_PROFILE_IMAGES |
                     COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST |
                                          COR_PRF_DISABLE_ALL_NGEN_IMAGES
}
