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

#ifndef FUNCTIONINFO_HEADER_GUARD
#define FUNCTIONINFO_HEADER_GUARD

#include "cor.h"
#include "corprof.h"
#include <vector>
#include <stack>
#include <utility>

struct FunctionInfo
{
	ModuleID ModuleId;
	mdTypeDef ClassToken;
	mdMethodDef FunctionToken;
	std::vector<std::tuple<UINT16, UINT16, bool>> ArgumentInfos;
	size_t TotalArgumentValuesSize;
	size_t TotalIndirectArgumentValuesSize;

	bool CaptureArguments;
	bool CaptureReturnValue;

	std::stack<std::vector<BYTE*>>& GetStack()
	{
		thread_local std::stack<std::vector<BYTE*>> indirectsStack;
		return indirectsStack;
	}
};

#endif