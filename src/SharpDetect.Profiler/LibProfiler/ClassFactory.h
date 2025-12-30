// Copyright (c) .NET Foundation and contributors. All rights reserved.

#pragma once

#include "unknwn.h"
#include <algorithm>
#include <atomic>
#include <memory>
#include <utility>

namespace LibProfiler
{
    template <class TInstance, class TArg> class ClassFactory : public IClassFactory
    {
    private:
        TInstance* _ptr;
        std::atomic<int> _refCount;
        TArg _arg;

    public:
        ClassFactory(TArg arg) : _ptr(nullptr), _refCount(0), _arg(arg)
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

            _ptr = new TInstance(_arg);
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