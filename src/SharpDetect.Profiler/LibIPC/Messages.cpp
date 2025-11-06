// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "Messages.h"

using namespace LibIPC;
using namespace LibIPC::Helpers;

MetadataMsg LibIPC::Helpers::CreateMetadataMsg(UINT32 pid, UINT64 tid)
{
    return MetadataMsg(pid, tid, tl::nullopt);
}

MetadataMsg LibIPC::Helpers::CreateMetadataMsg(UINT32 pid, UINT64 tid, UINT64 commandId)
{
    return MetadataMsg(pid, tid, tl::make_optional(commandId));
}

ProfilerInitializeMsg LibIPC::Helpers::CreateProfilerInitiazeMsg(LibIPC::MetadataMsg&& metadataMsg)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::ProfilerInitialize);
    return ProfilerInitializeMsg(std::move(metadataMsg), ProfilerInitializeMsgArgsInstance(discriminator, ProfilerInitializeMsgArgs()));
}

ProfilerLoadMsg LibIPC::Helpers::CreateProfilerLoadMsg(LibIPC::MetadataMsg&& metadataMsg, UINT32 runtime, UINT32 major, UINT32 minor, UINT32 build, UINT32 qfe)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::ProfilerLoad);
    return ProfilerLoadMsg(std::move(metadataMsg), ProfilerLoadMsgArgsInstance(discriminator, ProfilerLoadMsgArgs(runtime, major, minor, build, qfe)));
}

ProfilerDestroyMsg LibIPC::Helpers::CreateProfilerDestroyMsg(LibIPC::MetadataMsg&& metadataMsg)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::ProfilerDestroy);
    return ProfilerDestroyMsg(std::move(metadataMsg), ProfilerDestroyMsgArgsInstance(discriminator, ProfilerDestroyMsgArgs()));
}

AssemblyLoadMsg LibIPC::Helpers::CreateAssemblyLoadMsg(MetadataMsg&& metadataMsg, UINT64 assemblyId, const std::string& name)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::AssemblyLoad);
    return AssemblyLoadMsg(std::move(metadataMsg), AssemblyLoadMsgArgsInstance(discriminator, AssemblyLoadMsgArgs(assemblyId, name)));
}

ModuleLoadMsg LibIPC::Helpers::CreateModuleLoadMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT64 assemblyId, const std::string& name)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::ModuleLoad);
    return ModuleLoadMsg(std::move(metadataMsg), ModuleLoadMsgArgsInstance(discriminator, ModuleLoadMsgArgs(moduleId, assemblyId, name)));
}

TypeLoadMsg LibIPC::Helpers::CreateTypeLoadMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::TypeLoad);
    return TypeLoadMsg(std::move(metadataMsg), TypeLoadMsgArgsInstance(discriminator, TypeLoadMsgArgs(moduleId, mdTypeDef)));
}

JitCompilationMsg LibIPC::Helpers::CreateJitCompilationMsg(MetadataMsg&& metadataMsg, UINT32 mdTypeDef, UINT32 mdMethodDef)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::JITCompilation);
    return JitCompilationMsg(std::move(metadataMsg), JitCompilationMsgArgsInstance(discriminator, JitCompilationMsgArgs(mdTypeDef, mdMethodDef)));
}

GarbageCollectionStartMsg LibIPC::Helpers::CreateGarbageCollectionStartMsg(MetadataMsg&& metadataMsg)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::GarbageCollectionStart);
    return GarbageCollectionStartMsg(std::move(metadataMsg), GarbageCollectionStartMsgArgsInstance(discriminator, GarbageCollectionStartMsgArgs()));
}

GarbageCollectedTrackedObjectsMsg LibIPC::Helpers::CreateGarbageCollectedTrackedObjectsMsg(MetadataMsg&& metadataMsg, std::vector<UINT64>&& removedTrackedObjects)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::GarbageCollectedTrackedObjects);
    return GarbageCollectedTrackedObjectsMsg(std::move(metadataMsg), GarbageCollectedTrackedObjectsMsgArgsInstance(discriminator, GarbageCollectedTrackedObjectsMsgArgs(std::move(removedTrackedObjects))));
}

