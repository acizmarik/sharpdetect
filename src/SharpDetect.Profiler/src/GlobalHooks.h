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

#ifndef GLOBALHOOKS_HEADER_GUARD
#define GLOBALHOOKS_HEADER_GUARD

#include <cstdint>
#include "cor.h"
#include "corprof.h"
#include "profiler_pal.h"
#include "FunctionInfo.h"
#include "MessageFactory.h"
#include "CorProfilerBase.h"

PROFILER_STUB EnterStub(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo)
{
	CorProfilerBase::GetInstance()->EnterMethod(functionIDOrClientID, eltInfo);
}

PROFILER_STUB LeaveStub(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo)
{
	CorProfilerBase::GetInstance()->LeaveMethod(functionIDOrClientID, eltInfo);
}

PROFILER_STUB TailcallStub(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo)
{
	CorProfilerBase::GetInstance()->TailcallMethod(functionIDOrClientID, eltInfo);
}

UINT_PTR STDMETHODCALLTYPE FunctionIdMapper2(FunctionID functionId, void* clientData, BOOL* pbHookFunction)
{
	const FunctionInfo* info = nullptr; 
	auto instance = CorProfilerBase::GetInstance();

	// If there is not a record, do not hook the function
	if ((info = instance->TryGetHookData(functionId)) != nullptr)
	{
		*pbHookFunction = true;
		return reinterpret_cast<UINT_PTR>(info);
	}
	// Otherwise hook it and point to the helper structure
	else
	{
		*pbHookFunction = false;
		return functionId;
	}
}

#if UINTPTR_MAX == UINT64_MAX

EXTERN_C void EnterNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void LeaveNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void TailcallNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);

#elif UINTPTR_MAX == UINT32_MAX

void __declspec(naked) EnterNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo)
{
	__asm
	{
		PUSH EAX
		PUSH ECX
		PUSH EDX
		PUSH[ESP + 16]
		CALL EnterStub
		POP EDX
		POP ECX
		POP EAX
		RET 8
	}
}

void __declspec(naked) LeaveNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo)
{
	__asm
	{
		PUSH EAX
		PUSH ECX
		PUSH EDX
		PUSH[ESP + 16]
		CALL LeaveStub
		POP EDX
		POP ECX
		POP EAX
		RET 8
	}
}

void __declspec(naked) TailcallNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo)
{
	__asm
	{
		PUSH EAX
		PUSH ECX
		PUSH EDX
		PUSH[ESP + 16]
		CALL TailEnterStub
		POP EDX
		POP ECX
		POP EAX
		RET 8
	}
}

#else

#error Unknown pointer size or missing size macro definitions!

#endif

#endif