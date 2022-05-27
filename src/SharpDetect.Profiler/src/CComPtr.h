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

// Based on project microsoftarchive/clrprofiler/ILRewrite
// Original source: https://github.com/microsoftarchive/clrprofiler/tree/master/ILRewrite
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef CCOMPTR_HEADER_GUARD
#define CCOMPTR_HEADER_GUARD

#include <cstddef>

template<class TInterface>
class CComPtr
{
private:
	TInterface* pointer;
public:
	CComPtr(const CComPtr&) = delete;
	CComPtr& operator= (const CComPtr&) = delete;
	
	CComPtr& operator= (CComPtr&& other) noexcept
	{
		this->Reset(other.Release());
		return *this;
	}
	
	CComPtr(CComPtr&& other) noexcept
	{
		if (this->pointer != nullptr)
			this->pointer->Release();

		pointer = other.pointer;
		other.pointer = nullptr;
	}

	void* operator new(std::size_t) = delete;
	void* operator new[](std::size_t) = delete;

	void operator delete(void *ptr) = delete;
	void operator delete[](void *ptr) = delete;

	CComPtr(TInterface* pointer)
	{
		this->pointer = pointer;
	}

	TInterface* Release()
	{
		auto temp = pointer;
		pointer = nullptr;
		return temp;
	}

	void Reset(TInterface* ptr = nullptr)
	{
		if (pointer != ptr)
		{
			if (pointer != nullptr)
				pointer->Release();
			pointer = ptr;
		}
	}

	CComPtr()
	{
		this->pointer = nullptr;
	}

	~CComPtr()
	{
		if (this->pointer)
		{
			this->pointer->Release();
			this->pointer = nullptr;
		}
	}

	operator TInterface*()
	{
		return this->pointer;
	}

	operator TInterface*() const
	{
		return this->pointer;
	}

	TInterface& operator *()
	{
		return *this->pointer;
	}

	TInterface& operator *() const
	{
		return *this->pointer;
	}

	TInterface** operator&()
	{
		return &this->pointer;
	}

	TInterface** operator&() const
	{
		return &this->pointer;
	}

	TInterface* operator->()
	{
		return this->pointer;
	}

	TInterface* operator->() const
	{
		return this->pointer;
	}
};

#endif