// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events;

public abstract class RecordedEventActionVisitorBase
{
    public void Visit(RecordedEventMetadata metadata, IRecordedEventArgs args)
    {
        switch (args)
        {
            case ProfilerLoadRecordedEvent profilerLoadArgs: Visit(metadata, profilerLoadArgs); break;
            case ProfilerInitializeRecordedEvent profilerInitializeArgs: Visit(metadata, profilerInitializeArgs); break;
            case ProfilerDestroyRecordedEvent profilerDestroyArgs: Visit(metadata, profilerDestroyArgs); break;
            case AssemblyLoadRecordedEvent assemblyLoadArgs: Visit(metadata, assemblyLoadArgs); break;
            case ModuleLoadRecordedEvent moduleLoadArgs: Visit(metadata, moduleLoadArgs); break;
            case TypeLoadRecordedEvent typeLoadArgs: Visit(metadata, typeLoadArgs); break;
            case JitCompilationRecordedEvent jitCompilationArgs: Visit(metadata, jitCompilationArgs); break;
            case ThreadCreateRecordedEvent threadCreateArgs: Visit(metadata, threadCreateArgs); break;
            case ThreadRenameRecordedEvent threadRenameArgs: Visit(metadata, threadRenameArgs); break;
            case ThreadDestroyRecordedEvent threadDestroyArgs: Visit(metadata, threadDestroyArgs); break;
            case GarbageCollectionStartRecordedEvent gcStartedArgs: Visit(metadata, gcStartedArgs); break;
            case GarbageCollectedTrackedObjectsRecordedEvent gcTrackedObjectsArgs: Visit(metadata, gcTrackedObjectsArgs); break;
            case GarbageCollectionFinishRecordedEvent gcFinishedArgs: Visit(metadata, gcFinishedArgs); break;
            case MethodEnterRecordedEvent methodEnterArgs: Visit(metadata, methodEnterArgs); break;
            case MethodExitRecordedEvent methodExitArgs: Visit(metadata, methodExitArgs); break;
            case TailcallRecordedEvent tailcallArgs: Visit(metadata, tailcallArgs); break;
            case MethodEnterWithArgumentsRecordedEvent methodEnterWithArgumentsArgs: Visit(metadata, methodEnterWithArgumentsArgs); break;
            case MethodExitWithArgumentsRecordedEvent methodExitWithArgumentsArgs: Visit(metadata, methodExitWithArgumentsArgs); break;
            case TailcallWithArgumentsRecordedEvent tailcallWithArgumentsArgs: Visit(metadata, tailcallWithArgumentsArgs); break;
            case AssemblyReferenceInjectionRecordedEvent assemblyReferenceInjectionArgs: Visit(metadata, assemblyReferenceInjectionArgs); break;
            case TypeDefinitionInjectionRecordedEvent typeDefinitionInjectionArgs: Visit(metadata, typeDefinitionInjectionArgs); break;
            case TypeReferenceInjectionRecordedEvent typeReferenceInjectionArgs: Visit(metadata, typeReferenceInjectionArgs); break;
            case MethodDefinitionInjectionRecordedEvent methodDefinitionInjectionArgs: Visit(metadata, methodDefinitionInjectionArgs); break;
            case MethodWrapperInjectionRecordedEvent methodWrapperInjectionArgs: Visit(metadata, methodWrapperInjectionArgs); break;
            case MethodReferenceInjectionRecordedEvent methodReferenceInjectionArgs: Visit(metadata, methodReferenceInjectionArgs); break;
            case MethodBodyRewriteRecordedEvent methodBodyRewriteArgs: Visit(metadata, methodBodyRewriteArgs); break;
            default: throw new NotSupportedException($"{nameof(RecordedEventActionVisitorBase)} does not support {args.GetType()}.");
        }
    }

    protected virtual void Visit(RecordedEventMetadata metadata, ProfilerLoadRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, ProfilerInitializeRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, ProfilerDestroyRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, AssemblyLoadRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, ModuleLoadRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, TypeLoadRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, JitCompilationRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, ThreadCreateRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, ThreadRenameRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, ThreadDestroyRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, GarbageCollectionStartRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, GarbageCollectedTrackedObjectsRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, GarbageCollectionFinishRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodEnterRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodExitRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, TailcallRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodEnterWithArgumentsRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodExitWithArgumentsRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, TailcallWithArgumentsRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, AssemblyReferenceInjectionRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, TypeDefinitionInjectionRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, TypeReferenceInjectionRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodDefinitionInjectionRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodWrapperInjectionRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodReferenceInjectionRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void Visit(RecordedEventMetadata metadata, MethodBodyRewriteRecordedEvent args)
        => DefaultVisit(metadata, args);

    protected virtual void DefaultVisit(RecordedEventMetadata metadata, IRecordedEventArgs args)
        => throw new NotImplementedException($"{nameof(RecordedEventActionVisitorBase)} is missing implementation for {args.GetType()}.");
}
