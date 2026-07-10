// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Core.Plugins;

public readonly record struct LockAcquireAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct LockAcquireResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId, bool IsSuccess);
public readonly record struct LockReleaseArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct ObjectPulseOneArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct ObjectPulseAllArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct ObjectWaitAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct ObjectWaitResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId, bool IsSuccess);
public readonly record struct ThreadStartingArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId ThreadObjectId);
public readonly record struct ThreadStartArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId ThreadObjectId);
public readonly record struct ThreadJoinAttemptArgs(ProcessThreadId BlockedProcessThreadId, ProcessThreadId JoiningProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken);
public readonly record struct ThreadJoinResultArgs(ProcessThreadId BlockedProcessThreadId, ProcessThreadId JoinedProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, bool IsSuccess);
public readonly record struct StaticFieldReadArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, bool IsVolatile, CapturedStackTrace Stack);
public readonly record struct StaticFieldWriteArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, bool IsVolatile, CapturedStackTrace Stack);
public readonly record struct InstanceFieldReadArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, ProcessTrackedObjectId ObjectId, bool IsVolatile, CapturedStackTrace Stack);
public readonly record struct InstanceFieldWriteArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, ProcessTrackedObjectId ObjectId, bool IsVolatile, CapturedStackTrace Stack);
public readonly record struct TaskScheduleArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId TaskObjectId);
public readonly record struct TaskStartArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId TaskObjectId);
public readonly record struct TaskCompleteArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId TaskObjectId);
public readonly record struct TaskJoinFinishArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId TaskObjectId, bool IsSuccess);
public readonly record struct SemaphoreAcquireAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SemaphoreId);
public readonly record struct SemaphoreAcquireResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SemaphoreId, bool IsSuccess);
public readonly record struct SemaphoreReleaseArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SemaphoreId, int ReleaseCount);
public readonly record struct SemaphoreCreatedArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SemaphoreId, int InitialCount);
public readonly record struct SemaphoreWaitAsyncArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SemaphoreId, ProcessTrackedObjectId WaiterTaskId);
public readonly record struct EventWaitHandleCreatedArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId EventId, bool InitialState, bool IsAutoReset);
public readonly record struct EventWaitHandleSetArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId EventId, bool IsAutoReset);
public readonly record struct EventWaitHandleWaitResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId EventId, bool IsAutoReset, bool IsSuccess);
public readonly record struct EventWaitHandleResetArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId EventId);
