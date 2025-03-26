// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "../lib/loguru/loguru.hpp"

#include "CorProfiler.h"
#include "MethodHookInfo.h"

Profiler::CorProfiler* ProfilerInstance;

Profiler::CorProfiler::CorProfiler(Configuration configuration) :
    _configuration(configuration),
    _client(
        configuration.sharedMemoryName,
        configuration.sharedMemoryFile.value_or(std::string()),
        configuration.sharedMemorySize),
    _collectFullStackTraces(false)
{
    _terminating = false;
    ProfilerInstance = this;
}

EXTERN_C void EnterNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void LeaveNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void TailcallNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);

PROFILER_STUB EnterStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    ProfilerInstance->EnterMethod(functionId, eltInfo);
}

PROFILER_STUB LeaveStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    ProfilerInstance->LeaveMethod(functionId, eltInfo);
}

PROFILER_STUB TailcallStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo)
{
    ProfilerInstance->TailcallMethod(functionId, eltInfo);
}

void MarkAnalyzedMethod(Profiler::MethodHookInfo* methodHookInfo, void* clientData, BOOL* pbHookFunction)
{
    methodHookInfo->GetInstance = true;
    methodHookInfo->Hook = true;
    clientData = methodHookInfo;
    *pbHookFunction = true;
}

void MarkNotAnalyzedMethod(Profiler::MethodHookInfo* methodHookInfo, void* clientData, BOOL* pbHookFunction)
{
    auto collectFullStackTraces = ProfilerInstance->IsCollectFullStackTraces();
    methodHookInfo->GetInstance = false;
    methodHookInfo->Hook = collectFullStackTraces;
    clientData = methodHookInfo;
    *pbHookFunction = collectFullStackTraces;
}

BOOL IsTypeIgnored(const std::string& typeName)
{
    auto& typesToIgnore = ProfilerInstance->GetConfiguration().additionalData.typesToIgnore;
    for (auto&& item : typesToIgnore)
    {
        if (item.fullName == typeName)
            return true;
    }

    return false;
}

BOOL IsMethodIncludedInAnalysis(const std::string& typeName, const std::string& methodName)
{
    auto& methodsToInclude = ProfilerInstance->GetConfiguration().additionalData.methodsToInclude;
    for (auto&& item : methodsToInclude)
    {
        if (item.declaringTypeFullName == typeName && item.name == methodName)
            return true;
    }

    return false;
}

