// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "PAL.h"

#ifdef _WIN32

#include "windows.h"
#include "libloaderapi.h"
#include <process.h>

#elif __linux__

#include <unistd.h>
#include <dlfcn.h>

#else
#error "Unsupported or unrecognized platform!"
#endif

INT LibProfiler::PAL_GetCurrentPid()
{
#ifdef _WIN32
    return _getpid();
#else
    return getpid();
#endif
}

MODULE_HANDLE LibProfiler::PAL_LoadLibrary(const std::string& libraryPath)
{
#ifdef _WIN32
    return LoadLibraryA(libraryPath.c_str());
#else
    return dlopen(libraryPath.c_str(), RTLD_NOW);
#endif
}

void* LibProfiler::PAL_LoadSymbolAddress(MODULE_HANDLE libraryHandle, const std::string& symbolName)
{
#ifdef _WIN32
    return GetProcAddress(libraryHandle, symbolName.c_str());
#else
    return dlsym(libraryHandle, symbolName.c_str());
#endif
}