// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "cor.h"

#ifdef _WIN32
#include <windows.h>
#else
#include <semaphore.h>
#endif

namespace LibIPC
{
#ifdef _WIN32
	using SemaphoreHandle = HANDLE;
	inline constexpr SemaphoreHandle InvalidSemaphore = nullptr;
#else
	using SemaphoreHandle = sem_t*;
	inline const SemaphoreHandle InvalidSemaphore = SEM_FAILED;
#endif
	
	SemaphoreHandle Semaphore_Open(const std::string& name);
	void Semaphore_Post(SemaphoreHandle sem);
	bool Semaphore_TimedWait(SemaphoreHandle sem, int timeoutMs);
	void Semaphore_Close(SemaphoreHandle sem);
}
