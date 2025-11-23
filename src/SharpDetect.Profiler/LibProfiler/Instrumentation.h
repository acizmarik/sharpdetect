// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>
#include <unordered_map>

#include "cor.h"
#include "corprof.h"

#include "ModuleDef.h"

namespace LibProfiler
{
	HRESULT PatchMethodBody(
		IN ICorProfilerInfo& corProfilerInfo,
		IN ModuleDef& moduleDef,
		IN mdMethodDef mdMethodDef,
		IN const std::unordered_map<mdToken, mdToken>& tokensToPatch);

	HRESULT CreateManagedWrapperMethod(
		IN ICorProfilerInfo& corProfilerInfo,
		IN ModuleDef& moduleDef,
		IN mdTypeDef mdTypeDef,
		IN mdMethodDef mdWrappedMethodDef,
		OUT mdMethodDef& mdWrapperMethodDef,
		OUT std::string& wrapperMethodName);
}