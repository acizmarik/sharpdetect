// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <corhlpr.h>
#include <iomanip>
#include <locale>
#include <sstream>
#include <string>

namespace LibProfiler
{
    using WSTRING = std::basic_string<WCHAR, std::char_traits<WCHAR>, std::allocator<WCHAR>>;

#ifdef _WIN32
#define WSTR(value) L##value
#else
#define WSTR(value) u##value
#endif

    std::string ToString(const WSTRING& wstr);
    WSTRING ToWSTRING(const std::string& str);
}