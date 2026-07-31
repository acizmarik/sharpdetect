// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"
#include "ILRewriter.h"
#include "ModuleDef.h"

namespace LibProfiler
{
	HRESULT IsVolatile(
		IN const ILInstr& instruction,
		OUT BOOL* isVolatile);

	HRESULT IsFieldExcludedFromRaceAnalysis(
		IN const ModuleDef& moduleDef,
		IN mdToken fieldToken,
		OUT BOOL* isExcluded);
}
