// Copyright 2026 Andrej Čižmárik and Contributors
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
    ThreadStartCore = 11,
    ThreadStartCallback = 12,
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

    /* Profiler lifecycle */
    ProfilerLoad = 31,
    ProfilerInitialize = 32,
    ProfilerDestroy = 33,
    ProfilerAbortInitialize = 34,
    //Reserved = 35,

    /* Stack trace snapshots */
    StackTraceSnapshot = 36,
    StackTraceSnapshots = 37,
    //Reserved = 38,
    //Reserved = 39,

    /* Instrumentation */
    FieldAccessInstrumentation = 40,
    StaticFieldRead = 41,
    StaticFieldWrite = 42,
    InstanceFieldRead = 43,
    InstanceFieldWrite = 44,

    /* Exceptions */
    MethodUnwound = 90,

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
    SemaphoreAcquire = 118,
    SemaphoreTryAcquire = 119,
    SemaphoreAcquireResult = 120,
    SemaphoreRelease = 121,
    SemaphoreReleaseResult = 122,
    SemaphoreCreate = 123,
    SemaphoreWaitAsync = 124,
    SemaphoreWaitAsyncResult = 125,
    WaitHandleWait = 126,
    WaitHandleWaitResult = 127,
    MutexCreate = 128,
    MutexRelease = 129,
    MutexReleaseResult = 130,
    EventWaitHandleCreate = 131,
    AutoResetEventCreate = 132,
    ManualResetEventCreate = 133,
    EventWaitHandleSet = 134,
    EventWaitHandleSetResult = 135,
    WaitHandleSignalAndWait = 136,
    WaitHandleSignalAndWaitResult = 137,
    WaitHandleWaitMultiple = 138,
    WaitHandleWaitMultipleResult = 139,
    AbandonedMutexExceptionCreate = 140,
    EventWaitHandleReset = 141,
    EventWaitHandleResetResult = 142,

    /* Task synchronization */
    TaskSchedule = 200,
    TaskStart = 201,
    TaskComplete = 202,
    TaskJoinStart = 203,
    TaskJoinFinish = 204,

    /* Value publication */
    ValuePublicationStore = 220,
    ValuePublicationLoad = 221,
    ValuePublicationStoreLoad = 222,
    ValuePublicationLoadByRef = 223,
}
