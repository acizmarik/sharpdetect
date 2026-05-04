// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.E2ETests.Utils;

public interface ITestEventsSource
{
    event Action<(RecordedEventMetadata Metadata, AssemblyLoadRecordedEvent Args)>? AssemblyLoaded;
    event Action<(RecordedEventMetadata Metadata, AssemblyReferenceInjectionRecordedEvent Args)>? AssemblyReferenceInjected;
    event Action<(RecordedEventMetadata Metadata, GarbageCollectionStartRecordedEvent Args)>? GarbageCollectionStarted;
    event Action<(RecordedEventMetadata Metadata, GarbageCollectedTrackedObjectsRecordedEvent Args)>? GarbageCollectedTrackedObjects;
    event Action<(RecordedEventMetadata Metadata, GarbageCollectionFinishRecordedEvent Args)>? GarbageCollectionFinished;
    event Action<(RecordedEventMetadata Metadata, JitCompilationRecordedEvent Args)>? JitCompilationStarted;
    event Action<(RecordedEventMetadata Metadata, MethodBodyRewriteRecordedEvent Args)>? MethodBodyRewritten;
    event Action<(RecordedEventMetadata Metadata, MethodDefinitionInjectionRecordedEvent Args)>? MethodDefinitionInjected;
    event Action<(RecordedEventMetadata Metadata, MethodEnterRecordedEvent Args)>? MethodEntered;
    event Action<(RecordedEventMetadata Metadata, MethodEnterWithArgumentsRecordedEvent Args)>? MethodEnteredWithArguments;
    event Action<(RecordedEventMetadata Metadata, MethodExitRecordedEvent Args)>? MethodExited;
    event Action<(RecordedEventMetadata Metadata, MethodExitWithArgumentsRecordedEvent Args)>? MethodExitedWithArguments;
    event Action<(RecordedEventMetadata Metadata, MethodReferenceInjectionRecordedEvent Args)>? MethodReferenceInjected;
    event Action<(RecordedEventMetadata Metadata, MethodWrapperInjectionRecordedEvent Args)>? MethodWrapperInjected;
    event Action<(RecordedEventMetadata Metadata, ModuleLoadRecordedEvent Args)>? ModuleLoaded;
    event Action<(RecordedEventMetadata Metadata, ProfilerDestroyRecordedEvent Args)>? ProfilerDestroyed;
    event Action<(RecordedEventMetadata Metadata, ProfilerInitializeRecordedEvent Args)>? ProfilerInitialized;
    event Action<(RecordedEventMetadata Metadata, ProfilerLoadRecordedEvent Args)>? ProfilerLoaded;
    event Action<(RecordedEventMetadata Metadata, TailcallRecordedEvent Args)>? Tailcalled;
    event Action<(RecordedEventMetadata Metadata, TailcallWithArgumentsRecordedEvent Args)>? TailcalledWithArguments;
    event Action<(RecordedEventMetadata Metadata, ThreadCreateRecordedEvent Args)>? ThreadCreated;
    event Action<(RecordedEventMetadata Metadata, ThreadDestroyRecordedEvent Args)>? ThreadDestroyed;
    event Action<(RecordedEventMetadata Metadata, ThreadRenameRecordedEvent Args)>? ThreadRenamed;
    event Action<(RecordedEventMetadata Metadata, TypeDefinitionInjectionRecordedEvent Args)>? TypeDefinitionInjected;
    event Action<(RecordedEventMetadata Metadata, TypeLoadRecordedEvent Args)>? TypeLoaded;
    event Action<(RecordedEventMetadata Metadata, TypeReferenceInjectionRecordedEvent Args)>? TypeReferenceInjected;
}
