// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>
#include <cstdlib>
#include <cstring>
#include <filesystem>
#include <fstream>
#include <memory>
#include <numeric>
#include <sstream>
#include <stack>
#include <string>
#include <utility>
#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "../lib/loguru/loguru.hpp"

#include "CorProfiler.h"
#include "../LibIPC/Client.h"
#include "../LibIPC/Messages.h"
#include "../LibProfiler/AssemblyDef.h"
#include "../LibProfiler/ComPtr.h"
#include "../LibProfiler/ModuleDef.h"
#include "../LibProfiler/OpCodes.h"
#include "../LibProfiler/Instrumentation.h"
#include "../LibProfiler/PAL.h"
#include "../LibProfiler/WString.h"

using json = nlohmann::json;

Profiler::CorProfiler* ProfilerInstance;
thread_local std::stack<std::vector<UINT_PTR>> ArgsCallStack;

Profiler::CorProfiler::CorProfiler() : 
    _client(LibIPC::Client("SharpDetect_Run_SharedMemory.data", "", 2000000)),
    _coreModule(0)
{
    ProfilerInstance = this;
}

EXTERN_C void EnterNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void LeaveNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void TailcallNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);

UINT_PTR STDMETHODCALLTYPE FunctionMapper(FunctionID funcId, void* clientData, BOOL* pbHookFunction)
{
    if (ProfilerInstance->ShouldCollectFullStackTraces())
    {
        // Inject hooks for all methods
        *pbHookFunction = true;
    }
    else
    {
        // Inject hooks only for methods where we explicitly requested them
        auto const descriptor = ProfilerInstance->FindMethodDescriptor(funcId);
        auto const shouldInjectHooks = descriptor != nullptr && descriptor->rewritingDescriptor.injectHooks;
        *pbHookFunction = shouldInjectHooks;
    }

    return funcId;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    LOG_F(INFO, "Profiler initializing in PID = %d.", LibProfiler::PAL_GetCurrentPid());

    if (FAILED(CorProfilerBase::Initialize(pICorProfilerInfoUnk)))
    {
        LOG_F(ERROR, "Could not obtain profiling API. Terminating.");
        return E_FAIL;
    }
    
    if (FAILED(LoadRewritingConfiguration()))
    {
        LOG_F(ERROR, "Could not obtain configuration. Terminating.");
        return E_FAIL;
    }

    COR_PRF_RUNTIME_TYPE runtimeType;
    USHORT majorVersion;
    USHORT minorVersion;
    USHORT buildVersion;
    USHORT qfeVersion;
    if (FAILED(_corProfilerInfo->GetRuntimeInformation(
        nullptr, 
        &runtimeType, 
        &majorVersion, 
        &minorVersion, 
        &buildVersion, 
        &qfeVersion, 
        0, 
        nullptr, 
        nullptr)))
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

    if (FAILED(InitializeProfilingFeatures()))
    {
        LOG_F(ERROR, "Could not initialize requested profiling features. Terminating.");
        return E_FAIL;
    }

    LibProfiler::OpCodes::Initialize();
    LOG_F(INFO, "Initialized IL descriptors.");

    _client.Send(LibIPC::Helpers::CreateProfilerInitiazeMsg(CreateMetadataMsg()));
    LOG_F(INFO, "Profiler initialized.");
    return S_OK;
}

HRESULT Profiler::CorProfiler::LoadRewritingConfiguration()
{
    auto rawRewritingConfigPath = std::getenv("SHARPDETECT_REWRITING_CONFIGURATION_FILE_PATH");
    if (rawRewritingConfigPath == nullptr)
    {
        LOG_F(ERROR, "Rewriting configuration path is not set.");
        return E_FAIL;
    }

    auto rewritingConfigPath = std::string(rawRewritingConfigPath);
    if (!std::filesystem::exists(rewritingConfigPath))
    {
        LOG_F(ERROR, "Rewriting configuration path %s does not exist.", rewritingConfigPath.c_str());
        return E_FAIL;
    }

    auto rawShouldCollectFullStackTraces = std::getenv("SHARPDETECT_COLLECT_FULL_STACKTRACES");
    if (rawShouldCollectFullStackTraces == nullptr)
    {
        // Default: set to false
        _collectFullStackTraces = false;
    }
    else
    {
        _collectFullStackTraces = std::stoi(rawShouldCollectFullStackTraces);
    }

    try
    {
        auto file = std::ifstream(rewritingConfigPath);
        auto json = json::parse(file);
        std::vector<MethodDescriptor> methodDescriptors;
        from_json(json, methodDescriptors);
        for (auto&& methodDescriptor : methodDescriptors)
            _methodDescriptors.push_back(std::make_shared<MethodDescriptor>(std::move(methodDescriptor)));
    }
    catch (const std::exception& e)
    {
        LOG_F(ERROR, "Error parsing rewriting configuration from file %s. Due to error: %s.", rewritingConfigPath.c_str(), e.what());
        return E_FAIL;
    }

    return S_OK;
}

