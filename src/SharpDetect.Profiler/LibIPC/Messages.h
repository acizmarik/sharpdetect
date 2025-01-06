// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>
#include <vector>

#include "../lib/msgpack-c/include/msgpack.hpp"
#include "cor.h"

namespace LibIPC
{
	enum class RecordedEventType
	{
		NotSpecified = 0,

		/* Generic method enter/exit */
		MethodEnter = 1,
		MethodExit = 2,
		Tailcall = 3,
		MethodEnterWithArguments = 4,
		MethodExitWithArguments = 5,
		TailcallWithArguments = 6,

		/* Custom intepretation for method enter/exit (extension point) */
		InterpretedMethodEnter = 7,
		InterpretedMethodExit = 8,
		InterpretedTailcall = 9,
		InterpretedMethodEnterWithArguments = 10,
		InterpretedMethodExitWithArguments = 11,
		InterpretedTailcallWithArguments = 12,

		/* Threading */
		ThreadCreate = 13,
		ThreadRename = 14,
		ThreadDestroy = 15,

		/* Metadata loads, JIT */
		AssemblyLoad = 16,
		ModuleLoad = 17,
		TypeLoad = 18,
		JITCompilation = 19,

		/* Garbage collection */
		GarbageCollectionStart = 20,
		GarbageCollectionFinish = 21,
		GarbageCollectionSurvivors = 22,
		GarbageCollectionCompaction = 23,

		/* Metadata modifications */
		AssemblyReferenceInjection = 24,
		TypeDefinitionInjection = 25,
		TypeReferenceInjection = 26,
		MethodDefinitionInjection = 27,
		MethodWrapperInjection = 28,
		MethodReferenceInjection = 29,
		MethodBodyRewrite = 30,

		/* Objects tracking */
		ObjectTracking = 31,
		ObjectRemoved = 32,

		/* Profiler lifecycle */
		ProfilerLoad = 33,
		ProfilerInitialize = 34,
		ProfilerDestroy = 35
	};

	using MetadataMsg = msgpack::type::tuple<UINT32, UINT64>;

	using ProfilerInitializeMsgArgs = msgpack::type::tuple<>;
	using ProfilerInitializeMsgArgsInstance = msgpack::type::tuple<INT32, ProfilerInitializeMsgArgs>;
	using ProfilerInitializeMsg = msgpack::type::tuple<MetadataMsg, ProfilerInitializeMsgArgsInstance>;

	using ProfilerLoadMsgArgs = msgpack::type::tuple<UINT32, UINT32, UINT32, UINT32, UINT32>;
	using ProfilerLoadMsgArgsInstance = msgpack::type::tuple<INT32, ProfilerLoadMsgArgs>;
	using ProfilerLoadMsg = msgpack::type::tuple<MetadataMsg, ProfilerLoadMsgArgsInstance>;

	using ProfilerDestroyMsgArgs = msgpack::type::tuple<>;
	using ProfilerDestroyMsgArgsInstance = msgpack::type::tuple<INT32, ProfilerDestroyMsgArgs>;
	using ProfilerDestroyMsg = msgpack::type::tuple<MetadataMsg, ProfilerDestroyMsgArgsInstance>;

	using AssemblyLoadMsgArgs = msgpack::type::tuple<UINT64, std::string>;
	using AssemblyLoadMsgArgsInstance = msgpack::type::tuple<INT32, AssemblyLoadMsgArgs>;
	using AssemblyLoadMsg = msgpack::type::tuple<MetadataMsg, AssemblyLoadMsgArgsInstance>;

	using ModuleLoadMsgArgs = msgpack::type::tuple<UINT64, UINT64, std::string>;
	using ModuleLoadMsgArgsInstance = msgpack::type::tuple<INT32, ModuleLoadMsgArgs>;
	using ModuleLoadMsg = msgpack::type::tuple<MetadataMsg, ModuleLoadMsgArgsInstance>;

	using TypeLoadMsgArgs = msgpack::type::tuple<UINT64, UINT32>;
	using TypeLoadMsgArgsInstance = msgpack::type::tuple<INT32, TypeLoadMsgArgs>;
	using TypeLoadMsg = msgpack::type::tuple<MetadataMsg, TypeLoadMsgArgsInstance>;

	using JitCompilationMsgArgs = msgpack::type::tuple<UINT32, UINT32>;
	using JitCompilationMsgArgsInstance = msgpack::type::tuple<INT32, JitCompilationMsgArgs>;
	using JitCompilationMsg = msgpack::type::tuple<MetadataMsg, JitCompilationMsgArgsInstance>;

	using GarbageCollectionStartMsgArgs = msgpack::type::tuple<>;
	using GarbageCollectionStartMsgArgsInstance = msgpack::type::tuple<INT32, GarbageCollectionStartMsgArgs>;
	using GarbageCollectionStartMsg = msgpack::type::tuple<MetadataMsg, GarbageCollectionStartMsgArgsInstance>;

