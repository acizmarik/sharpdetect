// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "unknwn.h"
#include <atomic>

namespace LibProfiler
{
    template <class T> class ClassFactory : public IClassFactory
    {
    private:
        T* _ptr;
        std::atomic<int> _refCount;
    public:
        ClassFactory() : _ptr(nullptr), _refCount(0)
        {
        }

        ~ClassFactory()
        {
        }

        HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override
        {
            if (riid == IID_IUnknown || riid == IID_IClassFactory)
            {
                *ppvObject = this;
                this->AddRef();
                return S_OK;
            }

            *ppvObject = nullptr;
            return E_NOINTERFACE;
        }

        ULONG STDMETHODCALLTYPE AddRef() override
        {
            return std::atomic_fetch_add(&this->_refCount, 1) + 1;
        }

        ULONG STDMETHODCALLTYPE Release() override
        {
            int count = std::atomic_fetch_sub(&this->_refCount, 1) - 1;
            if (count <= 0)
            {
                delete this;
            }

            return count;
        }

        HRESULT STDMETHODCALLTYPE CreateInstance(IUnknown* pUnkOuter, REFIID riid, void** ppvObject) override
        {
            if (pUnkOuter != nullptr)
            {
                *ppvObject = nullptr;
                return CLASS_E_NOAGGREGATION;
            }

            _ptr = new T();
            if (_ptr == nullptr)
            {
                return E_FAIL;
            }

            return _ptr->QueryInterface(riid, ppvObject);
        }

        HRESULT STDMETHODCALLTYPE LockServer(BOOL fLock) override
        {
            return S_OK;
        }
    };
}