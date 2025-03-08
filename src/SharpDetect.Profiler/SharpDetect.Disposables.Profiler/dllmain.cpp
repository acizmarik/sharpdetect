// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE.coreclr.txt file in the project root for full license information.

#include <fstream>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "../lib/loguru/loguru.hpp"
#include "../LibProfiler/ClassFactory.h"

#include "CorProfiler.h"
#include "Configuration.h"

using json = nlohmann::json;

const IID IID_IUnknown = { 0x00000000, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };
const IID IID_IClassFactory = { 0x00000001, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };
const IID IID_Profiler = { 0x54FEC423, 0x9E6C, 0x46AF, {0x94, 0x57, 0x17, 0xC9, 0x36, 0x41, 0x56, 0xA5} };

EXTERN_C BOOL STDMETHODCALLTYPE DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}

_Check_return_ EXTERN_C HRESULT STDAPICALLTYPE DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Outptr_ LPVOID * ppv)
{
    //while (!::IsDebuggerPresent())
    //    ::Sleep(100);

    if (ppv == nullptr || rclsid != IID_Profiler)
    {
        return E_FAIL;
    }

    auto rawConfigPath = std::getenv("SharpDetect_CONFIGURATION_PATH");
    if (rawConfigPath == nullptr)
    {
        LOG_F(ERROR, "Configuration path is not set.");
        return E_FAIL;
    }

    auto configPath = std::string(rawConfigPath);
    Profiler::Configuration configuration { };

    try
    {
        auto file = std::ifstream(configPath);
        auto json = json::parse(file);
        from_json(json, configuration);
    }
    catch (const std::exception& e)
    {
        LOG_F(ERROR, "Error parsing configuration from file %s. Due to error: %s.", configPath.c_str(), e.what());
        return E_FAIL;
    }

    auto factory = new LibProfiler::ClassFactory<Profiler::CorProfiler, Profiler::Configuration>(configuration);
    if (factory == nullptr)
    {
        return E_FAIL;
    }

    return factory->QueryInterface(riid, ppv);
}