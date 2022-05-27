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

#include "InstructionFactory.h"

ILInstr* InstructionFactory::Ret()
{
	auto instruction = new ILInstr();
	instruction->m_opcode = OPCODE::CEE_RET;
	return instruction;
}

ILInstr* InstructionFactory::Invalid()
{
	auto instruction = new ILInstr();
	instruction->m_opcode = OPCODE::CEE_ILLEGAL;
	return instruction;
}

ILInstr* InstructionFactory::Ldarg(int index)
{
	auto instruction = new ILInstr();
	if (index < 4)
	{
		switch (index)
		{
		case 0:
			instruction->m_opcode = OPCODE::CEE_LDARG_0;
			break;
		case 1:
			instruction->m_opcode = OPCODE::CEE_LDARG_1;
			break;
		case 2:
			instruction->m_opcode = OPCODE::CEE_LDARG_2;
			break;
		case 3:
			instruction->m_opcode = OPCODE::CEE_LDARG_3;
			break;
		}
	}
	else if (index < 256)
	{
		instruction->m_opcode = OPCODE::CEE_LDARG_S;
		instruction->m_Arg8 = static_cast<UINT8>(index);
	}
	else
	{
		instruction->m_opcode = OPCODE::CEE_LDARG;
		instruction->m_Arg16 = static_cast<UINT16>(index);
	}

	return instruction;
}

ILInstr* InstructionFactory::Call(mdToken token)
{
	auto instruction = new ILInstr();
	instruction->m_opcode = OPCODE::CEE_CALL;
	instruction->m_Arg32 = token;
	return instruction;
}

ILInstr* InstructionFactory::Jmp(mdToken token)
{
	auto instruction = new ILInstr();
	instruction->m_opcode = OPCODE::CEE_JMP;
	instruction->m_Arg32 = token;
	return instruction;
}
