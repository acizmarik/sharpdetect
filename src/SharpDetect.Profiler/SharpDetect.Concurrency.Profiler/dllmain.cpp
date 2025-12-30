// Copyright (c) .NET Foundation and contributors. All rights reserved.

#include <fstream>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "../lib/loguru/loguru.hpp"
#include "../LibProfiler/ClassFactory.h"

#include "CorProfiler.h"

using json = nlohmann::json;

const IID IID_IUnknown = { 0x00000000, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };
const IID IID_IClassFactory = { 0x00000001, 0x0000, 0x0000, { 0xC0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 } };
constexpr IID IID_Profiler = { 0xB2C60596, 0xB36D, 0x460B, {0x90, 0x2A, 0x3D, 0x91, 0xF5, 0x87, 0x85, 0x29} };

EXTERN_C BOOL STDMETHODCALLTYPE DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    return TRUE;
}

_Check_return_ EXTERN_C HRESULT STDAPICALLTYPE DllGetClassObject(_In_ REFCLSID rclsid, _In_ REFIID riid, _Outptr_ LPVOID * ppv)
{
    if (ppv == nullptr || rclsid != IID_Profiler)
    {
        return E_FAIL;
    }

    auto rawLoguruLevel = std::getenv("SharpDetect_LOG_LEVEL");
    if (rawLoguruLevel != nullptr)
    {
        auto logLevel = std::string(rawLoguruLevel);
        loguru::g_stderr_verbosity = std::stoi(logLevel);
    }
    else
    {
        loguru::g_stderr_verbosity = loguru::Verbosity_WARNING;
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
    return factory->QueryInterface(riid, ppv);
}