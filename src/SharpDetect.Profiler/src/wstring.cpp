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

#include "wstring.h"

void TrimNullTerminator(WSTRING& string)
{
	if (string[string.size() - 1] == L'\0')
		string.pop_back();
}

WSTRING ToWString(const std::string& string)
{
	std::wstring_convert<std::codecvt_utf8_utf16<WCHAR>, WCHAR> convert;
	return convert.from_bytes(string);
}

std::string ToString(const WSTRING& string)
{
	std::wstring_convert<std::codecvt_utf8_utf16<WCHAR>, WCHAR> convert;
	return convert.to_bytes(string);
}

std::string ToString(BYTE* array, ULONG length)
{
	return std::string(reinterpret_cast<char*>(array), length);
}

WSTRING operator""_W(const char* arr, size_t size)
{
	return ToWString(std::string(arr, size));
}
