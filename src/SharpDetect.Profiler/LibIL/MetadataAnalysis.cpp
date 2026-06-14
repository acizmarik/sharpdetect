// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MetadataAnalysis.h"

HRESULT LibProfiler::IsVolatile(
	IN const ILInstr& instruction,
	OUT BOOL* isVolatile)
{
	*isVolatile = instruction.m_pPrev != nullptr && instruction.m_pPrev->m_opcode == CEE_VOLATILE;
	return S_OK;
}
