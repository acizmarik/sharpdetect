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

#ifndef TINYMETHODUSER_HEADER_GUARD
#define TINYMETHODUSER_HEADER_GUARD

#include <algorithm>
#include <memory>
#include "cor.h"
#include "corprof.h"
#include "ModuleMetadata.h"
#include "InstructionFactory.h"
#include "ILDefines.h"

class TinyMethodUser
{
public:

	TinyMethodUser(ModuleMetadata& moduleMetadata, WSTRING name, mdTypeDef type = mdTypeDefNil)
		: moduleMetadata(moduleMetadata), name(name), type(type), current(nullptr), code(nullptr), 
		signature(nullptr), signatureLength(0), methodAttributes(CorMethodAttr::mdStatic), 
		implementationAttributes(CorMethodImpl::miIL | CorMethodImpl::miManaged), instructionsCount(0)
	{
		// Make head of doubly-linked list
		AddInstruction(InstructionFactory::Invalid());
	}

	TinyMethodUser(const TinyMethodUser&) = delete;
	TinyMethodUser& operator= (const TinyMethodUser&) = delete;
	TinyMethodUser& operator= (TinyMethodUser&& other) = delete;
	TinyMethodUser(TinyMethodUser&& other) = delete;

	~TinyMethodUser()
	{
		// Break doubly linked list
		code->m_pPrev->m_pNext = nullptr;
		
		current = code;
		do
		{
			auto last = current;
			current = current->m_pNext;
			delete last;
		} while (current != nullptr);
	}

	HRESULT Emit(ICorProfilerInfo8& profiler, mdMethodDef& token);
	void AddInstruction(ILInstr* instruction);
	void SetName(WSTRING name) { this->name = name; }
	void SetSignature(PCCOR_SIGNATURE signature, ULONG signatureLength);
	void SetMethodFlag(CorMethodAttr flag) { methodAttributes |= flag; }
	void SetMethodImplFlag(CorMethodImpl flag) { implementationAttributes |= flag; }
	void SetAttributes(DWORD methodAttributes, DWORD implementationAttributes);

private:
	const ULONG maxCodeSize = 63;
	
	ModuleMetadata& moduleMetadata;
	WSTRING name;
	mdTypeDef type;
	ILInstr* current;
	ILInstr* code;
	PCCOR_SIGNATURE signature;
	ULONG signatureLength;
	DWORD methodAttributes;
	DWORD implementationAttributes;
	ULONG instructionsCount;
};

#endif