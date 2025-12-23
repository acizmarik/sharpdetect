// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Events;

[MessagePackObject]
public sealed record ProfilerLoadRecordedEvent(
    [property: Key(0)] COR_PRF_RUNTIME_TYPE RuntimeType,
    [property: Key(1)] ushort MajorVersion,
    [property: Key(2)] ushort MinorVersion,
    [property: Key(3)] ushort BuildVersion,
    [property: Key(4)] ushort QfeVersion) : IRecordedEventArgs;

[MessagePackObject]
public sealed record ProfilerInitializeRecordedEvent() : IRecordedEventArgs;

[MessagePackObject]
public sealed record ProfilerDestroyRecordedEvent() : IRecordedEventArgs;

[MessagePackObject]
public sealed record ProfilerAbortInitializeEvent(
    [property: Key(0)] string Reason) : IRecordedEventArgs;

[MessagePackObject]
public sealed record AssemblyLoadRecordedEvent(
    [property: Key(0)] AssemblyId AssemblyId,
    [property: Key(1)] string Name) : IRecordedEventArgs;

[MessagePackObject]
public sealed record ModuleLoadRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] AssemblyId AssemblyId,
    [property: Key(2)] string Path) : IRecordedEventArgs;

[MessagePackObject]
public sealed record TypeLoadRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdTypeDef TypeToken) : IRecordedEventArgs;

[MessagePackObject]
public sealed record JitCompilationRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdTypeDef TypeToken,
    [property: Key(2)] MdMethodDef MethodToken) : IRecordedEventArgs;

[MessagePackObject]
public sealed record ThreadCreateRecordedEvent(
    [property: Key(0)] ThreadId ThreadId) : IRecordedEventArgs;

[MessagePackObject]
public sealed record ThreadRenameRecordedEvent(
    [property: Key(0)] ThreadId ThreadId,
    [property: Key(1)] string NewName) : IRecordedEventArgs;

[MessagePackObject]
public sealed record ThreadDestroyRecordedEvent(
    [property: Key(0)] ThreadId ThreadId) : IRecordedEventArgs;

[MessagePackObject]
public sealed record GarbageCollectionStartRecordedEvent() : IRecordedEventArgs;

[MessagePackObject]
public sealed record GarbageCollectedTrackedObjectsRecordedEvent(
    [property: Key(0)] TrackedObjectId[] RemovedTrackedObjectIds) : IRecordedEventArgs;

[MessagePackObject]
public sealed record GarbageCollectionFinishRecordedEvent(
    [property: Key(0)] ulong OldTrackedObjectsCount,
    [property: Key(1)] ulong NewTrackedObjectsCount) : IRecordedEventArgs;

[MessagePackObject]
public sealed record MethodEnterRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdMethodDef MethodToken,
    [property: Key(2)] ushort Interpretation) : IRecordedEventArgs, ICustomizableEventType;

[MessagePackObject]
public sealed record MethodExitRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdMethodDef MethodToken,
    [property: Key(2)] ushort Interpretation) : IRecordedEventArgs, ICustomizableEventType;

[MessagePackObject]
public sealed record TailcallRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdMethodDef MethodToken) : IRecordedEventArgs;

[MessagePackObject]
public sealed record MethodEnterWithArgumentsRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdMethodDef MethodToken,
    [property: Key(2)] ushort Interpretation,
    [property: Key(3)] byte[] ArgumentValues,
    [property: Key(4)] byte[] ArgumentInfos) : IRecordedEventArgs, ICustomizableEventType;

[MessagePackObject]
public sealed record MethodExitWithArgumentsRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdMethodDef MethodToken,
    [property: Key(2)] ushort Interpretation,
    [property: Key(3)] byte[] ReturnValue,
    [property: Key(4)] byte[] ByRefArgumentValues,
    [property: Key(5)] byte[] ByRefArgumentInfos) : IRecordedEventArgs, ICustomizableEventType;

[MessagePackObject]
public sealed record TailcallWithArgumentsRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdMethodDef MethodToken,
    [property: Key(2)] byte[] ArgumentValues,
    [property: Key(3)] byte[] ArgumentInfos) : IRecordedEventArgs;

[MessagePackObject]
public sealed record AssemblyReferenceInjectionRecordedEvent(
    [property: Key(0)] AssemblyId TargetAssemblyId,
    [property: Key(1)] AssemblyId AssemblyId) : IRecordedEventArgs;

[MessagePackObject]
public sealed record TypeDefinitionInjectionRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdTypeDef TypeToken,
    [property: Key(2)] string TypeName) : IRecordedEventArgs;

[MessagePackObject]
public sealed record TypeReferenceInjectionRecordedEvent(
    [property: Key(0)] ModuleId TargetModuleId,
    [property: Key(1)] ModuleId FromModuleId,
    [property: Key(2)] MdTypeDef TypeToken) : IRecordedEventArgs;

[MessagePackObject]
public sealed record MethodDefinitionInjectionRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdTypeDef TypeToken,
    [property: Key(2)] MdMethodDef MethodToken,
    [property: Key(3)] string MethodName) : IRecordedEventArgs;

[MessagePackObject]
public sealed record MethodWrapperInjectionRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdTypeDef TypeToken,
    [property: Key(2)] MdMethodDef WrappedMethodToken,
    [property: Key(3)] MdMethodDef WrapperMethodToken,
    [property: Key(4)] string WrapperMethodName) : IRecordedEventArgs;

[MessagePackObject]
public sealed record MethodReferenceInjectionRecordedEvent(
    [property: Key(0)] ModuleId TargetModuleId,
    [property: Key(1)] string FullName) : IRecordedEventArgs;

[MessagePackObject]
public sealed record MethodBodyRewriteRecordedEvent(
    [property: Key(0)] ModuleId ModuleId,
    [property: Key(1)] MdMethodDef MethodToken) : IRecordedEventArgs;

[MessagePackObject]
public sealed record StackTraceSnapshotRecordedEvent(
    [property: Key(0)] ThreadId ThreadId,
    [property: Key(1)] ModuleId[] ModuleIds,
    [property: Key(2)] MdMethodDef[] MethodTokens) : IRecordedEventArgs;

[MessagePackObject]
public sealed record StackTraceSnapshotsRecordedEvent(
    [property: Key(0)] StackTraceSnapshotRecordedEvent[] Snapshots) : IRecordedEventArgs;

