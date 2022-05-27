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

#ifndef WSTRING_HEADER_GUARD
#define WSTRING_HEADER_GUARD

#include "cor.h"
#include "corprof.h"
#include <string>
#include <sstream>
#include <codecvt>
#include <locale>

using WSTRINGSTREAM = std::basic_stringstream<WCHAR>;
using WSTRING = std::basic_string<WCHAR>;

void TrimNullTerminator(WSTRING& string);

WSTRING ToWString(const std::string& string);
std::string ToString(const WSTRING& string);
std::string ToString(BYTE* array, ULONG length);

WSTRING operator "" _W(const char* arr, size_t size);

#endif