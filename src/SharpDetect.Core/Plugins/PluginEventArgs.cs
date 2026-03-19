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
public readonly record struct StaticFieldReadArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, uint MethodOffset, MdToken FieldToken, bool IsVolatile);
public readonly record struct StaticFieldWriteArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, uint MethodOffset, MdToken FieldToken, bool IsVolatile);
public readonly record struct InstanceFieldReadArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, uint MethodOffset, MdToken FieldToken, ProcessTrackedObjectId ObjectId, bool IsVolatile);
public readonly record struct InstanceFieldWriteArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, uint MethodOffset, MdToken FieldToken, ProcessTrackedObjectId ObjectId, bool IsVolatile);
