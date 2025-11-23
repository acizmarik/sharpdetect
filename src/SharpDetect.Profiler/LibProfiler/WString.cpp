// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "WString.h"
#include "../lib/miniutf/miniutf.hpp"

std::string LibProfiler::ToString(const WSTRING& wstr)
{
    std::u16string ustr(reinterpret_cast<const char16_t*>(wstr.c_str()), wstr.length());
    return miniutf::to_utf8(ustr);
}

LibProfiler::WSTRING LibProfiler::ToWSTRING(const std::string& str)
{
    auto ustr = miniutf::to_utf16(str);
    return WSTRING(reinterpret_cast<const WCHAR*>(ustr.c_str()));
}
