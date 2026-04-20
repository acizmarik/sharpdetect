// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "Semaphore.h"

#ifdef _WIN32
#include <windows.h>
#else
#include <semaphore.h>
#include <time.h>
#endif

LibIPC::SemaphoreHandle LibIPC::Semaphore_Open(const std::string& name)
{
#ifdef _WIN32
	return OpenSemaphoreA(SEMAPHORE_ALL_ACCESS, FALSE, name.c_str());
#else
	const auto handle = sem_open(name.c_str(), 0);
	return handle == SEM_FAILED ? InvalidSemaphore : handle;
#endif
}

void LibIPC::Semaphore_Post(SemaphoreHandle sem)
{
#ifdef _WIN32
	ReleaseSemaphore(sem, 1, nullptr);
#else
	sem_post(sem);
#endif
}

bool LibIPC::Semaphore_TimedWait(SemaphoreHandle sem, int timeoutMs)
{
#ifdef _WIN32
	return WaitForSingleObject(sem, static_cast<DWORD>(timeoutMs)) == WAIT_OBJECT_0;
#else
	struct timespec deadline;
	clock_gettime(CLOCK_REALTIME, &deadline);
	deadline.tv_sec += timeoutMs / 1000;
	deadline.tv_nsec += (timeoutMs % 1000) * 1000000L;
	if (deadline.tv_nsec >= 1000000000L)
	{
		deadline.tv_sec++;
		deadline.tv_nsec -= 1000000000L;
	}
	return sem_timedwait(sem, &deadline) == 0;
#endif
}

void LibIPC::Semaphore_Close(SemaphoreHandle sem)
{
#ifdef _WIN32
	if (sem != nullptr)
		CloseHandle(sem);
#else
	if (sem != SEM_FAILED)
		sem_close(sem);
#endif
}