	using GarbageCollectionFinishMsgArgs = msgpack::type::tuple<UINT64, UINT64>;
	using GarbageCollectionFinishMsgArgsInstance = msgpack::type::tuple<INT32, GarbageCollectionFinishMsgArgs>;
	using GarbageCollectionFinishMsg = msgpack::type::tuple<MetadataMsg, GarbageCollectionFinishMsgArgsInstance>;

	using ThreadCreateMsgArgs = msgpack::type::tuple<UINT64>;
	using ThreadCreateMsgArgsInstance = msgpack::type::tuple<INT32, ThreadCreateMsgArgs>;
	using ThreadCreateMsg = msgpack::type::tuple<MetadataMsg, ThreadCreateMsgArgsInstance>;

	using ThreadRenameMsgArgs = msgpack::type::tuple<UINT64, std::string>;
	using ThreadRenameMsgArgsInstance = msgpack::type::tuple<INT32, ThreadRenameMsgArgs>;
	using ThreadRenameMsg = msgpack::type::tuple<MetadataMsg, ThreadRenameMsgArgsInstance>;

	using ThreadDestroyMsgArgs = msgpack::type::tuple<UINT64>;
	using ThreadDestroyMsgArgsInstance = msgpack::type::tuple<INT32, ThreadDestroyMsgArgs>;
	using ThreadDestroyMsg = msgpack::type::tuple<MetadataMsg, ThreadDestroyMsgArgsInstance>;

	using MethodEnterMsgArgs = msgpack::type::tuple<UINT64, UINT32, USHORT>;
	using MethodEnterMsgArgsInstance = msgpack::type::tuple<INT32, MethodEnterMsgArgs>;
	using MethodEnterMsg = msgpack::type::tuple<MetadataMsg, MethodEnterMsgArgsInstance>;

	using MethodExitMsgArgs = msgpack::type::tuple<UINT64, UINT32, USHORT>;
	using MethodExitMsgArgsInstance = msgpack::type::tuple<INT32, MethodExitMsgArgs>;
	using MethodExitMsg = msgpack::type::tuple<MetadataMsg, MethodExitMsgArgsInstance>;

	using TailcallMsgArgs = msgpack::type::tuple<UINT64, UINT32>;
	using TailcallMsgArgsInstance = msgpack::type::tuple<INT32, TailcallMsgArgs>;
	using TailcallMsg = msgpack::type::tuple<MetadataMsg, TailcallMsgArgsInstance>;

	using MethodEnterWithArgumentsMsgArgs = msgpack::type::tuple<UINT64, UINT32, USHORT, std::vector<BYTE>, std::vector<BYTE>>;
	using MethodEnterWithArgumentsMsgArgsInstance = msgpack::type::tuple<INT32, MethodEnterWithArgumentsMsgArgs>;
	using MethodEnterWithArgumentsMsg = msgpack::type::tuple<MetadataMsg, MethodEnterWithArgumentsMsgArgsInstance>;

	using MethodExitWithArgumentsMsgArgs = msgpack::type::tuple<UINT64, UINT32, USHORT, std::vector<BYTE>, std::vector<BYTE>, std::vector<BYTE>>;
	using MethodExitWithArgumentsMsgArgsInstance = msgpack::type::tuple<INT32, MethodExitWithArgumentsMsgArgs>;
	using MethodExitWithArgumentsMsg = msgpack::type::tuple<MetadataMsg, MethodExitWithArgumentsMsgArgsInstance>;

	using TailcallWithArgumentsMsgArgs = msgpack::type::tuple<UINT64, UINT32, std::vector<BYTE>, std::vector<BYTE>>;
	using TailcallWithArgumentsMsgArgsInstance = msgpack::type::tuple<INT32, TailcallWithArgumentsMsgArgs>;
	using TailcallWithArgumentsMsg = msgpack::type::tuple<MetadataMsg, TailcallWithArgumentsMsgArgsInstance>;

	using AssemblyReferenceInjectionMsgArgs = msgpack::type::tuple<UINT64, UINT64>;
	using AssemblyReferenceInjectionMsgArgsInstance = msgpack::type::tuple<INT32, AssemblyReferenceInjectionMsgArgs>;
	using AssemblyReferenceInjectionMsg = msgpack::type::tuple<MetadataMsg, AssemblyReferenceInjectionMsgArgsInstance>;

	using TypeDefinitionInjectionMsgArgs = msgpack::type::tuple<UINT64, UINT32, std::string>;
	using TypeDefinitionInjectionMsgArgsInstance = msgpack::type::tuple<INT32, TypeDefinitionInjectionMsgArgs>;
	using TypeDefinitionInjectionMsg = msgpack::type::tuple<MetadataMsg, TypeDefinitionInjectionMsgArgsInstance>;

	using TypeReferenceInjectionMsgArgs = msgpack::type::tuple<UINT64, UINT64, UINT32>;
	using TypeReferenceInjectionMsgArgsInstance = msgpack::type::tuple<INT32, TypeReferenceInjectionMsgArgs>;
	using TypeReferenceInjectionMsg = msgpack::type::tuple<MetadataMsg, TypeReferenceInjectionMsgArgsInstance>;