GarbageCollectionFinishMsg LibIPC::Helpers::CreateGarbageCollectionFinishMsg(MetadataMsg&& metadataMsg, UINT64 oldTrackedObjectsCount, UINT64 newTrackedObjectsCount)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::GarbageCollectionFinish);
    return GarbageCollectionFinishMsg(std::move(metadataMsg), GarbageCollectionFinishMsgArgsInstance(discriminator, GarbageCollectionFinishMsgArgs(oldTrackedObjectsCount, newTrackedObjectsCount)));
}

ThreadCreateMsg LibIPC::Helpers::CreateThreadCreateMsg(MetadataMsg&& metadataMsg, UINT64 threadId)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::ThreadCreate);
    return ThreadCreateMsg(std::move(metadataMsg), ThreadCreateMsgArgsInstance(discriminator, ThreadCreateMsgArgs(threadId)));
}

ThreadRenameMsg LibIPC::Helpers::CreateThreadRenameMsg(MetadataMsg&& metadataMsg, UINT64 threadId, const std::string& name)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::ThreadRename);
    return ThreadRenameMsg(std::move(metadataMsg), ThreadRenameMsgArgsInstance(discriminator, ThreadRenameMsgArgs(threadId, name)));
}

ThreadDestroyMsg LibIPC::Helpers::CreateThreadDestroyMsg(MetadataMsg&& metadataMsg, UINT64 threadId)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::ThreadCreate);
    return ThreadDestroyMsg(std::move(metadataMsg), ThreadDestroyMsgArgsInstance(discriminator, ThreadDestroyMsgArgs(threadId)));
}

MethodEnterMsg LibIPC::Helpers::CreateMethodEnterMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodEnter);
    return MethodEnterMsg(std::move(metadataMsg), MethodEnterMsgArgsInstance(discriminator, MethodEnterMsgArgs(moduleId, mdMethodDef, interpretation)));
}

MethodExitMsg LibIPC::Helpers::CreateMethodExitMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodExit);
    return MethodExitMsg(std::move(metadataMsg), MethodExitMsgArgsInstance(discriminator, MethodExitMsgArgs(moduleId, mdMethodDef, interpretation)));
}

TailcallMsg LibIPC::Helpers::CreateTailcallrMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::Tailcall);
    return TailcallMsg(std::move(metadataMsg), TailcallMsgArgsInstance(discriminator, TailcallMsgArgs(moduleId, mdMethodDef)));
}

MethodEnterWithArgumentsMsg LibIPC::Helpers::CreateMethodEnterWithArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodEnterWithArguments);
    return MethodEnterWithArgumentsMsg(std::move(metadataMsg), MethodEnterWithArgumentsMsgArgsInstance(discriminator, MethodEnterWithArgumentsMsgArgs(moduleId, mdMethodDef, interpretation, std::move(argValues), std::move(argInfos))));
}

MethodExitWithArgumentsMsg LibIPC::Helpers::CreateMethodExitWithArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation, std::vector<BYTE>&& returnValue, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodExitWithArguments);
    return MethodExitWithArgumentsMsg(std::move(metadataMsg), MethodExitWithArgumentsMsgArgsInstance(discriminator, MethodExitWithArgumentsMsgArgs(moduleId, mdMethodDef, interpretation, std::move(returnValue), std::move(argValues), std::move(argInfos))));
}

TailcallWithArgumentsMsg LibIPC::Helpers::CreateTailcallArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::TailcallWithArguments);
    return TailcallWithArgumentsMsg(std::move(metadataMsg), TailcallWithArgumentsMsgArgsInstance(discriminator, TailcallWithArgumentsMsgArgs(moduleId, mdMethodDef, std::move(argValues), std::move(argInfos))));
}