HRESULT Profiler::CorProfiler::InitializeProfilingFeatures()
{
    auto rawProfilerEventMask = std::getenv("SHARPDETECT_PROF_EVENTMASK");
    if (rawProfilerEventMask == nullptr)
    {
        LOG_F(ERROR, "Event mask is not set.");
        return E_FAIL;
    }

    auto profilerEventMask = static_cast<COR_PRF_MONITOR>(std::stoll(std::string(rawProfilerEventMask)));
    auto hr = _corProfilerInfo->SetEventMask(profilerEventMask);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not set profiling flags. Error: 0x%x.", hr);
        return E_FAIL;
    }

    if ((profilerEventMask & COR_PRF_MONITOR::COR_PRF_MONITOR_ENTERLEAVE) != 0)
    {
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
    }

    return S_OK;
}

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

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ThreadCreated(ThreadID threadId)
{
    if (_terminating)
        return S_OK;

    LOG_F(INFO, "Thread created %lld.", threadId);
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
    _objectsTracker.ProcessGarbageCollectionFinished();
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
        std::span<ObjectID>(oldObjectIDRangeStart, cMovedObjectIDRanges),
        std::span<ObjectID>(newObjectIDRangeStart, cMovedObjectIDRanges),
        std::span<SIZE_T>(cObjectIDRangeLength, cMovedObjectIDRanges));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::SurvivingReferences2(
    ULONG cSurvivingObjectIDRanges, 
    ObjectID objectIDRangeStart[],
    SIZE_T cObjectIDRangeLength[])
{
    _objectsTracker.ProcessSurvivingReferences(
        std::span<ObjectID>(objectIDRangeStart, cSurvivingObjectIDRanges), 
        std::span<SIZE_T>(cObjectIDRangeLength, cSurvivingObjectIDRanges));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
    if (_terminating)
        return S_OK;

    ModuleID moduleId;
    mdMethodDef mdMethodDef;
    if (FAILED(_corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &mdMethodDef)))
    {
        LOG_F(ERROR, "Could not determine information about Function ID = %lld.", functionId);
        return E_FAIL;
    }

    if (!HasModuleDef(moduleId))
    {
        LOG_F(ERROR, "Could not resolve Module ID = %lld for method TOK = %d.", moduleId, mdMethodDef);
        return E_FAIL;
    }
    auto moduleDefPtr = GetModuleDef(moduleId);
    auto& moduleDef = *moduleDefPtr.get();

    mdTypeDef mdTypeDef;
    std::string methodName;
    std::string typeName;
    CorMethodAttr methodFlags;
    if (FAILED(moduleDef.GetMethodProps(mdMethodDef, &mdTypeDef, methodName, &methodFlags, nullptr, nullptr)) ||
        FAILED(moduleDef.GetTypeProps(mdTypeDef, typeName)))
    {
        LOG_F(ERROR, "Could not obtain methods properties for TOK = %d.", mdMethodDef);
        return E_FAIL;
    }

    PatchMethodBody(moduleDef, mdTypeDef, mdMethodDef);
    _client.Send(LibIPC::Helpers::CreateJitCompilationMsg(CreateMetadataMsg(), mdTypeDef, mdMethodDef));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
    if (FAILED(hrStatus) || _terminating)
        return S_OK;

    auto moduleDefPtr = std::make_shared<LibProfiler::ModuleDef>(*_corProfilerInfo);
    auto assemblyDefPtr = std::make_shared<LibProfiler::AssemblyDef>(*_corProfilerInfo);
    auto& moduleDef = *moduleDefPtr.get();
    auto& assemblyDef = *assemblyDefPtr.get();
    moduleDef.Initialize(moduleId);
    assemblyDef.Initialize(moduleId);
    auto const assemblyId = assemblyDef.GetAssemblyId();

    {
        auto guard = std::unique_lock<std::shared_mutex>(_assembliesAndModulesSharedMutex);
        // FIXME: modules and assemblies are not always 1:1 mapped (assembly can contain multiple modules)
        _modules.emplace(moduleId, moduleDefPtr);
        _assemblies.emplace(assemblyId, assemblyDefPtr);
    }

    if (_coreModule == 0)
    {
        _coreModule = moduleDef.GetModuleId();
        LOG_F(INFO, "Identified core module: %s with handle (%lld)", moduleDef.GetName().c_str(), moduleDef.GetModuleId());
    }

    LOG_F(INFO, "Loaded assembly: %s with handle (%lld)", assemblyDef.GetName().c_str(), assemblyDef.GetAssemblyId());
    LOG_F(INFO, "Loaded module: %s with handle (%lld)", moduleDef.GetFullPath().c_str(), moduleDef.GetModuleId());
    
    WrapAnalyzedExternMethods(moduleDef);
    ImportMethodWrappers(assemblyDef, moduleDef);
    ImportCustomRecordedEventTypes(moduleDef);

    _client.Send(LibIPC::Helpers::CreateAssemblyLoadMsg(CreateMetadataMsg(), assemblyDef.GetAssemblyId(), assemblyDef.GetName()));
    _client.Send(LibIPC::Helpers::CreateModuleLoadMsg(CreateMetadataMsg(), moduleDef.GetModuleId(), assemblyDef.GetAssemblyId(), moduleDef.GetFullPath()));

    return S_OK;
}

