// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstdlib>
#include <string>

#include "cor.h"
#include "corprof.h"

#define PROFILER_STUB EXTERN_C __declspec(dllexport) void STDMETHODCALLTYPE

#ifdef _WIN32
#define MODULE_HANDLE HMODULE
#else
#define MODULE_HANDLE PVOID
#endif

namespace LibProfiler
{
	INT PAL_GetCurrentPid();

	std::string PAL_CreateLibraryFileName(const std::string& libraryName);

	MODULE_HANDLE PAL_LoadLibrary(const std::string& libraryPath);

	void* PAL_LoadSymbolAddress(MODULE_HANDLE libraryHandle, const std::string& symbolName);
}