AssemblyReferenceInjectionMsg LibIPC::Helpers::CreateAssemblyReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetAssemblyId, UINT64 assemblyId)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::AssemblyReferenceInjection);
    return AssemblyReferenceInjectionMsg(std::move(metadataMsg), AssemblyReferenceInjectionMsgArgsInstance(discriminator, AssemblyReferenceInjectionMsgArgs(targetAssemblyId, assemblyId)));
}

TypeDefinitionInjectionMsg LibIPC::Helpers::CreateTypeDefinitionInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, const std::string& name)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::TypeDefinitionInjection);
    return TypeDefinitionInjectionMsg(std::move(metadataMsg), TypeDefinitionInjectionMsgArgsInstance(discriminator, TypeDefinitionInjectionMsgArgs(moduleId, mdTypeDef, name)));
}

TypeReferenceInjectionMsg LibIPC::Helpers::CreateTypeReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetModuleId, UINT64 fromModuleId, UINT32 mdTypeDef)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::TypeReferenceInjection);
    return TypeReferenceInjectionMsg(std::move(metadataMsg), TypeReferenceInjectionMsgArgsInstance(discriminator, TypeReferenceInjectionMsgArgs(targetModuleId, fromModuleId, mdTypeDef)));
}

MethodDefinitionInjectionMsg LibIPC::Helpers::CreateMethodDefinitionInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, UINT32 mdMethodDef, const std::string& name)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodDefinitionInjection);
    return MethodDefinitionInjectionMsg(std::move(metadataMsg), MethodDefinitionInjectionMsgArgsInstance(discriminator, MethodDefinitionInjectionMsgArgs(moduleId, mdTypeDef, mdMethodDef, name)));
}

MethodRefenceInjectionMsg LibIPC::Helpers::CreateMethodReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetModuleId, const std::string& fullName)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodReferenceInjection);
    return MethodRefenceInjectionMsg(std::move(metadataMsg), MethodRefenceInjectionMsgArgsInstance(discriminator, MethodRefenceInjectionMsgArgs(targetModuleId, fullName)));
}

MethodWrapperInjectionMsg LibIPC::Helpers::CreateMethodWrapperInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, UINT32 wrappedMethodToken, UINT32 wrapperMethodToken, const std::string& wrapperMethodName)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodWrapperInjection);
    return MethodWrapperInjectionMsg(std::move(metadataMsg), MethodWrapperInjectionMsgArgsInstance(discriminator, MethodWrapperInjectionMsgArgs(moduleId, mdTypeDef, wrappedMethodToken, wrapperMethodToken, wrapperMethodName)));
}

MethodBodyRewriteMsg LibIPC::Helpers::CreateMethodBodyRewriteMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::MethodBodyRewrite);
    return MethodBodyRewriteMsg(std::move(metadataMsg), MethodBodyRewriteMsgArgsInstance(discriminator, MethodBodyRewriteMsgArgs(moduleId, mdMethodDef)));
}

StackTraceSnapshotMsg LibIPC::Helpers::CreateStackTraceSnapshotMsg(MetadataMsg&& metadataMsg, UINT64 threadId, std::vector<UINT64>&& moduleIds, std::vector<UINT32>&& methodTokens)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::StackTraceSnapshot);
    return StackTraceSnapshotMsg(std::move(metadataMsg), StackTraceSnapshotMsgArgsInstance(discriminator, StackTraceSnapshotMsgArgs(threadId, std::move(moduleIds), std::move(methodTokens))));
}

StackTraceSnapshotsMsg LibIPC::Helpers::CreateStackTraceSnapshotsMsg(MetadataMsg&& metadataMsg, std::vector<StackTraceSnapshotMsgArgs>&& snapshots)
{
    auto const discriminator = static_cast<INT32>(RecordedEventType::StackTraceSnapshots);
    return StackTraceSnapshotsMsg(std::move(metadataMsg), StackTraceSnapshotsMsgArgsInstance(discriminator, StackTraceSnapshotsMsgArgs(std::move(snapshots))));
}