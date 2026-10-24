// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MetadataAnalysis.h"
#include "WString.h"

static bool IsPrefix(const ILInstr* instruction)
{
	if (instruction == nullptr)
		return false;

	switch (instruction->m_opcode)
	{
		case CEE_UNALIGNED:
		case CEE_VOLATILE:
		case CEE_TAILCALL:
		case CEE_CONSTRAINED:
		case CEE_READONLY:
			return true;
		default:
			return false;
	}
}

ILInstr* LibProfiler::GetFirstPrefix(
	IN ILInstr& instruction)
{
	auto first = &instruction;
	while (IsPrefix(first->m_pPrev))
		first = first->m_pPrev;

	return first;
}

HRESULT LibProfiler::IsVolatile(
	IN const ILInstr& instruction,
	OUT BOOL* isVolatile)
{
	*isVolatile = FALSE;
	for (auto prefix = instruction.m_pPrev; IsPrefix(prefix); prefix = prefix->m_pPrev)
	{
		if (prefix->m_opcode == CEE_VOLATILE)
			*isVolatile = TRUE;
	}

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

	if ((IsFdInitOnly(attributes) && !IsFdStatic(attributes)) || IsFdLiteral(attributes))
	{
		*isExcluded = TRUE;
		return S_OK;
	}
	
	if (!IsFdStatic(attributes))
		return S_OK;

	return moduleDef.HasCustomAttribute(fieldToken, WSTR("System.ThreadStaticAttribute"), isExcluded);
}
