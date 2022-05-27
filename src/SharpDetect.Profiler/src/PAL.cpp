#include "Stdafx.h"
#include "PAL.h"

#ifdef WIN32
#include <Windows.h>
#elif LINUX

#else
#error "Unsupported platform (expected WIN32 or LINUX)"
#endif

int32_t PAL::GetProcessId()
{
#ifdef WIN32
	return static_cast<int32_t>(::GetCurrentProcessId());
#else
	return getpid();
#endif
}

std::string PAL::ReadEnvironmentVariable(const std::string& name)
{
#ifdef WIN32
	const size_t maxLength = 1024;
	char buffer[maxLength];
	if (GetEnvironmentVariableA(name.c_str(), buffer, maxLength))
	{
		// Successfully read environment value
		return std::string(buffer);
	}
	else
	{
		// For some reason we were unable to read the value
		return { };
	}
#else
	return ::getenv(name);
#endif
}
