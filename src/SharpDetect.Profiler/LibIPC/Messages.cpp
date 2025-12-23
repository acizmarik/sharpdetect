// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "Messages.h"

using namespace LibIPC;
using namespace LibIPC::Helpers;

MetadataMsg Helpers::CreateMetadataMsg(UINT32 pid, UINT64 tid)
{
    return { pid, tid, std::nullopt };
}

MetadataMsg Helpers::CreateMetadataMsg(UINT32 pid, UINT64 tid, UINT64 commandId)
{
    return { pid, tid, std::make_optional(commandId) };
}

ProfilerInitializeMsg Helpers::CreateProfilerInitiazeMsg(LibIPC::MetadataMsg&& metadataMsg)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ProfilerInitialize);
    return { std::move(metadataMsg), ProfilerInitializeMsgArgsInstance(discriminator, ProfilerInitializeMsgArgs()) };
}

ProfilerLoadMsg Helpers::CreateProfilerLoadMsg(LibIPC::MetadataMsg&& metadataMsg, UINT32 runtime, UINT32 major, UINT32 minor, UINT32 build, UINT32 qfe)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ProfilerLoad);
    return { std::move(metadataMsg), ProfilerLoadMsgArgsInstance(discriminator, ProfilerLoadMsgArgs(runtime, major, minor, build, qfe)) };
}

ProfilerDestroyMsg Helpers::CreateProfilerDestroyMsg(LibIPC::MetadataMsg&& metadataMsg)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ProfilerDestroy);
    return { std::move(metadataMsg), ProfilerDestroyMsgArgsInstance(discriminator, ProfilerDestroyMsgArgs()) };
}

ProfilerAbortInitializeMsg Helpers::CreateProfilerAbortInitializeMsg(LibIPC::MetadataMsg&& metadataMsg, const std::string& reason)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ProfilerAbortInitialize);
    return { std::move(metadataMsg), ProfilerAbortInitializeMsgArgsInstance(discriminator, ProfilerAbortInitializeMsgArgs(reason)) };
}

AssemblyLoadMsg Helpers::CreateAssemblyLoadMsg(MetadataMsg&& metadataMsg, UINT64 assemblyId, const std::string& name)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::AssemblyLoad);
    return { std::move(metadataMsg), AssemblyLoadMsgArgsInstance(discriminator, AssemblyLoadMsgArgs(assemblyId, name)) };
}

ModuleLoadMsg Helpers::CreateModuleLoadMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT64 assemblyId, const std::string& name)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ModuleLoad);
    return { std::move(metadataMsg), ModuleLoadMsgArgsInstance(discriminator, ModuleLoadMsgArgs(moduleId, assemblyId, name)) };
}

TypeLoadMsg Helpers::CreateTypeLoadMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::TypeLoad);
    return { std::move(metadataMsg), TypeLoadMsgArgsInstance(discriminator, TypeLoadMsgArgs(moduleId, mdTypeDef)) };
}

JitCompilationMsg Helpers::CreateJitCompilationMsg(MetadataMsg&& metadataMsg, UINT32 mdTypeDef, UINT32 mdMethodDef)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::JITCompilation);
    return { std::move(metadataMsg), JitCompilationMsgArgsInstance(discriminator, JitCompilationMsgArgs(mdTypeDef, mdMethodDef)) };
}

GarbageCollectionStartMsg Helpers::CreateGarbageCollectionStartMsg(MetadataMsg&& metadataMsg)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::GarbageCollectionStart);
    return { std::move(metadataMsg), GarbageCollectionStartMsgArgsInstance(discriminator, GarbageCollectionStartMsgArgs()) };
}

GarbageCollectedTrackedObjectsMsg Helpers::CreateGarbageCollectedTrackedObjectsMsg(MetadataMsg&& metadataMsg, std::vector<UINT64>&& removedTrackedObjects)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::GarbageCollectedTrackedObjects);
    return { std::move(metadataMsg), GarbageCollectedTrackedObjectsMsgArgsInstance(discriminator, GarbageCollectedTrackedObjectsMsgArgs(std::move(removedTrackedObjects)))};
}

GarbageCollectionFinishMsg Helpers::CreateGarbageCollectionFinishMsg(MetadataMsg&& metadataMsg, UINT64 oldTrackedObjectsCount, UINT64 newTrackedObjectsCount)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::GarbageCollectionFinish);
    return { std::move(metadataMsg), GarbageCollectionFinishMsgArgsInstance(discriminator, GarbageCollectionFinishMsgArgs(oldTrackedObjectsCount, newTrackedObjectsCount)) };
}

ThreadCreateMsg Helpers::CreateThreadCreateMsg(MetadataMsg&& metadataMsg, UINT64 threadId)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ThreadCreate);
    return { std::move(metadataMsg), ThreadCreateMsgArgsInstance(discriminator, ThreadCreateMsgArgs(threadId)) };
}

ThreadRenameMsg Helpers::CreateThreadRenameMsg(MetadataMsg&& metadataMsg, UINT64 threadId, const std::string& name)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ThreadRename);
    return { std::move(metadataMsg), ThreadRenameMsgArgsInstance(discriminator, ThreadRenameMsgArgs(threadId, name)) };
}

ThreadDestroyMsg Helpers::CreateThreadDestroyMsg(MetadataMsg&& metadataMsg, UINT64 threadId)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::ThreadCreate);
    return { std::move(metadataMsg), ThreadDestroyMsgArgsInstance(discriminator, ThreadDestroyMsgArgs(threadId)) };
}

