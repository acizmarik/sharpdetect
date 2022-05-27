#ifndef PAL_HEADER_GUARD
#define PAL_HEADER_GUARD

#include <string>

class PAL final
{
public:
	static int32_t GetProcessId();
	static std::string ReadEnvironmentVariable(const std::string& name);
};

#endif