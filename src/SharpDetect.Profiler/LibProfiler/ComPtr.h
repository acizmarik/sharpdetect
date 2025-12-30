// Copyright (c) .NET Foundation and contributors. All rights reserved.

#pragma once

#include <concepts>

namespace LibProfiler
{
    template<typename T>
    concept ComInterface = requires(T* ptr) {
        { ptr->Release() } -> std::convertible_to<unsigned long>;
    };

    template<ComInterface TInterface>
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

        ComPtr() : pointer(nullptr)
        {
        }

        ComPtr(ComPtr&& other) noexcept
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

        explicit operator TInterface* ()
        {
            return this->pointer;
        }

        explicit operator TInterface* () const
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