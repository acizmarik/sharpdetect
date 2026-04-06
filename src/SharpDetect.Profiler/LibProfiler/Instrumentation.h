// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <string>
#include <unordered_map>
#include <vector>

#include "cor.h"
#include "corprof.h"

#include "../LibIPC/Client.h"
#include "ILRewriter.h"
#include "ModuleDef.h"

namespace LibProfiler
{
	static constexpr COR_SIGNATURE g_ObjectTypeSignature[] = { ELEMENT_TYPE_OBJECT };

	HRESULT PatchMethodBody(
		IN ICorProfilerInfo& corProfilerInfo,
		IN LibIPC::Client& client,
		IN const ModuleDef& moduleDef,
		IN mdMethodDef mdMethodDef,
		IN const std::unordered_map<mdToken, mdToken>& tokensToPatch,
		IN const std::unordered_map<LibIPC::RecordedEventType, mdToken>& injectedMethods,
		IN BOOL enableFieldsAccessInstrumentation,
		IN const std::vector<std::string>& skipInstrumentationForAssemblies);

	HRESULT CreateManagedWrapperMethod(
		IN ICorProfilerInfo& corProfilerInfo,
		IN ModuleDef& moduleDef,
		IN mdTypeDef mdTypeDef,
		IN mdMethodDef mdWrappedMethodDef,
		OUT mdMethodDef& mdWrapperMethodDef,
		OUT std::string& wrapperMethodName);

	HRESULT InstrumentStaticFieldAccess(
		IN ICorProfilerInfo& corProfilerInfo,
		IN LibIPC::Client& client,
		IN ILRewriter& rewriter,
		IN ILInstr* currentInstruction,
		IN UINT64 instrumentationMark,
		IN const ModuleDef& moduleDef,
		IN mdMethodDef mdMethodDef,
		IN const std::unordered_map<LibIPC::RecordedEventType, mdToken>& injectedMethods,
		OUT ILInstr** nextInstruction);

	HRESULT InstrumentInstanceFieldAccess(
		IN ICorProfilerInfo& corProfilerInfo,
		IN LibIPC::Client& client,
		IN ILRewriter& rewriter,
		IN ILInstr* currentInstruction,
		IN UINT16 importedLocalsCount,
		IN std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>& addedLocals,
		IN UINT64 instrumentationMark,
		IN const ModuleDef& moduleDef,
		IN mdMethodDef mdMethodDef,
		IN const std::unordered_map<LibIPC::RecordedEventType, mdToken>& injectedMethods,
		OUT ILInstr** nextInstruction);

	HRESULT AddLocalVariables(
		IN const ModuleDef& moduleDef,
		IN ILRewriter& rewriter,
		IN PCCOR_SIGNATURE oldSignature,
		IN ULONG oldSignatureLength,
		IN const std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>& addedLocals);
}