UINT_PTR STDMETHODCALLTYPE FunctionMapper(FunctionID functionId, void* clientData, BOOL* pbHookFunction)
{
    HRESULT hr;
    ModuleID moduleId;
    mdMethodDef mdMethodDef;
    auto& corProfilerInfo = ProfilerInstance->GetCorProfilerInfo();
    auto methodHookInfo = new Profiler::MethodHookInfo();
    methodHookInfo->FunctionId = functionId;
    auto returnValue = reinterpret_cast<UINT_PTR>(methodHookInfo);

    if (FAILED(corProfilerInfo.GetFunctionInfo(functionId, nullptr, &moduleId, &mdMethodDef)))
    {
        LOG_F(WARNING, "Could not determine information about Function ID = %" UINT_PTR_FORMAT ".", functionId);
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    if (!ProfilerInstance->HasModuleDef(moduleId))
    {
        LOG_F(WARNING, "Could not resolve Module ID = %" UINT_PTR_FORMAT " for method token = %d.", moduleId, mdMethodDef);
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    auto moduleDefPtr = ProfilerInstance->GetModuleDef(moduleId);
    auto& moduleDef = *moduleDefPtr.get();
    
    mdTypeDef typeDef;
    mdToken extendsTypeToken;
    std::string methodName;
    std::string typeName;
    PCCOR_SIGNATURE signature;
    ULONG signatureSize;
    if (FAILED(moduleDef.GetMethodProps(mdMethodDef, &typeDef, methodName, nullptr, &signature, &signatureSize)) ||
        FAILED(moduleDef.GetTypeProps(typeDef, &extendsTypeToken, typeName)))
    {
        LOG_F(ERROR, "Could not obtain methods properties for token = %d.", mdMethodDef);
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    if (IsTypeIgnored(typeName))
    {
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    auto shouldAnalyze = methodName == ".ctor" || 
                         (methodName == "Dispose" && signature[1] == 0) ||
                         IsMethodIncludedInAnalysis(typeName, methodName);

    if (!shouldAnalyze)
    {
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    std::string baseTypeName;
    if (TypeFromToken(extendsTypeToken) == mdtTypeDef && FAILED(moduleDef.GetTypeProps(extendsTypeToken, nullptr, baseTypeName)) ||
       (TypeFromToken(extendsTypeToken) == mdtTypeRef && FAILED(moduleDef.GetTypeRefProps(extendsTypeToken, nullptr, baseTypeName))))
    {
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    if (baseTypeName == "System.ValueType" || baseTypeName == "System.Enum")
    {
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    mdTypeDef disposableTypeDef;
    if (FAILED(moduleDef.FindImplementedInterface(typeDef, "System.IDisposable", &disposableTypeDef)))
    {
        MarkNotAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
        return returnValue;
    }

    LOG_F(INFO, "Hooking %s::%s.", typeName.c_str(), methodName.c_str());
    MarkAnalyzedMethod(methodHookInfo, clientData, pbHookFunction);
    return returnValue;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    HRESULT hr;

    LOG_F(INFO, "Profiler initializing in PID = %d.", LibProfiler::PAL_GetCurrentPid());
    if (FAILED(CorProfilerBase::Initialize(pICorProfilerInfoUnk)))
    {
        LOG_F(ERROR, "Could not obtain profiling API. Terminating.");
        return E_FAIL;
    }

    auto rawShouldCollectFullStackTraces = std::getenv("SharpDetect_COLLECT_FULL_STACKTRACES");
    if (rawShouldCollectFullStackTraces == nullptr)
    {
        // Default: set to false
        _collectFullStackTraces = false;
    }
    else
    {
        _collectFullStackTraces = std::stoi(rawShouldCollectFullStackTraces);
    }

    COR_PRF_RUNTIME_TYPE runtimeType;
    USHORT majorVersion;
    USHORT minorVersion;
    USHORT buildVersion;
    USHORT qfeVersion;
    hr = _corProfilerInfo->GetRuntimeInformation(
        nullptr,
        &runtimeType,
        &majorVersion,
        &minorVersion,
        &buildVersion,
        &qfeVersion,
        0,
        nullptr,
        nullptr);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not determine .NET runtime information. Terminating.");
        return E_FAIL;
    }
    _client.Send(LibIPC::Helpers::CreateProfilerLoadMsg(
        CreateMetadataMsg(),
        runtimeType,
        majorVersion,
        minorVersion,
        buildVersion,
        qfeVersion));

    auto runtimeTypeString = (runtimeType == COR_PRF_RUNTIME_TYPE::COR_PRF_DESKTOP_CLR) ? "CLR" : "CoreCLR";
    LOG_F(INFO, "Running on %s %d.%d.%d.%d.",
        runtimeTypeString,
        majorVersion,
        minorVersion,
        buildVersion,
        qfeVersion);

    hr = _corProfilerInfo->SetEventMask(_configuration.eventMask);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not set event mask. Terminating.");
        return E_FAIL;
    }

    hr = _corProfilerInfo->SetEnterLeaveFunctionHooks3WithInfo(EnterNaked, LeaveNaked, TailcallNaked);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not register enter/leave hooks. Error: 0x%x.", hr);
        return E_FAIL;
    }

    hr = _corProfilerInfo->SetFunctionIDMapper2(FunctionMapper, nullptr);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not register function ID mapper. Error: 0x%x.", hr);
        return E_FAIL;
    }

    _client.Send(LibIPC::Helpers::CreateProfilerInitiazeMsg(CreateMetadataMsg()));
    LOG_F(INFO, "Profiler initialized.");
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    if (FAILED(hrStatus) || _terminating)
        return S_OK;

    auto moduleDefPtr = std::make_shared<LibProfiler::ModuleDef>(*_corProfilerInfo);
    auto& moduleDef = *moduleDefPtr.get();
    moduleDef.Initialize(moduleId);

    {
        auto guard = std::unique_lock<std::mutex>(_modulesMutex);
        _modules.emplace(moduleId, moduleDefPtr);
    }

    LOG_F(INFO, "Loaded module: %s with handle (%" UINT_PTR_FORMAT ")", moduleDef.GetFullPath().c_str(), moduleDef.GetModuleId());
    _client.Send(LibIPC::Helpers::CreateModuleLoadMsg(CreateMetadataMsg(), moduleDef.GetModuleId(), moduleDef.GetAssemblyId(), moduleDef.GetFullPath()));

    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ThreadCreated(ThreadID threadId)
{
    if (_terminating)
        return S_OK;

    LOG_F(INFO, "Thread created %" UINT_PTR_FORMAT ".", threadId);
    _client.Send(LibIPC::Helpers::CreateThreadCreateMsg(CreateMetadataMsg(), threadId));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ThreadDestroyed(ThreadID threadId)
{
    if (_terminating)
        return S_OK;

    _client.Send(LibIPC::Helpers::CreateThreadDestroyMsg(CreateMetadataMsg(), threadId));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ThreadNameChanged(
    ThreadID threadId,
    ULONG cchName,
    WCHAR name[])
{
    if (_terminating)
        return S_OK;

    auto nameString = LibProfiler::ToString(LibProfiler::WSTRING(name, cchName));
    _client.Send(LibIPC::Helpers::CreateThreadRenameMsg(CreateMetadataMsg(), threadId, nameString));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::GarbageCollectionStarted(
    int cGenerations,
    BOOL generationCollected[],
    COR_PRF_GC_REASON reason)
{
    ULONG rangesCount;
    auto hr = _corProfilerInfo->GetGenerationBounds(0, &rangesCount, nullptr);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not obtain GC generations bounds lengths. Error: 0x%x.", hr);
        return E_FAIL;
    }

    std::vector<COR_PRF_GC_GENERATION_RANGE> ranges;
    ranges.resize(rangesCount);
    hr = _corProfilerInfo->GetGenerationBounds(rangesCount, nullptr, ranges.data());
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not obtain GC generations bounds. Error: 0x%x.", hr);
        return E_FAIL;
    }

    auto collectedGenerations = std::vector<BOOL>(generationCollected, generationCollected + cGenerations);
    _objectsTracker.ProcessGarbageCollectionStarted(std::move(collectedGenerations), std::move(ranges));
    _client.Send(LibIPC::Helpers::CreateGarbageCollectionStartMsg(CreateMetadataMsg()));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::GarbageCollectionFinished()
{
    auto oldSize = _objectsTracker.GetTrackedObjectsCount();
    auto gcContext = _objectsTracker.ProcessGarbageCollectionFinished();
    auto& nextTrackedObjectIds = gcContext.GetNextTrackedObjects();
    auto& previousTrackedObjectIds = gcContext.GetPreviousTrackedObjects();
    std::vector<LibProfiler::TrackedObjectId> removedTrackedObjectIds;
    for (auto&& previousTrackedObjectId : previousTrackedObjectIds)
    {
        if (nextTrackedObjectIds.find(previousTrackedObjectId) == nextTrackedObjectIds.cend())
            removedTrackedObjectIds.push_back(previousTrackedObjectId);
    }
    if (removedTrackedObjectIds.size() > 0)
    {
        _client.Send(LibIPC::Helpers::CreateGarbageCollectedTrackedObjectsMsg(
            CreateMetadataMsg(),
            std::move(removedTrackedObjectIds)));
    }

    auto newSize = _objectsTracker.GetTrackedObjectsCount();
    _client.Send(LibIPC::Helpers::CreateGarbageCollectionFinishMsg(CreateMetadataMsg(), oldSize, newSize));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::MovedReferences2(
    ULONG cMovedObjectIDRanges,
    ObjectID oldObjectIDRangeStart[],
    ObjectID newObjectIDRangeStart[],
    SIZE_T cObjectIDRangeLength[])
{
    _objectsTracker.ProcessMovingReferences(
        tcb::span<ObjectID>(oldObjectIDRangeStart, cMovedObjectIDRanges),
        tcb::span<ObjectID>(newObjectIDRangeStart, cMovedObjectIDRanges),
        tcb::span<SIZE_T>(cObjectIDRangeLength, cMovedObjectIDRanges));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::SurvivingReferences2(
    ULONG cSurvivingObjectIDRanges,
    ObjectID objectIDRangeStart[],
    SIZE_T cObjectIDRangeLength[])
{
    _objectsTracker.ProcessSurvivingReferences(
        tcb::span<ObjectID>(objectIDRangeStart, cSurvivingObjectIDRanges),
        tcb::span<SIZE_T>(cObjectIDRangeLength, cSurvivingObjectIDRanges));
    return S_OK;
}

HRESULT Profiler::CorProfiler::EnterMethod(FunctionIDOrClientID functionOrClientId, COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    HRESULT hr;
    MethodHookInfo* methodHookInfo = static_cast<MethodHookInfo*>((void*)functionOrClientId.clientID);
    FunctionID functionId = methodHookInfo->FunctionId;

    // Retrieve method token
    ModuleID moduleId;
    mdMethodDef methodDef;
    hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not resolve functionId %" UINT_PTR_FORMAT " to a method. Error: 0x%x", functionId, hr);
        return E_FAIL;
    }

    if (!methodHookInfo->GetInstance)
    {
        _client.Send(LibIPC::Helpers::CreateMethodEnterMsg(
            CreateMetadataMsg(),
            moduleId,
            methodDef,
            static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnter)));
        return S_OK;
    }

    // Retrieve "this" pointer
    COR_PRF_FRAME_INFO frameInfo{ };
    ULONG argumentsLength = 0;
    hr = _corProfilerInfo->GetFunctionEnter3Info(functionId, eltInfo, &frameInfo, &argumentsLength, nullptr);
    if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        LOG_F(ERROR, "Could not retrieve arguments info for method %d. Error: 0x%x", methodDef, hr);
        return E_FAIL;
    }
    auto rawArgumentInfos = std::unique_ptr<BYTE[]>(new BYTE[argumentsLength]);
    hr = _corProfilerInfo->GetFunctionEnter3Info(functionId, eltInfo, &frameInfo, &argumentsLength, (COR_PRF_FUNCTION_ARGUMENT_INFO*)(rawArgumentInfos.get()));
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not retrieve arguments data for method %d. Error: 0x%x.", methodDef, hr);
        return E_FAIL;
    }
    auto& argumentInfos = *((COR_PRF_FUNCTION_ARGUMENT_INFO*)rawArgumentInfos.get());
    

    std::vector<BYTE> argData(sizeof(ObjectID));
    std::vector<BYTE> argOffset(sizeof(UINT));
    UINT argInfo = (0 << 16 | 8);
    ObjectID objectId;
    std::memcpy(&objectId, (LPVOID)argumentInfos.ranges[0].startAddress, sizeof(ObjectID));
    auto trackedObjectId = _objectsTracker.GetTrackedObject(objectId);
    std::memcpy(argOffset.data(), &argInfo, sizeof(UINT));
    std::memcpy(argData.data(), &trackedObjectId, sizeof(ObjectID));

    // Notify about method enter with arguments
    _client.Send(LibIPC::Helpers::CreateMethodEnterWithArgumentsMsg(
        CreateMetadataMsg(),
        moduleId,
        methodDef,
        static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnterWithArguments),
        std::move(argData),
        std::move(argOffset)));
    return S_OK;
}

HRESULT Profiler::CorProfiler::LeaveMethod(FunctionIDOrClientID functionOrClientId, COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    HRESULT hr;
    MethodHookInfo* methodHookInfo = static_cast<MethodHookInfo*>((void*)functionOrClientId.clientID);
    FunctionID functionId = methodHookInfo->FunctionId;

    // Retrieve method token
    ModuleID moduleId;
    mdMethodDef methodDef;
    hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not resolve functionId %" UINT_PTR_FORMAT " to a method. Error: 0x%x", functionId, hr);
        return E_FAIL;
    }

    _client.Send(LibIPC::Helpers::CreateMethodExitMsg(
        CreateMetadataMsg(),
        moduleId,
        methodDef,
        static_cast<USHORT>(LibIPC::RecordedEventType::MethodExit)));
    return S_OK;
}

HRESULT Profiler::CorProfiler::TailcallMethod(FunctionIDOrClientID functionOrClientId, COR_PRF_ELT_INFO eltInfo)
{
    return S_OK;
}

ICorProfilerInfo8& Profiler::CorProfiler::GetCorProfilerInfo()
{
    return *_corProfilerInfo;
}

std::shared_ptr<LibProfiler::ModuleDef> Profiler::CorProfiler::GetModuleDef(ModuleID moduleId)
{
    auto guard = std::unique_lock<std::mutex>(_modulesMutex);
    return _modules.find(moduleId)->second;
}

BOOL Profiler::CorProfiler::HasModuleDef(ModuleID moduleId)
{
    auto guard = std::unique_lock<std::mutex>(_modulesMutex);
    return _modules.find(moduleId) != _modules.cend();
}

LibIPC::MetadataMsg Profiler::CorProfiler::CreateMetadataMsg()
{
    ThreadID threadId;
    _corProfilerInfo->GetCurrentThreadID(&threadId);
    return LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId);
}