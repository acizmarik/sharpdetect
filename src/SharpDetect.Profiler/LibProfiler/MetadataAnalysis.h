// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "cor.h"
#include "ILRewriter.h"

#include "ModuleDef.h"

namespace LibProfiler
{
	HRESULT IsValueType(
		IN const ModuleDef& moduleDef,
		IN mdTypeDef typeDef,
		OUT BOOL* isValueType);

	HRESULT IsVolatile(
		IN const ILInstr& instruction,
		OUT BOOL* isVolatile);

	HRESULT AnalyzeFieldAccess(
		IN const ModuleDef& moduleDef,
		IN mdToken fieldToken,
		IN const ILInstr& instruction,
		OUT PCCOR_SIGNATURE* signature,
		OUT ULONG* signatureLength,
		OUT BOOL* isDeclaringTypeValueType,
		OUT BOOL* isVolatile);
}