static std::vector<COR_SIGNATURE> SerializeMethodSignatureDescriptor(const Profiler::MethodSignatureDescriptor& descriptor)
{
    const INT signatureLength = 3 * sizeof(BYTE) + descriptor.argumentTypeElements.size() * sizeof(BYTE);
    std::vector<COR_SIGNATURE> result;
    result.resize(signatureLength);
    auto signaturePointer = 0;

    result[signaturePointer++] = descriptor.callingConvention;
    result[signaturePointer++] = descriptor.parametersCount;
    result[signaturePointer++] = descriptor.returnType;
    for (auto&& elementType : descriptor.argumentTypeElements)
        result[signaturePointer++] = elementType;

    return result;
}

HRESULT Profiler::CorProfiler::WrapAnalyzedExternMethods(LibProfiler::ModuleDef& moduleDef)
{
    std::unordered_map<mdToken, mdToken> rewritingsBuilder;
    
    {
        auto guard = std::shared_lock<std::shared_mutex>(_rewritingsSharedMutex);
        for (auto&& methodPtr : _methodDescriptors)
        {
            auto& method = *methodPtr.get();

            // Method should be marked for wrapper injection in order to continue
            if (!method.rewritingDescriptor.injectManagedWrapper)
                continue;

            mdTypeDef typeDef;
            auto hr = moduleDef.FindTypeDef(method.declaringTypeFullName, &typeDef);
            if (FAILED(hr))
                continue;

            mdMethodDef methodDef;
            auto methodSignature = SerializeMethodSignatureDescriptor(method.signatureDescriptor);
            hr = moduleDef.FindMethodDef(
                method.methodName,
                methodSignature.data(),
                methodSignature.size(),
                typeDef,
                &methodDef);
            if (FAILED(hr))
                continue;

            mdMethodDef wrapperMethodDef;
            std::string wrapperMethodName;
            hr = LibProfiler::CreateManagedWrapperMethod(
                *_corProfilerInfo,
                moduleDef,
                typeDef,
                methodDef,
                wrapperMethodDef,
                wrapperMethodName);
            if (FAILED(hr))
            {
                LOG_F(ERROR, "Could not inject managed method wrapper for method %s in type %s in module %s. Error: 0x%x.",
                    method.methodName.c_str(),
                    method.declaringTypeFullName.c_str(),
                    moduleDef.GetName().c_str(),
                    hr);

                return E_FAIL;
            }

            rewritingsBuilder.emplace(methodDef, wrapperMethodDef);
            {
                auto guard = std::unique_lock<std::shared_mutex>(_wrappersSharedMutex);
                _wrappers.emplace(std::make_pair(moduleDef.GetModuleId(), wrapperMethodDef), true);
            }

            _client.Send(LibIPC::Helpers::CreateMethodWrapperInjectionMsg(CreateMetadataMsg(), moduleDef.GetModuleId(), typeDef, methodDef, wrapperMethodDef, wrapperMethodName));

            LOG_F(INFO, "Wrapped %s::%s (%d) -> (%d) in module %s.",
                method.declaringTypeFullName.c_str(),
                method.methodName.c_str(), methodDef,
                wrapperMethodDef,
                moduleDef.GetName().c_str());
        }
    }
    
    auto guard = std::unique_lock<std::shared_mutex>(_rewritingsSharedMutex);
    _rewritings.emplace(moduleDef.GetModuleId(), rewritingsBuilder);
    return S_OK;
}

HRESULT Profiler::CorProfiler::ImportMethodWrappers(LibProfiler::AssemblyDef& assemblyDef, LibProfiler::ModuleDef& moduleDef)
{
    auto guard = std::shared_lock<std::shared_mutex>(_methodDescriptorsSharedMutex);
    for (auto&& methodPointer : _methodDescriptors)
    {
        auto& method = *methodPointer.get();
        if (!method.rewritingDescriptor.injectManagedWrapper)
            continue;

        auto& originalReferences = assemblyDef.GetOriginalReferences();
        for (auto&& assemblyRef : originalReferences)
            ImportMethodWrapper(moduleDef, assemblyRef, method);
    }

    return S_OK;
}

