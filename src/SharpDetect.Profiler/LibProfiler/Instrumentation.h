// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <atomic>
#include <string>
#include <unordered_map>

#include "cor.h"
#include "corprof.h"

#include "../LibIPC/Client.h"
#include "ILRewriter.h"
#include "ModuleDef.h"

namespace LibProfiler
{
	HRESULT PatchMethodBody(
		IN ICorProfilerInfo& corProfilerInfo,
		IN LibIPC::Client& client,
		IN const ModuleDef& moduleDef,
		IN mdMethodDef mdMethodDef,
		IN const std::unordered_map<mdToken, mdToken>& tokensToPatch,
		IN const std::unordered_map<LibIPC::RecordedEventType, mdToken>& injectedMethods,
		IN BOOL enableFieldsAccessInstrumentation);

	HRESULT CreateManagedWrapperMethod(
		IN ICorProfilerInfo& corProfilerInfo,
		IN ModuleDef& moduleDef,
		IN mdTypeDef mdTypeDef,
		IN mdMethodDef mdWrappedMethodDef,
		OUT mdMethodDef& mdWrapperMethodDef,
		OUT std::string& wrapperMethodName);
}