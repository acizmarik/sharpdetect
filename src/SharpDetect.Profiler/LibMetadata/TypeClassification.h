// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <functional>
#include <memory>

#include "cor.h"
#include "corprof.h"

#include "ModuleDef.h"

namespace LibProfiler
{
	enum class GenericTypeArgKind { Reference, Value, Unknown };

	using ModuleDefResolver = std::function<std::shared_ptr<ModuleDef>(ModuleID)>;

	[[nodiscard]] GenericTypeArgKind ClassifyClass(
		ICorProfilerInfo2& corProfilerInfo,
		const ModuleDefResolver& resolveModuleDef,
		ClassID classId);
	
	[[nodiscard]] GenericTypeArgKind ClassifyClassGenericArgument(
		ICorProfilerInfo2& corProfilerInfo,
		const ModuleDefResolver& resolveModuleDef,
		FunctionID functionId,
		COR_PRF_FRAME_INFO frameInfo,
		ULONG32 typeArgIndex);
}