HRESULT Profiler::CorProfiler::ImportMethodWrapper(
    LibProfiler::ModuleDef& moduleDef,
    const LibProfiler::AssemblyRef& assemblyRef, 
    const MethodDescriptor& method)
{
    // Try to find reference to the declaring type
    mdTypeRef typeRef;
    auto hr = moduleDef.FindTypeRef(
        assemblyRef.GetMdAssemblyRef(),
        method.declaringTypeFullName,
        &typeRef);
    if (FAILED(hr))
        return E_FAIL;
        
    // Try to find reference to the wrapped method
    mdMemberRef methodRef;
    auto const signature = SerializeMethodSignatureDescriptor(method.signatureDescriptor);
    hr = moduleDef.FindMethodRef(
        method.methodName,
        signature.data(),
        signature.size(),
        typeRef,
        &methodRef);
    if (FAILED(hr))
        return E_FAIL;

    // Create reference to the wrapper method
    mdMemberRef wrapperMethodRef;
    hr = moduleDef.AddMethodRef(
        "." + method.methodName,
        typeRef,
        signature.data(),
        signature.size(),
        &wrapperMethodRef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not import wrapper for %s. Error: 0x%x.", method.declaringTypeFullName.c_str(), hr);
        return E_FAIL;
    }
    
    // Store mapping
    {
        auto guard = std::unique_lock<std::shared_mutex>(_rewritingsSharedMutex);
        _rewritings.at(moduleDef.GetModuleId()).emplace(methodRef, wrapperMethodRef);
    }

    _client.Send(LibIPC::Helpers::CreateMethodReferenceInjectionMsg(
        CreateMetadataMsg(),
        moduleDef.GetModuleId(),
        method.declaringTypeFullName + "::" + method.methodName));

    LOG_F(INFO, "Imported %s::.%s for module %s",
        method.declaringTypeFullName.c_str(),
        method.methodName.c_str(),
        moduleDef.GetName().c_str());

    return S_OK;
}

void Profiler::CorProfiler::AddCustomEventMapping(
    CustomEventsLookup& lookup,
    ModuleID moduleId,
    mdMethodDef methodDef,
    USHORT original,
    USHORT mapping)
{
    if (static_cast<RecordedEventType>(mapping) == RecordedEventType::NotSpecified)
        return;

    auto guard = std::unique_lock<std::shared_mutex>(_customEventLookupsSharedMutex);
    lookup.emplace(std::make_tuple(moduleId, methodDef, original), mapping);
}

HRESULT Profiler::CorProfiler::ImportCustomRecordedEventTypes(LibProfiler::ModuleDef& moduleDef)
{
    auto guard = std::shared_lock<std::shared_mutex>(_rewritingsSharedMutex);
    auto& moduleRewritings = (*_rewritings.find(moduleDef.GetModuleId())).second;

    for (auto&& methodPointer : _methodDescriptors)
    {
        auto& method = *methodPointer.get();
        HRESULT hr;
        
        // Get declaring type
        mdTypeDef typeDef;
        hr = moduleDef.FindTypeDef(method.declaringTypeFullName, &typeDef);
        if (FAILED(hr))
            continue;

        // Get method
        mdMethodDef methodDef;
        auto const signature = SerializeMethodSignatureDescriptor(method.signatureDescriptor);
        PCCOR_SIGNATURE signaturePointer = signature.data();
        hr = moduleDef.FindMethodDef(method.methodName, signaturePointer, signature.size(), typeDef, &methodDef);
        if (FAILED(hr))
            continue;
        
        auto const wrapperIt = moduleRewritings.find(methodDef);
        auto const hasWrapper = wrapperIt != moduleRewritings.cend();
        auto const sourceToken = hasWrapper ? (*wrapperIt).second : methodDef;
        
        // Store mappings
        ModuleID moduleId = moduleDef.GetModuleId();
        if (method.rewritingDescriptor.methodEnterInterpretation.has_value())
        {
            auto const mapping = method.rewritingDescriptor.methodEnterInterpretation.value();
            AddCustomEventMapping(
                _customEventOnMethodEntryLookup, 
                moduleId,
                sourceToken, 
                static_cast<USHORT>(RecordedEventType::MethodEnter), 
                mapping);

            AddCustomEventMapping(
                _customEventOnMethodEntryLookup, 
                moduleId,
                sourceToken,
                static_cast<USHORT>(RecordedEventType::MethodEnterWithArguments), 
                mapping);
        }
        if (method.rewritingDescriptor.methodExitInterpretation.has_value())
        {
            auto const mapping = method.rewritingDescriptor.methodExitInterpretation.value();
            AddCustomEventMapping(
                _customEventOnMethodExitLookup,
                moduleId, 
                sourceToken,
                static_cast<USHORT>(RecordedEventType::MethodExit),
                mapping);

            AddCustomEventMapping(
                _customEventOnMethodExitLookup,
                moduleId,
                sourceToken,
                static_cast<USHORT>(RecordedEventType::MethodExitWithArguments), 
                mapping);
        }

        auto _ = std::unique_lock<std::shared_mutex>(_methodDescriptorsSharedMutex);
        _methodDescriptorsLookup.emplace(std::make_pair(moduleDef.GetModuleId(), sourceToken), methodPointer);
        LOG_F(INFO, "Imported custom event on method %s::%s (%d) in module %s.", method.declaringTypeFullName.c_str(), method.methodName.c_str(), sourceToken, moduleDef.GetName().c_str());
    }

    return S_OK;
}

