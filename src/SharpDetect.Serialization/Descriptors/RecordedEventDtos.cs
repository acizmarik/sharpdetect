// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using MessagePack;
using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Serialization.Descriptors
{
    [MessagePackObject]
    public sealed record ProfilerLoadRecordedEventDto(
        [property: Key(0)] COR_PRF_RUNTIME_TYPE RuntimeType,
        [property: Key(1)] ushort MajorVersion,
        [property: Key(2)] ushort MinorVersion,
        [property: Key(3)] ushort BuildVersion,
        [property: Key(4)] ushort QfeVersion) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new ProfilerLoadRecordedEvent(
                RuntimeType,
                MajorVersion,
                MinorVersion,
                BuildVersion,
                QfeVersion);
        }
    }

    [MessagePackObject]
    public sealed record ProfilerInitializeRecordedEventDto() : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new ProfilerInitializeRecordedEvent();
        }
    }

    [MessagePackObject]
    public sealed record ProfilerDestroyRecordedEventDto() : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new ProfilerDestroyRecordedEvent();
        }
    }

    [MessagePackObject]
    public sealed record AssemblyLoadRecordedEventDto(
        [property: Key(0)] nuint AssemblyId,
        [property: Key(1)] string Name) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new AssemblyLoadRecordedEvent(
                new AssemblyId(AssemblyId),
                Name);
        }
    }

    [MessagePackObject]
    public sealed record ModuleLoadRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] nuint AssemblyId,
        [property: Key(2)] string Path) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new ModuleLoadRecordedEvent(
                new AssemblyId(AssemblyId),
                new ModuleId(ModuleId),
                Path);
        }
    }

    [MessagePackObject]
    public sealed record TypeLoadRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int TypeToken) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new TypeLoadRecordedEvent(
                new ModuleId(ModuleId),
                new MdTypeDef(TypeToken));
        }
    }

    [MessagePackObject]
    public sealed record JitCompilationRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int TypeToken,
        [property: Key(2)] int MethodToken) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new JitCompilationRecordedEvent(
                new ModuleId(ModuleId),
                new MdTypeDef(TypeToken),
                new MdMethodDef(MethodToken));
        }
    }

    [MessagePackObject]
    public sealed record ThreadCreateRecordedEventDto(
        [property: Key(0)] nuint ThreadId) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new ThreadCreateRecordedEvent(
                new ThreadId(ThreadId));
        }
    }

    [MessagePackObject]
    public sealed record ThreadRenameRecordedEventDto(
        [property: Key(0)] nuint ThreadId,
        [property: Key(1)] string NewName) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new ThreadRenameRecordedEvent(
                new ThreadId(ThreadId),
                NewName);
        }
    }

    [MessagePackObject]
    public sealed record ThreadDestroyRecordedEventDto(
        [property: Key(0)] nuint ThreadId) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new ThreadCreateRecordedEvent(
                new ThreadId(ThreadId));
        }
    }

    [MessagePackObject]
    public sealed record GarbageCollectionStartRecordedEventDto() : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new GarbageCollectionStartRecordedEvent();
        }
    }

    [MessagePackObject]
    public sealed record GarbageCollectionFinishRecordedEventDto(
        [property: Key(0)] ulong OldTrackedObjectsCount,
        [property: Key(1)] ulong NewTrackedObjectsCount) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new GarbageCollectionFinishRecordedEvent(
                OldTrackedObjectsCount,
                NewTrackedObjectsCount);
        }
    }

    [MessagePackObject]
    public sealed record MethodEnterRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int MethodToken,
        [property: Key(2)] ushort Interpretation) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodEnterRecordedEvent(
                new ModuleId(ModuleId),
                new MdMethodDef(MethodToken),
                Interpretation);
        }
    }

    [MessagePackObject]
    public sealed record MethodExitRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int MethodToken,
        [property: Key(2)] ushort Interpretation) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodExitRecordedEvent(
                new ModuleId(ModuleId),
                new MdMethodDef(MethodToken),
                Interpretation);
        }
    }

    [MessagePackObject]
    public sealed record TailcallRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int MethodToken) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new TailcallRecordedEvent(
                new ModuleId(ModuleId),
                new MdMethodDef(MethodToken));
        }
    }

    [MessagePackObject]
    public sealed record MethodEnterWithArgumentsRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int MethodToken,
        [property: Key(2)] ushort Interpretation,
        [property: Key(3)] byte[] ArgumentValues,
        [property: Key(4)] byte[] ArgumentInfos) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodEnterWithArgumentsRecordedEvent(
                new ModuleId(ModuleId),
                new MdMethodDef(MethodToken),
                Interpretation,
                ArgumentValues,
                ArgumentInfos);
        }
    }

    [MessagePackObject]
    public sealed record MethodExitWithArgumentsRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int MethodToken,
        [property: Key(2)] ushort Interpretation,
        [property: Key(3)] byte[] ReturnValue,
        [property: Key(4)] byte[] ByRefArgumentValues,
        [property: Key(5)] byte[] ByRefArgumentInfos) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodExitWithArgumentsRecordedEvent(
                new ModuleId(ModuleId),
                new MdMethodDef(MethodToken),
                Interpretation,
                ReturnValue,
                ByRefArgumentInfos,
                ByRefArgumentInfos);
        }
    }

    [MessagePackObject]
    public sealed record TailcallWithArgumentsRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int MethodToken,
        [property: Key(2)] byte[] ArgumentValues,
        [property: Key(3)] byte[] ArgumentInfos) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new TailcallWithArgumentsRecordedEvent(
                new ModuleId(ModuleId),
                new MdMethodDef(MethodToken),
                ArgumentValues,
                ArgumentInfos);
        }
    }

    [MessagePackObject]
    public sealed record AssemblyReferenceInjectionRecordedEventDto(
        [property: Key(0)] nuint TargetAssemblyId,
        [property: Key(1)] nuint AssemblyId) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new AssemblyReferenceInjectionRecordedEvent(
                new AssemblyId(TargetAssemblyId),
                new AssemblyId(AssemblyId));
        }
    }

    [MessagePackObject]
    public sealed record TypeDefinitionInjectionRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int TypeToken,
        [property: Key(2)] string TypeName) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new TypeDefinitionInjectionRecordedEvent(
                new ModuleId(ModuleId),
                new MdTypeDef(TypeToken),
                TypeName);
        }
    }

    [MessagePackObject]
    public sealed record TypeReferenceInjectionRecordedEventDto(
        [property: Key(0)] nuint TargetModuleId,
        [property: Key(1)] nuint FromModuleId,
        [property: Key(2)] int TypeToken) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new TypeReferenceInjectionRecordedEvent(
                new ModuleId(TargetModuleId),
                new ModuleId(FromModuleId),
                new MdTypeDef(TypeToken));
        }
    }

    [MessagePackObject]
    public sealed record MethodDefinitionInjectionRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int TypeToken,
        [property: Key(2)] int MethodToken,
        [property: Key(3)] string MethodName) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodDefinitionInjectionRecordedEvent(
                new ModuleId(ModuleId),
                new MdTypeDef(TypeToken),
                new MdMethodDef(MethodToken),
                MethodName);
        }
    }

    [MessagePackObject]
    public sealed record MethodWrapperInjectionRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int TypeToken,
        [property: Key(2)] int WrappedMethodToken,
        [property: Key(3)] int WrapperMethodToken,
        [property: Key(4)] string WrapperMethodName) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodWrapperInjectionRecordedEvent(
                new ModuleId(ModuleId),
                new MdTypeDef(TypeToken),
                new MdMethodDef(WrappedMethodToken),
                new MdMethodDef(WrapperMethodToken),
                WrapperMethodName);
        }
    }

    [MessagePackObject]
    public sealed record MethodReferenceInjectionRecordedEventDto(
        [property: Key(0)] nuint TargetModuleId,
        [property: Key(1)] string FullName) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodReferenceInjectionRecordedEvent(
                new ModuleId(TargetModuleId),
                FullName);
        }
    }

    [MessagePackObject]
    public sealed record MethodBodyRewriteRecordedEventDto(
        [property: Key(0)] nuint ModuleId,
        [property: Key(1)] int MethodToken) : IRecordedEventArgsDto
    {
        public IRecordedEventArgs Convert()
        {
            return new MethodBodyRewriteRecordedEvent(
                new ModuleId(ModuleId),
                new MdMethodDef(MethodToken));
        }
    }
}
