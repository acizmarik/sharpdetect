// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MetadataAnalysis.h"
#include "WString.h"

HRESULT LibProfiler::IsVolatile(
	IN const ILInstr& instruction,
	OUT BOOL* isVolatile)
{
	*isVolatile = instruction.m_pPrev != nullptr && instruction.m_pPrev->m_opcode == CEE_VOLATILE;
	return S_OK;
}

HRESULT LibProfiler::IsFieldExcludedFromRaceAnalysis(
	IN const ModuleDef& moduleDef,
	IN const mdToken fieldToken,
	OUT BOOL* isExcluded)
{
	*isExcluded = FALSE;

	// A member reference cannot be resolved to its definition without crossing module boundaries
	if (TypeFromToken(fieldToken) != mdtFieldDef)
		return S_OK;

	DWORD attributes;
	HRESULT hr = moduleDef.GetFieldAttributes(fieldToken, &attributes);
	if (FAILED(hr))
		return hr;

	if (IsFdInitOnly(attributes) || IsFdLiteral(attributes))
	{
		*isExcluded = TRUE;
		return S_OK;
	}
	
	if (!IsFdStatic(attributes))
		return S_OK;

	return moduleDef.HasCustomAttribute(fieldToken, WSTR("System.ThreadStaticAttribute"), isExcluded);
}