	using MethodDefinitionInjectionMsgArgs = msgpack::type::tuple<UINT64, UINT32, UINT32, std::string>;
	using MethodDefinitionInjectionMsgArgsInstance = msgpack::type::tuple<INT32, MethodDefinitionInjectionMsgArgs>;
	using MethodDefinitionInjectionMsg = msgpack::type::tuple<MetadataMsg, MethodDefinitionInjectionMsgArgsInstance>;

	using MethodRefenceInjectionMsgArgs = msgpack::type::tuple<UINT64, std::string>;
	using MethodRefenceInjectionMsgArgsInstance = msgpack::type::tuple<INT32, MethodRefenceInjectionMsgArgs>;
	using MethodRefenceInjectionMsg = msgpack::type::tuple<MetadataMsg, MethodRefenceInjectionMsgArgsInstance>;

	using MethodWrapperInjectionMsgArgs = msgpack::type::tuple<UINT64, UINT32, UINT32, UINT32, std::string>;
	using MethodWrapperInjectionMsgArgsInstance = msgpack::type::tuple<INT32, MethodWrapperInjectionMsgArgs>;
	using MethodWrapperInjectionMsg = msgpack::type::tuple<MetadataMsg, MethodWrapperInjectionMsgArgsInstance>;

	using MethodBodyRewriteMsgArgs = msgpack::type::tuple<UINT64, UINT32>;
	using MethodBodyRewriteMsgArgsInstance = msgpack::type::tuple<INT32, MethodBodyRewriteMsgArgs>;
	using MethodBodyRewriteMsg = msgpack::type::tuple<MetadataMsg, MethodBodyRewriteMsgArgsInstance>;

	namespace Helpers
	{
		MetadataMsg CreateMetadataMsg(UINT32 pid, UINT64 tid);
		ProfilerInitializeMsg CreateProfilerInitiazeMsg(MetadataMsg&& metadataMsg);
		ProfilerLoadMsg CreateProfilerLoadMsg(MetadataMsg&& metadataMsg, UINT32 runtime, UINT32 major, UINT32 minor, UINT32 build, UINT32 qfe);
		ProfilerDestroyMsg CreateProfilerDestroyMsg(MetadataMsg&& metadataMsg);
		AssemblyLoadMsg CreateAssemblyLoadMsg(MetadataMsg&& metadataMsg, UINT64 assemblyId, const std::string& name);
		ModuleLoadMsg CreateModuleLoadMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT64 assemblyId, const std::string& name);
		TypeLoadMsg CreateTypeLoadMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef);
		JitCompilationMsg CreateJitCompilationMsg(MetadataMsg&& metadataMsg, UINT32 mdTypeDef, UINT32 mdMethodDef);
		
		GarbageCollectionStartMsg CreateGarbageCollectionStartMsg(MetadataMsg&& metadataMsg);
		GarbageCollectionFinishMsg CreateGarbageCollectionFinishMsg(MetadataMsg&& metadataMsg, UINT64 oldTrackedObjectsCount, UINT64 newTrackedObjectsCount);
		
		ThreadCreateMsg CreateThreadCreateMsg(MetadataMsg&& metadataMsg, UINT64 threadId);
		ThreadRenameMsg CreateThreadRenameMsg(MetadataMsg&& metadataMsg, UINT64 threadId, const std::string& name);
		ThreadDestroyMsg CreateThreadDestroyMsg(MetadataMsg&& metadataMsg, UINT64 threadId);
		
		MethodEnterMsg CreateMethodEnterMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation);
		MethodExitMsg CreateMethodExitMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation);
		TailcallMsg CreateTailcallrMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef);
		MethodEnterWithArgumentsMsg CreateMethodEnterWithArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT interpretation, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos);
		MethodExitWithArgumentsMsg CreateMethodExitWithArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, USHORT, std::vector<BYTE>&& returnValue, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos);
		TailcallWithArgumentsMsg CreateTailcallArgumentsMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef, std::vector<BYTE>&& argValues, std::vector<BYTE>&& argInfos);

		AssemblyReferenceInjectionMsg CreateAssemblyReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetAssemblyId, UINT64 assemblyId);
		TypeDefinitionInjectionMsg CreateTypeDefinitionInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, const std::string& name);
		TypeReferenceInjectionMsg CreateTypeReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetModuleId, UINT64 fromModuleId, UINT32 mdTypeDef);
		MethodDefinitionInjectionMsg CreateMethodDefinitionInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, UINT32 mdMethodDef, const std::string& name);
		MethodRefenceInjectionMsg CreateMethodReferenceInjectionMsg(MetadataMsg&& metadataMsg, UINT64 targetModuleId, const std::string& fullName);
		MethodWrapperInjectionMsg CreateMethodWrapperInjectionMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdTypeDef, UINT32 wrappedMethodToken, UINT32 wrapperMethodToken, const std::string& wrapperMethodName);
		MethodBodyRewriteMsg CreateMethodBodyRewriteMsg(MetadataMsg&& metadataMsg, UINT64 moduleId, UINT32 mdMethodDef);
	}
}