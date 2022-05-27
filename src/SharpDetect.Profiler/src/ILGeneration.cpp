/*
 * Copyright (C) 2020, Andrej Čižmárik
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

 // Based on project microsoftarchive/clrprofiler/ILRewrite
 // Original source: https://github.com/microsoftarchive/clrprofiler/tree/master/ILRewrite
 // Copyright (c) .NET Foundation and contributors. All rights reserved.
 // Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "Stdafx.h"
#include "ILGenerator.h"

HRESULT GenerateTinyMethod(ModuleMetadata& metadata, ILInstr* instruction, ULONG instructionsCount, LPCBYTE& code)
{
	// One instruction produces 2 + sizeof(native int) bytes in the worst case which can be 10 bytes for 64-bit.
		// For simplification we just use 10 here.
	unsigned maxSize = instructionsCount * 10;

	auto m_pOutputBuffer = new BYTE[maxSize];
	IfNullRet(m_pOutputBuffer);

again:
	BYTE * pIL = m_pOutputBuffer;

	bool fBranch = false;
	unsigned offset = 0;

	// Go over all instructions and produce code for them
	for (auto pInstr = instruction->m_pNext; pInstr != instruction; pInstr = pInstr->m_pNext)
	{
		assert(offset < maxSize);
		pInstr->m_offset = offset;

		unsigned opcode = pInstr->m_opcode;
		if (opcode < CEE_COUNT)
		{
			// CEE_PREFIX1 refers not to instruction prefixes (like tail.), but to
			// the lead byte of multi-byte opcodes. For now, the only lead byte
			// supported is CEE_PREFIX1 = 0xFE.
			if (opcode >= 0x100)
				m_pOutputBuffer[offset++] = CEE_PREFIX1;

			// This appears to depend on an implicit conversion from
			// unsigned opcode down to BYTE, to deliberately lose data and have
			// opcode >= 0x100 wrap around to 0.
			m_pOutputBuffer[offset++] = (opcode & 0xFF);
		}

		assert(pInstr->m_opcode < sizeof(s_OpCodeFlags)/sizeof(*(s_OpCodeFlags)));
		BYTE flags = s_OpCodeFlags[pInstr->m_opcode];
		switch (flags)
		{
		case 0:
			break;
		case 1:
			*(UNALIGNED INT8 *)&(pIL[offset]) = pInstr->m_Arg8;
			break;
		case 2:
			*(UNALIGNED INT16 *)&(pIL[offset]) = pInstr->m_Arg16;
			break;
		case 4:
			*(UNALIGNED INT32 *)&(pIL[offset]) = pInstr->m_Arg32;
			break;
		case 8:
			*(UNALIGNED INT64 *)&(pIL[offset]) = pInstr->m_Arg64;
			break;
		case 1 | OPCODEFLAGS_BranchTarget:
			fBranch = true;
			break;
		case 4 | OPCODEFLAGS_BranchTarget:
			fBranch = true;
			break;
		case 0 | OPCODEFLAGS_Switch:
			*(UNALIGNED INT32 *)&(pIL[offset]) = pInstr->m_Arg32;
			offset += sizeof(INT32);
			break;
		default:
			assert(false);
			break;
		}
		offset += (flags & OPCODEFLAGS_SizeMask);
	}
	instruction->m_offset = offset;

	if (fBranch)
	{
		bool fTryAgain = false;
		unsigned switchBase = 0;

		// Go over all control flow instructions and resolve the targets
		for (auto pInstr = instruction->m_pNext; pInstr != instruction; pInstr = pInstr->m_pNext)
		{
			unsigned opcode = pInstr->m_opcode;

			if (pInstr->m_opcode == CEE_SWITCH)
			{
				switchBase = pInstr->m_offset + 1 + sizeof(INT32) * (pInstr->m_Arg32 + 1);
				continue;
			}
			if (opcode == CEE_SWITCH_ARG)
			{
				// Switch args are special
				*(UNALIGNED INT32 *)&(pIL[pInstr->m_offset]) = pInstr->m_pTarget->m_offset - switchBase;
				continue;
			}

			BYTE flags = s_OpCodeFlags[pInstr->m_opcode];

			if (flags & OPCODEFLAGS_BranchTarget)
			{
				int delta = pInstr->m_pTarget->m_offset - pInstr->m_pNext->m_offset;

				switch (flags)
				{
				case 1 | OPCODEFLAGS_BranchTarget:
					// Check if delta is too big to fit into an INT8.
					// 
					// (see #pragma at top of file)
					if ((INT8)delta != delta)
					{
						if (opcode == CEE_LEAVE_S)
						{
							pInstr->m_opcode = CEE_LEAVE;
						}
						else
						{
							assert(opcode >= CEE_BR_S && opcode <= CEE_BLT_UN_S);
							pInstr->m_opcode = opcode - CEE_BR_S + CEE_BR;
							assert(pInstr->m_opcode >= CEE_BR && pInstr->m_opcode <= CEE_BLT_UN);
						}
						fTryAgain = true;
						continue;
					}
					*(UNALIGNED INT8 *)&(pIL[pInstr->m_pNext->m_offset - sizeof(INT8)]) = delta;
					break;
				case 4 | OPCODEFLAGS_BranchTarget:
					*(UNALIGNED INT32 *)&(pIL[pInstr->m_pNext->m_offset - sizeof(INT32)]) = delta;
					break;
				default:
					assert(false);
					break;
				}
			}
		}

		// Do the whole thing again if we changed the size of some branch targets
		if (fTryAgain)
			goto again;
	}

	unsigned codeSize = offset;
	unsigned totalSize;
	LPBYTE pBody = NULL;

	// Make sure we can fit in a tiny header
	if (codeSize >= 64)
		return E_FAIL;

	totalSize = sizeof(IMAGE_COR_ILMETHOD_TINY) + codeSize;
	pBody = static_cast<LPBYTE>(metadata.MethodAllocator->Alloc(totalSize));
	IfNullRet(pBody);

	BYTE * pCurrent = pBody;

	// Here's the tiny header
	*pCurrent = (BYTE)(CorILMethod_TinyFormat | (codeSize << 2));
	pCurrent += sizeof(IMAGE_COR_ILMETHOD_TINY);

	// And the body
	CopyMemory(pCurrent, m_pOutputBuffer, codeSize);
	delete[] m_pOutputBuffer;

	code = static_cast<LPCBYTE>(pBody);
	return S_OK;
}
