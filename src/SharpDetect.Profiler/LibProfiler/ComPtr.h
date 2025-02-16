// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE.coreclr.txt file in the project root for full license information.

#pragma once

namespace LibProfiler
{
    template<class TInterface>
    class ComPtr
    {
    private:
        TInterface* pointer;
    public:
        ComPtr(const ComPtr&) = delete; // Copy constructor
        ComPtr& operator= (const ComPtr&) = delete; // Copy assignment
        ComPtr& operator= (ComPtr&&) = delete; // Move assignment

        void* operator new(std::size_t) = delete;
        void* operator new[](std::size_t) = delete;

        void operator delete(void* ptr) = delete;
        void operator delete[](void* ptr) = delete;

        ComPtr()
        {
            this->pointer = nullptr;
        }

        ComPtr(ComPtr&& other)
            : pointer(other.pointer)
        {
            other.pointer = nullptr;
        }

        ~ComPtr()
        {
            if (this->pointer)
            {
                this->pointer->Release();
                this->pointer = nullptr;
            }
        }

        operator TInterface* ()
        {
            return this->pointer;
        }

        operator TInterface* () const
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

        void Release()
        {
            this->~ComPtr();
        }
    };
}