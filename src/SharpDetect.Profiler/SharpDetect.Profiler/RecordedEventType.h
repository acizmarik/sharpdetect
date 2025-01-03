// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"

namespace Profiler
{
    enum class RecordedEventType : USHORT
    {
        NotSpecified = 0,

        /* Generic method enter/exit */
        MethodEnter = 1,
        MethodExit = 2,
        Tailcall = 3,
        MethodEnterWithArguments = 4,
        MethodExitWithArguments = 5,
        TailcallWithArguments = 6,

        /* Threading */
        ThreadCreate = 13,
        ThreadRename = 14,
        ThreadDestroy = 15,

        /* Metadata loads, JIT */
        AssemblyLoad = 16,
        ModuleLoad = 17,
        TypeLoad = 18,
        JITCompilation = 19,

        /* Garbage collection */
        GarbageCollectionStart = 20,
        GarbageCollectionFinish = 21,
        GarbageCollectionSurvivors = 22,
        GarbageCollectionCompaction = 23,

        /* Metadata modifications */
        AssemblyReferenceInjection = 24,
        TypeDefinitionInjection = 25,
        TypeReferenceInjection = 26,
        MethodDefinitionInjection = 27,
        MethodWrapperInjection = 28,
        MethodReferenceInjection = 29,
        MethodBodyRewrite = 30,

        /* Objects tracking */
        ObjectTracking = 31,
        ObjectRemoved = 32,

        /* Profiler lifecycle */
        ProfilerLoad = 33,
        ProfilerInitialize = 34,
        ProfilerDestroy = 35
    };
}