BOOL Profiler::CorProfiler::FindCustomEventMapping(
    const CustomEventsLookup& lookup,
    ModuleID moduleId,
    mdMethodDef methodDef,
    USHORT original,
    USHORT& mapping)
{
    auto guard = std::unique_lock<std::shared_mutex>(_customEventLookupsSharedMutex);
    auto mappingIt = lookup.find(std::make_tuple(moduleId, methodDef, original));
    if (mappingIt == lookup.cend())
        return false;

    mapping = (*mappingIt).second;
    return true;
}

HRESULT Profiler::CorProfiler::PatchMethodBody(LibProfiler::ModuleDef& moduleDef, mdTypeDef mdTypeDef, mdMethodDef mdMethodDef)
{
    {
        // If we are compiling injected method, skip it
        auto guard = std::shared_lock<std::shared_mutex>(_wrappersSharedMutex);
        if (_wrappers.find(std::make_pair(moduleDef.GetModuleId(), mdMethodDef)) != _wrappers.cend())
            return E_FAIL;
    }

    // If there are no rewritings registered for current module, skip it
    auto guard = std::shared_lock<std::shared_mutex>(_rewritingsSharedMutex);
    auto tokensToRewriteIterator = _rewritings.find(moduleDef.GetModuleId());
    if (tokensToRewriteIterator == _rewritings.cend())
        return E_FAIL;

    auto& tokensToRewrite = (*tokensToRewriteIterator).second;
    if (SUCCEEDED(LibProfiler::PatchMethodBody(
        *_corProfilerInfo,
        moduleDef,
        mdMethodDef,
        tokensToRewrite)))
    {
        _client.Send(LibIPC::Helpers::CreateMethodBodyRewriteMsg(
            CreateMetadataMsg(),
            moduleDef.GetModuleId(),
            mdMethodDef));

        return S_OK;
    }

    return E_FAIL;
}

static BOOL HasIndirects(const Profiler::MethodDescriptor& descriptor)
{
    auto const indirectIt = std::find_if(
        descriptor.rewritingDescriptor.arguments.cbegin(),
        descriptor.rewritingDescriptor.arguments.cend(),
        [](const Profiler::CapturedArgumentDescriptor& d)
        {
            auto const flags = static_cast<UINT>(d.value.flags);
            auto const indirect = static_cast<UINT>(Profiler::CapturedValueFlags::IndirectLoad);

            return (flags & indirect) != 0;
        });

    return indirectIt != descriptor.rewritingDescriptor.arguments.cend();
}

