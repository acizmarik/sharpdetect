// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Events
{
    public sealed record ProfilerLoadRecordedEvent(
        COR_PRF_RUNTIME_TYPE RuntimeType,
        ushort MajorVersion,
        ushort MinorVersion,
        ushort BuildVersion,
        ushort QfeVersion) : IRecordedEventArgs;

    public sealed record ProfilerInitializeRecordedEvent() : IRecordedEventArgs;

    public sealed record ProfilerDestroyRecordedEvent() : IRecordedEventArgs;

    public sealed record AssemblyLoadRecordedEvent(
        AssemblyId AssemblyId,
        string Name) : IRecordedEventArgs;

    public sealed record ModuleLoadRecordedEvent(
        AssemblyId AssemblyId,
        ModuleId ModuleId,
        string Path) : IRecordedEventArgs;

    public sealed record TypeLoadRecordedEvent(
        ModuleId ModuleId,
        MdTypeDef TypeToken) : IRecordedEventArgs;

    public sealed record JitCompilationRecordedEvent(
        ModuleId ModuleId,
        MdTypeDef TypeToken,
        MdMethodDef MethodToken) : IRecordedEventArgs;

    public sealed record ThreadCreateRecordedEvent(
        ThreadId ThreadId) : IRecordedEventArgs;

    public sealed record ThreadRenameRecordedEvent(
        ThreadId ThreadId,
        string NewName) : IRecordedEventArgs;

    public sealed record ThreadDestroyRecordedEvent(
        ThreadId ThreadId) : IRecordedEventArgs;

    public sealed record GarbageCollectionStartRecordedEvent() : IRecordedEventArgs;

    public sealed record GarbageCollectionFinishRecordedEvent(
        ulong OldTrackedObjectsCount,
        ulong NewTrackedObjectsCount) : IRecordedEventArgs;

    public sealed record MethodEnterRecordedEvent(
        ModuleId ModuleId,
        MdMethodDef MethodToken,
        ushort Interpretation) : IRecordedEventArgs, ICustomizableEventType;

    public sealed record MethodExitRecordedEvent(
        ModuleId ModuleId,
        MdMethodDef MethodToken,
        ushort Interpretation) : IRecordedEventArgs, ICustomizableEventType;

    public sealed record TailcallRecordedEvent(
        ModuleId ModuleId,
        MdMethodDef MethodToken) : IRecordedEventArgs;

    public sealed record MethodEnterWithArgumentsRecordedEvent(
        ModuleId ModuleId,
        MdMethodDef MethodToken,
        ushort Interpretation,
        byte[] ArgumentValues,
        byte[] ArgumentInfos) : IRecordedEventArgs, ICustomizableEventType;

    public sealed record MethodExitWithArgumentsRecordedEvent(
        ModuleId ModuleId,
        MdMethodDef MethodToken,
        ushort Interpretation,
        byte[] ReturnValue,
        byte[] ByRefArgumentValues,
        byte[] ByRefArgumentInfos) : IRecordedEventArgs, ICustomizableEventType;

    public sealed record TailcallWithArgumentsRecordedEvent(
        ModuleId ModuleId,
        MdMethodDef MethodToken,
        byte[] ArgumentValues,
        byte[] ArgumentInfos) : IRecordedEventArgs;

    public sealed record AssemblyReferenceInjectionRecordedEvent(
        AssemblyId TargetAssemblyId,
        AssemblyId AssemblyId) : IRecordedEventArgs;

    public sealed record TypeDefinitionInjectionRecordedEvent(
        ModuleId ModuleId,
        MdTypeDef TypeToken,
        string TypeName) : IRecordedEventArgs;

    public sealed record TypeReferenceInjectionRecordedEvent(
        ModuleId TargetModuleId,
        ModuleId FromModuleId,
        MdTypeDef TypeToken) : IRecordedEventArgs;

    public sealed record MethodDefinitionInjectionRecordedEvent(
        ModuleId ModuleId,
        MdTypeDef TypeToken,
        MdMethodDef MethodToken,
        string MethodName) : IRecordedEventArgs;

    public sealed record MethodWrapperInjectionRecordedEvent(
        ModuleId ModuleId,
        MdTypeDef TypeToken,
        MdMethodDef WrappedMethodToken,
        MdMethodDef WrapperMethodToken,
        string WrapperMethodName) : IRecordedEventArgs;

    public sealed record MethodReferenceInjectionRecordedEvent(
        ModuleId TargetModuleId,
        string FullName) : IRecordedEventArgs;

    public sealed record MethodBodyRewriteRecordedEvent(
        ModuleId ModuleId,
        MdMethodDef MethodToken) : IRecordedEventArgs;
}
