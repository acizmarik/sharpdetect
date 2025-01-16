// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;
using SharpDetect.Core.Events;

namespace SharpDetect.Serialization.Descriptors;

[Union((int)RecordedEventType.ProfilerInitialize, typeof(ProfilerInitializeRecordedEventDto))]
[Union((int)RecordedEventType.ProfilerLoad, typeof(ProfilerLoadRecordedEventDto))]
[Union((int)RecordedEventType.ProfilerDestroy, typeof(ProfilerDestroyRecordedEventDto))]
[Union((int)RecordedEventType.AssemblyLoad, typeof(AssemblyLoadRecordedEventDto))]
[Union((int)RecordedEventType.ModuleLoad, typeof(ModuleLoadRecordedEventDto))]
[Union((int)RecordedEventType.TypeLoad, typeof(TypeLoadRecordedEventDto))]
[Union((int)RecordedEventType.JITCompilation, typeof(JitCompilationRecordedEventDto))]
[Union((int)RecordedEventType.GarbageCollectionStart, typeof(GarbageCollectionStartRecordedEventDto))]
[Union((int)RecordedEventType.GarbageCollectedTrackedObjects, typeof(GarbageCollectedTrackedObjectsRecordedEventDto))]
[Union((int)RecordedEventType.GarbageCollectionFinish, typeof(GarbageCollectionFinishRecordedEventDto))]
[Union((int)RecordedEventType.ThreadCreate, typeof(ThreadCreateRecordedEventDto))]
[Union((int)RecordedEventType.ThreadRename, typeof(ThreadRenameRecordedEventDto))]
[Union((int)RecordedEventType.ThreadDestroy, typeof(ThreadDestroyRecordedEventDto))]
[Union((int)RecordedEventType.MethodEnter, typeof(MethodEnterRecordedEventDto))]
[Union((int)RecordedEventType.MethodExit, typeof(MethodExitRecordedEventDto))]
[Union((int)RecordedEventType.Tailcall, typeof(TailcallRecordedEventDto))]
[Union((int)RecordedEventType.MethodEnterWithArguments, typeof(MethodEnterWithArgumentsRecordedEventDto))]
[Union((int)RecordedEventType.MethodExitWithArguments, typeof(MethodExitWithArgumentsRecordedEventDto))]
[Union((int)RecordedEventType.TailcallWithArguments, typeof(TailcallWithArgumentsRecordedEventDto))]
[Union((int)RecordedEventType.AssemblyReferenceInjection, typeof(AssemblyReferenceInjectionRecordedEventDto))]
[Union((int)RecordedEventType.TypeDefinitionInjection, typeof(TypeDefinitionInjectionRecordedEventDto))]
[Union((int)RecordedEventType.TypeReferenceInjection, typeof(TypeReferenceInjectionRecordedEventDto))]
[Union((int)RecordedEventType.MethodDefinitionInjection, typeof(MethodDefinitionInjectionRecordedEventDto))]
[Union((int)RecordedEventType.MethodWrapperInjection, typeof(MethodWrapperInjectionRecordedEventDto))]
[Union((int)RecordedEventType.MethodReferenceInjection, typeof(MethodReferenceInjectionRecordedEventDto))]
[Union((int)RecordedEventType.MethodBodyRewrite, typeof(MethodBodyRewriteRecordedEventDto))]
public interface IRecordedEventArgsDto
{
    IRecordedEventArgs Convert();
}