HRESULT Profiler::CorProfiler::EnterMethod(FunctionIDOrClientID functionOrClientId, COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    HRESULT hr;
    ModuleID moduleId;
    mdMethodDef methodDef;
    auto const functionId = functionOrClientId.functionID;
    hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not resolve functionId %lld to a method. Error: 0x%x", functionId, hr);
        return E_FAIL;
    }

    // Check if event mapping is available
    USHORT customMethodEnterEvent;
    USHORT customMethodEnterWithArgumentsEvent;
    auto const originalMethodEnterEvent = static_cast<USHORT>(RecordedEventType::MethodEnter);
    auto const originalMethodEnterWithArgumentsEvent = static_cast<USHORT>(RecordedEventType::MethodEnterWithArguments);
    auto const hasCustomMethodEnterEvent = FindCustomEventMapping(
        _customEventOnMethodEntryLookup,
        moduleId,
        methodDef,
        originalMethodEnterEvent,
        customMethodEnterEvent);
    auto const hasCustomMethodEnterWithArgumentsEvent = FindCustomEventMapping(
        _customEventOnMethodEntryLookup,
        moduleId,
        methodDef,
        originalMethodEnterWithArgumentsEvent,
        customMethodEnterWithArgumentsEvent);

    auto const hasDescriptor = HasMethodDescriptor(moduleId, methodDef);
    if (!hasDescriptor)
    {
        // Notify about method enter without arguments
        _client.Send(LibIPC::Helpers::CreateMethodEnterMsg(
            CreateMetadataMsg(),
            moduleId,
            methodDef,
            hasCustomMethodEnterEvent ? customMethodEnterEvent : originalMethodEnterEvent));
        return S_OK;
    }

    // Retrieve information about arguments
    auto descriptorPointer = GetMethodDescriptor(moduleId, methodDef);
    auto& descriptor = *descriptorPointer.get();
    auto const hasIndirects = HasIndirects(descriptor);
    if (descriptor.rewritingDescriptor.arguments.size() == 0)
    {
        // Notify about method enter without arguments
        _client.Send(LibIPC::Helpers::CreateMethodEnterMsg(
            CreateMetadataMsg(), 
            moduleId, 
            methodDef,
            hasCustomMethodEnterEvent ? customMethodEnterEvent : originalMethodEnterEvent));
        return S_OK;
    }

    // Retrieve arguments data
    COR_PRF_FRAME_INFO frameInfo { };
    ULONG argumentsLength = 0;
    hr = _corProfilerInfo->GetFunctionEnter3Info(functionId, eltInfo, &frameInfo, &argumentsLength, nullptr);
    if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        LOG_F(ERROR, "Could not retrieve arguments info for method %d. Error: 0x%x", methodDef, hr);
        return E_FAIL;
    }

    std::vector<UINT_PTR> indirects;
    auto rawArgumentInfos = std::make_unique<BYTE[]>(argumentsLength);
    hr = _corProfilerInfo->GetFunctionEnter3Info(functionId, eltInfo, &frameInfo, &argumentsLength, (COR_PRF_FUNCTION_ARGUMENT_INFO*)(rawArgumentInfos.get()));
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not retrieve arguments data for method %d. Error: 0x%x.", methodDef, hr);
        return E_FAIL;
    }

    auto& argumentInfos = *((COR_PRF_FUNCTION_ARGUMENT_INFO*)rawArgumentInfos.get());
    auto const argumentValuesLength = std::accumulate(
        descriptor.rewritingDescriptor.arguments.cbegin(),
        descriptor.rewritingDescriptor.arguments.cend(),
        0, [](INT sum, const CapturedArgumentDescriptor& d) { return sum + d.value.size; });
    auto const argumentOffsetsLength = descriptor.rewritingDescriptor.arguments.size() * sizeof(UINT);
    std::vector<BYTE> argumentValues;
    argumentValues.resize(argumentValuesLength);
    std::vector<BYTE> argumentOffsets;
    argumentOffsets.resize(argumentOffsetsLength);
    
    hr = GetArguments(
        descriptor,
        indirects,
        argumentInfos,
        std::span(argumentValues.data(), argumentValuesLength),
        std::span(argumentOffsets.data(), argumentOffsetsLength));
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not parse arguments data for method %d. Error: 0x%x.", methodDef, hr);
        return E_FAIL;
    }

    if (descriptor.rewritingDescriptor.returnValue.has_value() || indirects.size() > 0)
        ArgsCallStack.push(indirects);

    // Notify about method enter with arguments
    _client.Send(LibIPC::Helpers::CreateMethodEnterWithArgumentsMsg(
        CreateMetadataMsg(),
        moduleId,
        methodDef,
        hasCustomMethodEnterWithArgumentsEvent
            ? customMethodEnterWithArgumentsEvent
            : originalMethodEnterWithArgumentsEvent,
        std::move(argumentValues),
        std::move(argumentOffsets)));
    return S_OK;
}

