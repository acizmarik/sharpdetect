// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Plugins.Models;

namespace SharpDetect.Core.Plugins;

public readonly record struct LockAcquireAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId, bool IsShared = false);
public readonly record struct LockAcquireResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId, bool IsSuccess, bool IsShared = false);
public readonly record struct LockReleaseArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId, bool IsShared = false);
public readonly record struct ObjectPulseOneArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct ObjectPulseAllArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct ObjectWaitAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId);
public readonly record struct ObjectWaitResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId LockId, bool IsSuccess);
public readonly record struct ThreadJoinAttemptArgs(ProcessThreadId BlockedProcessThreadId, ProcessThreadId JoiningProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken);
public readonly record struct ThreadJoinResultArgs(ProcessThreadId BlockedProcessThreadId, ProcessThreadId JoinedProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, bool IsSuccess);
public readonly record struct StaticFieldReadArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, nuint DeclaringTypeHandle, FieldAccessKind AccessKind, CapturedStackTrace Stack);
public readonly record struct StaticFieldWriteArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, nuint DeclaringTypeHandle, FieldAccessKind AccessKind, CapturedStackTrace Stack);
public readonly record struct InstanceFieldReadArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, ProcessTrackedObjectId ObjectId, FieldAccessKind AccessKind, CapturedStackTrace Stack);
public readonly record struct InstanceFieldWriteArgs(ProcessThreadId ProcessThreadId, uint MethodOffset, MdToken FieldToken, ProcessTrackedObjectId ObjectId, FieldAccessKind AccessKind, CapturedStackTrace Stack);
public readonly record struct ForkRequestArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId ChildId, ForkKind Kind);
public readonly record struct ForkStartArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId ChildId, ForkKind Kind);
public readonly record struct ForkFinishArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId ChildId, ForkKind Kind, ForkFinishKind FinishKind);
public readonly record struct ForkJoinArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId ChildId, TaskJoinOutcome Outcome)
{
    public bool IsSuccess => Outcome == TaskJoinOutcome.Completed;
}
public readonly record struct PermitsCreatedArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId PermitPoolId, int InitialCount);
public readonly record struct PermitAcquireAttemptArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId PermitPoolId);
public readonly record struct PermitAcquireResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId PermitPoolId, bool IsSuccess, bool IsAsyncContinuation = false);
public readonly record struct PermitsReleaseArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId PermitPoolId, int ReleaseCount);
public readonly record struct SignalCreatedArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SignalId, bool InitialState, bool IsAutoReset);
public readonly record struct SignalSetArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SignalId, bool IsAutoReset);
public readonly record struct SignalResetArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SignalId);
public readonly record struct SignalWaitResultArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef MethodToken, ProcessTrackedObjectId SignalId, bool IsAutoReset, bool IsSuccess);
public readonly record struct TypeInitializerArgs(ProcessThreadId ProcessThreadId, ModuleId ModuleId, MdMethodDef TypeInitializerToken);
public readonly record struct ValuePublicationArgs(ProcessThreadId ProcessThreadId, ProcessTrackedObjectId Container, ProcessTrackedObjectId Value, ValuePublicationKind Kind);
