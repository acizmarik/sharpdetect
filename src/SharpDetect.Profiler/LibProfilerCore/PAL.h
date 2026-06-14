// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstdlib>
#include <string>

#include "cor.h"
#include "corprof.h"

#ifdef _WIN32
#define PROFILER_STUB EXTERN_C __declspec(dllexport) void STDMETHODCALLTYPE
#define MODULE_HANDLE HMODULE
#define UINT_PTR_FORMAT "llx"
#define ULONG_FORMAT "ld"
#define SIZE_FORMAT "lld"
#else
#include <cstdlib>
#define PROFILER_STUB EXTERN_C __attribute__((visibility("hidden"))) void STDMETHODCALLTYPE
#define MODULE_HANDLE PVOID
#define UINT_PTR_FORMAT "lx"
#define ULONG_FORMAT "u"
#define SIZE_FORMAT "zu"
#endif

namespace LibProfiler
{
	INT PAL_GetCurrentPid();

	MODULE_HANDLE PAL_LoadLibrary(const std::string& libraryPath);

	void* PAL_LoadSymbolAddress(MODULE_HANDLE libraryHandle, const std::string& symbolName);
}