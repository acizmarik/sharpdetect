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

#include "Stdafx.h"
#include "TinyMethodUser.h"
#include "ILGenerator.h"

void TinyMethodUser::AddInstruction(ILInstr* instruction)
{
	if (code == nullptr)
	{
		code = instruction;
		current = code;
		code->m_pNext = code;
		code->m_pPrev = code;
		return;
	}

	current->m_pNext = instruction;
	instruction->m_pPrev = current;
	current = instruction;
	current->m_pNext = code;
	code->m_pPrev = current;
	++instructionsCount;
}

HRESULT TinyMethodUser::Emit(ICorProfilerInfo8& profiler, mdMethodDef& token)
{
	auto body = LPCBYTE(nullptr);
	
	// Prepare code
	IfFailRet(GenerateTinyMethod(moduleMetadata, code, instructionsCount, body));
	// Generate method metadata
	IfFailRet(moduleMetadata.AddMethodDef(profiler, name, methodAttributes, type,
		signature, signatureLength, token));
	// Set method header + code
	IfFailRet(profiler.SetILFunctionBody(moduleMetadata.GetModuleId(), token, body));
	return S_OK;
}

void TinyMethodUser::SetSignature(PCCOR_SIGNATURE signature, ULONG signatureLength)
{
	this->signature = signature;
	this->signatureLength = signatureLength;
}

void TinyMethodUser::SetAttributes(DWORD methodAttributes, DWORD implementationAttributes)
{
	this->methodAttributes = methodAttributes;
	this->implementationAttributes = implementationAttributes;
}
