// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MessagePack;

namespace SharpDetect.Core.Events;

[Union((int)RecordedEventType.ProfilerInitialize, typeof(ProfilerInitializeRecordedEvent))]
[Union((int)RecordedEventType.ProfilerLoad, typeof(ProfilerLoadRecordedEvent))]
[Union((int)RecordedEventType.ProfilerDestroy, typeof(ProfilerDestroyRecordedEvent))]
[Union((int)RecordedEventType.AssemblyLoad, typeof(AssemblyLoadRecordedEvent))]
[Union((int)RecordedEventType.ModuleLoad, typeof(ModuleLoadRecordedEvent))]
[Union((int)RecordedEventType.TypeLoad, typeof(TypeLoadRecordedEvent))]
[Union((int)RecordedEventType.JITCompilation, typeof(JitCompilationRecordedEvent))]
[Union((int)RecordedEventType.GarbageCollectionStart, typeof(GarbageCollectionStartRecordedEvent))]
[Union((int)RecordedEventType.GarbageCollectedTrackedObjects, typeof(GarbageCollectedTrackedObjectsRecordedEvent))]
[Union((int)RecordedEventType.GarbageCollectionFinish, typeof(GarbageCollectionFinishRecordedEvent))]
[Union((int)RecordedEventType.ThreadCreate, typeof(ThreadCreateRecordedEvent))]
[Union((int)RecordedEventType.ThreadRename, typeof(ThreadRenameRecordedEvent))]
[Union((int)RecordedEventType.ThreadDestroy, typeof(ThreadDestroyRecordedEvent))]
[Union((int)RecordedEventType.MethodEnter, typeof(MethodEnterRecordedEvent))]
[Union((int)RecordedEventType.MethodExit, typeof(MethodExitRecordedEvent))]
[Union((int)RecordedEventType.Tailcall, typeof(TailcallRecordedEvent))]
[Union((int)RecordedEventType.MethodEnterWithArguments, typeof(MethodEnterWithArgumentsRecordedEvent))]
[Union((int)RecordedEventType.MethodExitWithArguments, typeof(MethodExitWithArgumentsRecordedEvent))]
[Union((int)RecordedEventType.TailcallWithArguments, typeof(TailcallWithArgumentsRecordedEvent))]
[Union((int)RecordedEventType.AssemblyReferenceInjection, typeof(AssemblyReferenceInjectionRecordedEvent))]
[Union((int)RecordedEventType.TypeDefinitionInjection, typeof(TypeDefinitionInjectionRecordedEvent))]
[Union((int)RecordedEventType.TypeReferenceInjection, typeof(TypeReferenceInjectionRecordedEvent))]
[Union((int)RecordedEventType.MethodDefinitionInjection, typeof(MethodDefinitionInjectionRecordedEvent))]
[Union((int)RecordedEventType.MethodWrapperInjection, typeof(MethodWrapperInjectionRecordedEvent))]
[Union((int)RecordedEventType.MethodReferenceInjection, typeof(MethodReferenceInjectionRecordedEvent))]
[Union((int)RecordedEventType.MethodBodyRewrite, typeof(MethodBodyRewriteRecordedEvent))]
public interface IRecordedEventArgs
{
}