MethodEnterMsg Helpers::CreateMethodEnterMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodEnter);
    return { std::move(metadataMsg), MethodEnterMsgArgsInstance(discriminator, MethodEnterMsgArgs(moduleId, mdMethodDef, interpretation)) };
}

MethodExitMsg Helpers::CreateMethodExitMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodExit);
    return { std::move(metadataMsg), MethodExitMsgArgsInstance(discriminator, MethodExitMsgArgs(moduleId, mdMethodDef, interpretation)) };
}

TailcallMsg Helpers::CreateTailcallrMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::Tailcall);
    return { std::move(metadataMsg), TailcallMsgArgsInstance(discriminator, TailcallMsgArgs(moduleId, mdMethodDef)) };
}

MethodEnterWithArgumentsMsg Helpers::CreateMethodEnterWithArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodEnterWithArguments);
    return { std::move(metadataMsg), MethodEnterWithArgumentsMsgArgsInstance(discriminator, MethodEnterWithArgumentsMsgArgs(moduleId, mdMethodDef, interpretation, std::move(argValues), std::move(argInfos))) };
}

MethodExitWithArgumentsMsg Helpers::CreateMethodExitWithArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation, std::vector<BYTE>&& returnValue, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodExitWithArguments);
    return { std::move(metadataMsg), MethodExitWithArgumentsMsgArgsInstance(discriminator, MethodExitWithArgumentsMsgArgs(moduleId, mdMethodDef, interpretation, std::move(returnValue), std::move(argValues), std::move(argInfos))) };
}

TailcallWithArgumentsMsg Helpers::CreateTailcallArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::TailcallWithArguments);
    return { std::move(metadataMsg), TailcallWithArgumentsMsgArgsInstance(discriminator, TailcallWithArgumentsMsgArgs(moduleId, mdMethodDef, std::move(argValues), std::move(argInfos))) };
}

AssemblyReferenceInjectionMsg Helpers::CreateAssemblyReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetAssemblyId, UINT64 assemblyId)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::AssemblyReferenceInjection);
    return { std::move(metadataMsg), AssemblyReferenceInjectionMsgArgsInstance(discriminator, AssemblyReferenceInjectionMsgArgs(targetAssemblyId, assemblyId)) };
}

TypeDefinitionInjectionMsg Helpers::CreateTypeDefinitionInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, const std::string& name)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::TypeDefinitionInjection);
    return { std::move(metadataMsg), TypeDefinitionInjectionMsgArgsInstance(discriminator, TypeDefinitionInjectionMsgArgs(moduleId, mdTypeDef, name)) };
}

TypeReferenceInjectionMsg Helpers::CreateTypeReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetModuleId, UINT64 fromModuleId, UINT32 mdTypeDef)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::TypeReferenceInjection);
    return { std::move(metadataMsg), TypeReferenceInjectionMsgArgsInstance(discriminator, TypeReferenceInjectionMsgArgs(targetModuleId, fromModuleId, mdTypeDef)) };
}

MethodDefinitionInjectionMsg Helpers::CreateMethodDefinitionInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, UINT32 mdMethodDef, const std::string& name)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodDefinitionInjection);
    return { std::move(metadataMsg), MethodDefinitionInjectionMsgArgsInstance(discriminator, MethodDefinitionInjectionMsgArgs(moduleId, mdTypeDef, mdMethodDef, name)) };
}

MethodRefenceInjectionMsg Helpers::CreateMethodReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetModuleId, const std::string& fullName)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodReferenceInjection);
    return { std::move(metadataMsg), MethodRefenceInjectionMsgArgsInstance(discriminator, MethodRefenceInjectionMsgArgs(targetModuleId, fullName)) };
}

MethodWrapperInjectionMsg Helpers::CreateMethodWrapperInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, UINT32 wrappedMethodToken, UINT32 wrapperMethodToken, const std::string& wrapperMethodName)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodWrapperInjection);
    return { std::move(metadataMsg), MethodWrapperInjectionMsgArgsInstance(discriminator, MethodWrapperInjectionMsgArgs(moduleId, mdTypeDef, wrappedMethodToken, wrapperMethodToken, wrapperMethodName)) };
}

MethodBodyRewriteMsg Helpers::CreateMethodBodyRewriteMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::MethodBodyRewrite);
    return { std::move(metadataMsg), MethodBodyRewriteMsgArgsInstance(discriminator, MethodBodyRewriteMsgArgs(moduleId, mdMethodDef)) };
}

StackTraceSnapshotMsg Helpers::CreateStackTraceSnapshotMsg(MetadataMsg&& metadataMsg, UINT64 threadId, std::vector<UINT64>&& moduleIds, std::vector<UINT32>&& methodTokens)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::StackTraceSnapshot);
    return { std::move(metadataMsg), StackTraceSnapshotMsgArgsInstance(discriminator, StackTraceSnapshotMsgArgs(threadId, std::move(moduleIds), std::move(methodTokens))) };
}

StackTraceSnapshotsMsg Helpers::CreateStackTraceSnapshotsMsg(MetadataMsg&& metadataMsg, std::vector<StackTraceSnapshotMsgArgs>&& snapshots)
{
    constexpr auto discriminator = static_cast<INT32>(RecordedEventType::StackTraceSnapshots);
    return { std::move(metadataMsg), StackTraceSnapshotsMsgArgsInstance(discriminator, StackTraceSnapshotsMsgArgs(std::move(snapshots))) };
}