HRESULT Profiler::CorProfiler::LeaveMethod(FunctionIDOrClientID functionOrClientId, COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    HRESULT hr;
    ModuleID moduleId;
    mdMethodDef methodDef;
    auto const functionId = functionOrClientId.functionID;
    hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not resolve functionId %lld to a method. Error: 0x%x", functionId, hr);
        return E_FAIL;
    }

    // Check if event mapping is available
    USHORT customMethodExitEvent;
    USHORT customMethodExitWithArgumentsEvent;
    auto const originalMethodExitEvent = static_cast<USHORT>(RecordedEventType::MethodExit);
    auto const originalMethodExitWithArgumentsEvent = static_cast<USHORT>(RecordedEventType::MethodExitWithArguments);
    auto const hasCustomMethodExitEvent = FindCustomEventMapping(
        _customEventOnMethodExitLookup,
        moduleId,
        methodDef,
        originalMethodExitEvent,
        customMethodExitEvent);
    auto const hasCustomMethodEnterWithArgumentsEvent = FindCustomEventMapping(
        _customEventOnMethodExitLookup,
        moduleId,
        methodDef,
        originalMethodExitWithArgumentsEvent,
        customMethodExitWithArgumentsEvent);

    auto const hasDescriptor = HasMethodDescriptor(moduleId, methodDef);
    if (!hasDescriptor)
    {
        // Notify about method leave without arguments
        _client.Send(LibIPC::Helpers::CreateMethodExitMsg(
            CreateMetadataMsg(),
            moduleId,
            methodDef,
            hasCustomMethodExitEvent ? customMethodExitEvent : originalMethodExitEvent));
        return S_OK;
    }

    // Retrieve information about arguments
    auto descriptorPtr = GetMethodDescriptor(moduleId, methodDef);
    auto& descriptor = *descriptorPtr.get();
    auto const hasIndirects = HasIndirects(descriptor);
    auto const hasReturnValue = descriptor.rewritingDescriptor.returnValue.has_value();
    if (!hasReturnValue && !hasIndirects)
    {
        // Notify about method leave without arguments
        _client.Send(LibIPC::Helpers::CreateMethodExitMsg(
            CreateMetadataMsg(),
            moduleId,
            methodDef,
            hasCustomMethodExitEvent ? customMethodExitEvent : originalMethodExitEvent));
        return S_OK;
    }

    // Retrieve return value data
    COR_PRF_FRAME_INFO frameInfo;
    COR_PRF_FUNCTION_ARGUMENT_RANGE returnValueInfo;
    hr = _corProfilerInfo->GetFunctionLeave3Info(functionId, eltInfo, &frameInfo, &returnValueInfo);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not retrieve return value info for method %d. Error: 0x%x", methodDef, hr);
        return E_FAIL;
    }
    std::vector<BYTE> returnValue;
    returnValue.resize(returnValueInfo.length);
    
    // Retrieve by-ref arguments data
    std::vector<BYTE> argumentValues;
    std::vector<BYTE> argumentOffsets;
    if (hasIndirects)
    {
        auto& indirects = ArgsCallStack.top();
        if (indirects.size() > 0)
        {
            auto argValuePointer = 0;
            auto argOffsetPointer = 0;
            auto const argumentValuesLength = std::accumulate(
                descriptor.rewritingDescriptor.arguments.cbegin(),
                descriptor.rewritingDescriptor.arguments.cend(),
                0, [](INT sum, const CapturedArgumentDescriptor& d) 
                {
                    auto const flags = static_cast<UINT>(d.value.flags);
                    auto const indirect = static_cast<UINT>(CapturedValueFlags::IndirectLoad);
                    if ((flags & indirect) == 0)
                        return sum;

                    return sum + d.value.size; 
                });
            auto const argumentOffsetsLength = indirects.size() * sizeof(UINT);
            argumentValues.resize(argumentValuesLength);
            argumentOffsets.resize(argumentOffsetsLength);
            hr = GetByRefArguments(
                descriptor,
                indirects,
                std::span<BYTE>(argumentValues.data(), argumentValues.size()),
                std::span<BYTE>(argumentOffsets.data(), argumentOffsets.size()));
            if (FAILED(hr))
            {
                LOG_F(ERROR, "Could not parse by-ref arguments data for method %d. Error: 0x%x.", methodDef, hr);
                ArgsCallStack.pop();
                return E_FAIL;
            }
        }
        ArgsCallStack.pop();
    }

    // Notify about method leave with arguments
    _client.Send(LibIPC::Helpers::CreateMethodExitWithArgumentsMsg(
        CreateMetadataMsg(),
        moduleId,
        methodDef,
        hasCustomMethodEnterWithArgumentsEvent
            ? customMethodExitWithArgumentsEvent
            : originalMethodExitWithArgumentsEvent,
        std::move(returnValue),
        std::move(argumentValues),
        std::move(argumentOffsets)));

    return S_OK;
}

HRESULT Profiler::CorProfiler::TailcallMethod(FunctionIDOrClientID functionOrClientId, COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    LOG_F(WARNING, "Tailcall.");
    return E_NOTIMPL;
}

HRESULT Profiler::CorProfiler::GetArguments(
    const MethodDescriptor& methodDescriptor,
    std::vector<UINT_PTR>& indirects,
    const COR_PRF_FUNCTION_ARGUMENT_INFO& argumentInfos,
    std::span<BYTE> argumentValues,
    std::span<BYTE> argumentOffsets)
{
    for (auto&& argument : methodDescriptor.rewritingDescriptor.arguments)
    {
        auto const flags = argument.value.flags;
        auto const range = argumentInfos.ranges[argument.index];

        auto hr = GetArgument(argument, range, indirects, argumentValues, argumentOffsets);
        if (FAILED(hr))
        {
            LOG_F(ERROR, "Could not retrieve argument on index %d from method %s invocation.", 
                argument.index,
                methodDescriptor.methodName.c_str());

            return E_FAIL;
        }
    }

    return S_OK;
}

HRESULT Profiler::CorProfiler::GetByRefArguments(
    const MethodDescriptor& methodDescriptor,
    const std::vector<UINT_PTR>& indirects,
    std::span<BYTE> indirectValues,
    std::span<BYTE> indirectOffsets)
{
    auto indirectsPointer = 0;
    for (auto&& argument : methodDescriptor.rewritingDescriptor.arguments)
    {
        if ((static_cast<UINT>(argument.value.flags) & static_cast<UINT>(CapturedValueFlags::IndirectLoad)) == 0)
            continue;

        UINT argInfo = argument.index << 16 | argument.value.size;
        UINT_PTR indirectAddress = indirects[indirectsPointer];
        std::memcpy(indirectValues.data(), (LPVOID)indirectAddress, argument.value.size);
        std::memcpy(indirectOffsets.data(), &argInfo, sizeof(UINT));
        indirectValues = indirectValues.subspan(argument.value.size);
        indirectOffsets = indirectOffsets.subspan(sizeof(UINT));
        indirectsPointer++;
    }

    return S_OK;
}

