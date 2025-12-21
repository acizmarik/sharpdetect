// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events;

public enum RecordedEventType : ushort
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
    ThreadStart = 11,
    ThreadMapping = 12,
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
    GarbageCollectedTrackedObjects = 22,
    //Reserved = 23,

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
    ProfilerDestroy = 35,

    /* Stack trace snapshots */
    StackTraceSnapshot = 36,
    StackTraceSnapshots = 37,

    /* Synchronization */
    MonitorLockAcquire = 100,
    MonitorLockTryAcquire = 101,
    MonitorLockAcquireResult = 102,
    MonitorLockRelease = 103,
    MonitorLockReleaseResult = 104,
    MonitorPulseOneAttempt = 105,
    MonitorPulseOneResult = 106,
    MonitorPulseAllAttempt = 107,
    MonitorPulseAllResult = 108,
    MonitorWaitAttempt = 109,
    MonitorWaitResult = 110,
    ThreadJoinAttempt = 111,
    ThreadJoinResult = 112,
    LockAcquire = 113,
    LockTryAcquire = 114,
    LockAcquireResult = 115,
    LockRelease = 116,
    LockReleaseResult = 117,
}