HRESULT Profiler::CorProfiler::GetArgument(
    const CapturedArgumentDescriptor& argument,
    COR_PRF_FUNCTION_ARGUMENT_RANGE range,
    std::vector<UINT_PTR>& indirects,
    std::span<BYTE>& argValue,
    std::span<BYTE>& argOffset)
{
    auto const flags = argument.value.flags;

    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::IndirectLoad)) != 0)
    {
        // Get pointer to the value
        UINT_PTR pointer;
        std::memcpy(&pointer, (LPVOID)range.startAddress, sizeof(UINT_PTR));
        indirects.push_back(pointer);

        // Read the value
        std::memcpy(argValue.data(), (LPVOID)pointer, argument.value.size);
        UINT argInfo = (argument.index << 16) | argument.value.size;
        std::memcpy(argOffset.data(), &argInfo, sizeof(UINT));
        argValue = argValue.subspan(argument.value.size);
    }
    else
    {
        // Read the value
        UINT argInfo = (argument.index << 16) | range.length;
        std::memcpy(argValue.data(), (LPVOID)range.startAddress, range.length);
        std::memcpy(argOffset.data(), &argInfo, sizeof(UINT));
        argValue = argValue.subspan(range.length);
    }

    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::CaptureAsReference)) != 0)
    {
        // Managed reference (object can be later moved by GC)
        ObjectID objectId;
        std::memcpy(&objectId, argValue.data() - sizeof(ObjectID), sizeof(ObjectID));
        auto const trackedObjectId = _objectsTracker.GetTrackedObject(objectId);
        std::memcpy(argValue.data() - sizeof(ObjectID), &trackedObjectId, sizeof(ObjectID));
    }

    argOffset = argOffset.subspan(sizeof(UINT));
    return S_OK;
}

std::shared_ptr<LibProfiler::ModuleDef> Profiler::CorProfiler::GetModuleDef(ModuleID moduleId)
{
    auto guard = std::shared_lock<std::shared_mutex>(_assembliesAndModulesSharedMutex);
    return _modules.find(moduleId)->second;
}

std::shared_ptr<LibProfiler::AssemblyDef> Profiler::CorProfiler::GetAssemblyDef(AssemblyID assemblyID)
{
    auto guard = std::shared_lock<std::shared_mutex>(_assembliesAndModulesSharedMutex);
    return _assemblies.find(assemblyID)->second;
}

std::shared_ptr<Profiler::MethodDescriptor> Profiler::CorProfiler::GetMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef)
{
    auto guard = std::shared_lock<std::shared_mutex>(_methodDescriptorsSharedMutex);
    return _methodDescriptorsLookup.find(std::make_pair(moduleId, methodDef))->second;
}

BOOL Profiler::CorProfiler::HasModuleDef(ModuleID moduleId)
{
    auto guard = std::shared_lock<std::shared_mutex>(_assembliesAndModulesSharedMutex);
    return _modules.find(moduleId) != _modules.cend();
}

BOOL Profiler::CorProfiler::HasAssemblyDef(AssemblyID assemblyId)
{
    auto guard = std::shared_lock<std::shared_mutex>(_assembliesAndModulesSharedMutex);
    return _assemblies.find(assemblyId) != _assemblies.cend();
}

BOOL Profiler::CorProfiler::HasMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef)
{
    auto guard = std::shared_lock<std::shared_mutex>(_methodDescriptorsSharedMutex);
    return _methodDescriptorsLookup.find(std::make_pair(moduleId, methodDef)) != _methodDescriptorsLookup.cend();
}

std::shared_ptr<Profiler::MethodDescriptor> Profiler::CorProfiler::FindMethodDescriptor(FunctionID functionId)
{
    ModuleID moduleId;
    mdMethodDef mdMethodDef;
    HRESULT hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &mdMethodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not determine information about Function ID = %lld. Error: 0x%x.", functionId, hr);
        return { };
    }

    auto guard = std::shared_lock<std::shared_mutex>(_methodDescriptorsSharedMutex);
    auto it = _methodDescriptorsLookup.find(std::make_pair(moduleId, mdMethodDef));
    return (it != _methodDescriptorsLookup.cend()) ? (*it).second : nullptr;
}

LibIPC::MetadataMsg Profiler::CorProfiler::CreateMetadataMsg()
{
    ThreadID threadId;
    _corProfilerInfo->GetCurrentThreadID(&threadId);
    return LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId);
}