// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <memory>
#include <numeric>
#include <stack>
#include <string>
#include <tuple>
#include <utility>
#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "../lib/loguru/loguru.hpp"

#include "../LibIPC/Client.h"
#include "../LibIPC/Messages.h"
#include "../LibProfiler/AssemblyDef.h"
#include "../LibProfiler/ModuleDef.h"
#include "../LibProfiler/Instrumentation.h"
#include "../LibProfiler/PAL.h"
#include "../LibProfiler/StackWalker.h"
#include "../LibProfiler/WString.h"
#include "CorProfiler.h"

using json = nlohmann::json;

Profiler::CorProfiler* ProfilerInstance;
thread_local std::stack<std::vector<UINT_PTR>> ArgsCallStack;

Profiler::CorProfiler::CorProfiler(const Configuration &configuration) :
    _configuration(configuration),
    _client(
		configuration.commandQueueName,
		configuration.commandQueueFile.value_or(std::string()),
		configuration.commandQueueSize,
        configuration.sharedMemoryName,
        configuration.sharedMemoryFile.value_or(std::string()),
        configuration.sharedMemorySize),
    _coreModule(0)
{
    _terminating = false;
    ProfilerInstance = this;
}

EXTERN_C void EnterNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void LeaveNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);
EXTERN_C void TailcallNaked(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO eltInfo);

UINT_PTR STDMETHODCALLTYPE FunctionMapper(const FunctionID funcId, void* clientData, BOOL* pbHookFunction)
{
    // Inject hooks only for methods where we explicitly requested them
    auto const descriptor = ProfilerInstance->FindMethodDescriptor(funcId);
    auto const shouldInjectHooks = descriptor != nullptr && descriptor->rewritingDescriptor.injectHooks;
    *pbHookFunction = shouldInjectHooks;

    return funcId;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::Initialize(IUnknown* pICorProfilerInfoUnk)
{
    LOG_F(INFO, "Profiler initializing in PID = %d.", LibProfiler::PAL_GetCurrentPid());

    if (FAILED(CorProfilerBase::Initialize(pICorProfilerInfoUnk)))
        return AbortAttach("Failed to initialize profiling API.");

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
        return AbortAttach("Could not determine .NET runtime information.");
    }

    ImportMethodDescriptors(majorVersion, minorVersion, buildVersion);

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

    if (runtimeType == COR_PRF_RUNTIME_TYPE::COR_PRF_DESKTOP_CLR)
        return AbortAttach(".NET Framework is not supported.");

    if (majorVersion < 8 || majorVersion > 10)
        return AbortAttach("Unsupported .NET SDK. Supported version are only 8, 9 and 10.");

    if (FAILED(InitializeProfilingFeatures()))
        return AbortAttach("Could not initialize requested profiling features.");
    LOG_F(INFO, "Initialized IL descriptors.");

    _client.SetCommandHandler(this);
    LOG_F(INFO, "Registered command handler.");

    _client.Send(LibIPC::Helpers::CreateProfilerInitiazeMsg(CreateMetadataMsg()));
    LOG_F(INFO, "Profiler initialized.");
    return S_OK;
}

HRESULT Profiler::CorProfiler::InitializeProfilingFeatures() const
{
    auto hr = _corProfilerInfo->SetEventMask(_configuration.eventMask);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not set profiling flags. Error: 0x%x.", hr);
        return E_FAIL;
    }

    if ((_configuration.eventMask & COR_PRF_MONITOR::COR_PRF_MONITOR_ENTERLEAVE) != 0)
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

HRESULT Profiler::CorProfiler::ImportMethodDescriptors(
    const INT32 versionMajor,
    const INT32 versionMinor,
    const INT32 versionBuild)
{
    for (auto&& item : _configuration.methodDescriptors)
    {
        if (!item.versionDescriptor.has_value())
        {
            _methodDescriptors.emplace_back(std::make_shared<MethodDescriptor>(item));
        }
        else
        {
            const auto& methodVersion = item.versionDescriptor.value();
            const auto fromVersion = std::make_tuple(
                methodVersion.fromMajorVersion,
                methodVersion.fromMinorVersion,
                methodVersion.fromBuildVersion);
            const auto toVersion = std::make_tuple(
                methodVersion.toMajorVersion,
                methodVersion.toMinorVersion,
                methodVersion.toBuildVersion);
            const auto currentVersion = std::make_tuple(
                versionMajor,
                versionMinor,
                versionBuild);
            
            // Check if currentVersion is within [fromVersion, toVersion] range
            if (currentVersion >= fromVersion && currentVersion <= toVersion)
            {
                _methodDescriptors.emplace_back(std::make_shared<MethodDescriptor>(item));
            }
        }
    }

    return S_OK;
}

PROFILER_STUB EnterStub(const FunctionIDOrClientID functionId, const COR_PRF_ELT_INFO eltInfo)
{
    ProfilerInstance->EnterMethod(functionId, eltInfo);
}

PROFILER_STUB LeaveStub(const FunctionIDOrClientID functionId, const COR_PRF_ELT_INFO eltInfo)
{
    ProfilerInstance->LeaveMethod(functionId, eltInfo);
}

PROFILER_STUB TailcallStub(const FunctionIDOrClientID functionId, const COR_PRF_ELT_INFO eltInfo)
{
    ProfilerInstance->TailcallMethod(functionId, eltInfo);
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ThreadCreated(const ThreadID threadId)
{
    if (_terminating)
        return S_OK;

    LOG_F(INFO, "Thread created %" UINT_PTR_FORMAT ".", threadId);
    _client.Send(LibIPC::Helpers::CreateThreadCreateMsg(CreateMetadataMsg(), threadId));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ThreadDestroyed(const ThreadID threadId)
{
    if (_terminating)
        return S_OK;

    _client.Send(LibIPC::Helpers::CreateThreadDestroyMsg(CreateMetadataMsg(), threadId));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ThreadNameChanged(
    const ThreadID threadId,
    const ULONG cchName,
    WCHAR name[])
{
    if (_terminating)
        return S_OK;

    const auto nameString = LibProfiler::ToString(LibProfiler::WSTRING(name, cchName));
    _client.Send(LibIPC::Helpers::CreateThreadRenameMsg(CreateMetadataMsg(), threadId, nameString));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::GarbageCollectionStarted(
    const int cGenerations,
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
    const auto oldSize = _objectsTracker.GetTrackedObjectsCount();
    const auto gcContext = _objectsTracker.ProcessGarbageCollectionFinished();
    auto& nextTrackedObjectIds = gcContext.GetNextTrackedObjects();
    auto& previousTrackedObjectIds = gcContext.GetPreviousTrackedObjects();
    std::vector<LibProfiler::TrackedObjectId> removedTrackedObjectIds;
    for (auto&& previousTrackedObjectId : previousTrackedObjectIds)
    {
        if (!nextTrackedObjectIds.contains(previousTrackedObjectId))
            removedTrackedObjectIds.push_back(previousTrackedObjectId);
    }
    if (!removedTrackedObjectIds.empty())
    {
        _client.Send(LibIPC::Helpers::CreateGarbageCollectedTrackedObjectsMsg(
            CreateMetadataMsg(), 
            std::move(removedTrackedObjectIds)));
    }

    const auto newSize = _objectsTracker.GetTrackedObjectsCount();
    _client.Send(LibIPC::Helpers::CreateGarbageCollectionFinishMsg(CreateMetadataMsg(), oldSize, newSize));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::MovedReferences2(
    const ULONG cMovedObjectIDRanges,
    ObjectID oldObjectIDRangeStart[], 
    ObjectID newObjectIDRangeStart[], 
    SIZE_T cObjectIDRangeLength[])
{
    _objectsTracker.ProcessMovingReferences(
        std::span(oldObjectIDRangeStart, cMovedObjectIDRanges),
        std::span(newObjectIDRangeStart, cMovedObjectIDRanges),
        std::span(cObjectIDRangeLength, cMovedObjectIDRanges));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::SurvivingReferences2(
    const ULONG cSurvivingObjectIDRanges,
    ObjectID objectIDRangeStart[],
    SIZE_T cObjectIDRangeLength[])
{
    _objectsTracker.ProcessSurvivingReferences(
        std::span(objectIDRangeStart, cSurvivingObjectIDRanges),
        std::span(cObjectIDRangeLength, cSurvivingObjectIDRanges));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::JITCompilationStarted(const FunctionID functionId, BOOL fIsSafeToBlock)
{
    if (_terminating)
        return S_OK;

    ModuleID moduleId;
    mdMethodDef mdMethodDef;
    if (FAILED(_corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &mdMethodDef)))
    {
        LOG_F(ERROR, "Could not determine information about Function ID = %" UINT_PTR_FORMAT ".", functionId);
        return E_FAIL;
    }

    if (!HasModuleDef(moduleId))
    {
        LOG_F(ERROR, "Could not resolve Module ID = %" UINT_PTR_FORMAT " for method TOK = %d.", moduleId, mdMethodDef);
        return E_FAIL;
    }
    const auto moduleDefPtr = GetModuleDef(moduleId);
    auto& moduleDef = *moduleDefPtr.get();

    mdTypeDef mdTypeDef;
    std::string methodName;
    std::string typeName;
    CorMethodAttr methodFlags;
    if (FAILED(moduleDef.GetMethodProps(mdMethodDef, &mdTypeDef, methodName, &methodFlags, nullptr, nullptr)) ||
        FAILED(moduleDef.GetTypeProps(mdTypeDef, nullptr, typeName)))
    {
        LOG_F(ERROR, "Could not obtain methods properties for TOK = %d.", mdMethodDef);
        return E_FAIL;
    }

    PatchMethodBody(moduleDef, mdTypeDef, mdMethodDef);
    _client.Send(LibIPC::Helpers::CreateJitCompilationMsg(CreateMetadataMsg(), mdTypeDef, mdMethodDef));
    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ModuleLoadFinished(ModuleID moduleId, const HRESULT hrStatus)
{
    if (FAILED(hrStatus) || _terminating)
        return S_OK;

    auto moduleDefPtr = std::make_shared<LibProfiler::ModuleDef>(*_corProfilerInfo);
    auto assemblyDefPtr = std::make_shared<LibProfiler::AssemblyDef>(*_corProfilerInfo);
    auto& moduleDef = *moduleDefPtr.get();
    auto& assemblyDef = *assemblyDefPtr.get();
    moduleDef.Initialize(moduleId);
    assemblyDef.Initialize(moduleId);

    {
        auto const assemblyId = assemblyDef.GetAssemblyId();
        auto guard = std::unique_lock(_assembliesAndModulesMutex);
        // FIXME: modules and assemblies are not always 1:1 mapped (assembly can contain multiple modules)
        _modules.emplace(moduleId, moduleDefPtr);
        _assemblies.emplace(assemblyId, assemblyDefPtr);
    }

    if (_coreModule == 0)
    {
        _coreModule = moduleDef.GetModuleId();
        LOG_F(INFO, "Identified core module: %s with handle (%" UINT_PTR_FORMAT ")", moduleDef.GetName().c_str(), moduleDef.GetModuleId());
    }

    LOG_F(INFO, "Loaded assembly: %s with handle (%" UINT_PTR_FORMAT ")", assemblyDef.GetName().c_str(), assemblyDef.GetAssemblyId());
    LOG_F(INFO, "Loaded module: %s with handle (%" UINT_PTR_FORMAT ")", moduleDef.GetFullPath().c_str(), moduleDef.GetModuleId());

    if (_coreModule == moduleId)
    {
        InjectTypesForProfilingFeatures(moduleDef);
    }
    else
    {
        ImportInjectedTypes(assemblyDef, moduleDef);
    }

    WrapAnalyzedExternMethods(moduleDef);
    ImportMethodWrappers(assemblyDef, moduleDef);
    ImportCustomRecordedEventTypes(moduleDef);

    _client.Send(LibIPC::Helpers::CreateAssemblyLoadMsg(CreateMetadataMsg(), assemblyDef.GetAssemblyId(), assemblyDef.GetName()));
    _client.Send(LibIPC::Helpers::CreateModuleLoadMsg(CreateMetadataMsg(), moduleDef.GetModuleId(), assemblyDef.GetAssemblyId(), moduleDef.GetFullPath()));

    return S_OK;
}

static HRESULT SerializeArgumentTypeDescriptor(
    const Profiler::ArgumentTypeDescriptor& descriptor,
    const LibProfiler::ModuleDef& moduleDef,
    std::vector<COR_SIGNATURE>& result)
{
    for (size_t i = 0; i < descriptor.elementTypes.size(); i++)
    {
        const auto elementType = descriptor.elementTypes[i];
        result.push_back(elementType);

        if (elementType == CorElementType::ELEMENT_TYPE_CLASS || elementType == CorElementType::ELEMENT_TYPE_VALUETYPE)
        {
            mdTypeDef typeDef;
            auto hr = moduleDef.FindTypeDef(descriptor.typeName.value(), &typeDef);
            
            if (SUCCEEDED(hr))
            {
                // Compress the token into the signature
                BYTE tokenBytes[4];
                const ULONG tokenLength = CorSigCompressToken(typeDef, tokenBytes);
                
                for (ULONG j = 0; j < tokenLength; j++)
                    result.push_back(tokenBytes[j]);
            }
            else
            {
                // FIXME: add support for type references
                LOG_F(ERROR, "Type %s not found in module", descriptor.typeName.value().c_str());
                return E_FAIL;
            }
        }
    }

    return S_OK;
}

static std::vector<COR_SIGNATURE> SerializeMethodSignatureDescriptor(
    const Profiler::MethodSignatureDescriptor& descriptor,
    const LibProfiler::ModuleDef& moduleDef)
{
    std::vector<COR_SIGNATURE> result;
    result.push_back(descriptor.callingConvention);
    result.push_back(descriptor.parametersCount);
    if (FAILED(SerializeArgumentTypeDescriptor(descriptor.returnType, moduleDef, result)))
    {
        LOG_F(ERROR, "Could not serialize return type of method signature.");
        return { };
    }

    for (auto&& argumentType : descriptor.argumentTypeElements)
    {
        if (FAILED(SerializeArgumentTypeDescriptor(argumentType, moduleDef, result)))
        {
            LOG_F(ERROR, "Could not serialize argument type of method signature.");
            return { };
        }
    }

    return result;
}

HRESULT Profiler::CorProfiler::WrapAnalyzedExternMethods(LibProfiler::ModuleDef& moduleDef)
{
    std::unordered_map<mdToken, mdToken> rewritingsBuilder;
    
    {
        auto guard = std::unique_lock(_rewritingsMutex);
        for (auto&& methodPtr : _methodDescriptors)
        {
            auto&[
                methodName,
                declaringTypeFullName,
                versionDescriptor,
                signatureDescriptor,
                rewritingDescriptor] = *methodPtr.get();

            // Method should be marked for wrapper injection in order to continue
            if (!rewritingDescriptor.injectManagedWrapper)
                continue;

            mdTypeDef typeDef;
            auto hr = moduleDef.FindTypeDef(declaringTypeFullName, &typeDef);
            if (FAILED(hr))
                continue;

            mdMethodDef methodDef;
            auto methodSignature = SerializeMethodSignatureDescriptor(signatureDescriptor, moduleDef);
            hr = moduleDef.FindMethodDef(
                methodName,
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
                    methodName.c_str(),
                    declaringTypeFullName.c_str(),
                    moduleDef.GetName().c_str(),
                    hr);

                return E_FAIL;
            }

            rewritingsBuilder.emplace(methodDef, wrapperMethodDef);
            {
                auto guard = std::unique_lock(_methodStubsMutex);
                _methodStubs.emplace(std::make_pair(moduleDef.GetModuleId(), wrapperMethodDef), true);
            }

            _client.Send(LibIPC::Helpers::CreateMethodWrapperInjectionMsg(CreateMetadataMsg(), moduleDef.GetModuleId(), typeDef, methodDef, wrapperMethodDef, wrapperMethodName));

            LOG_F(INFO, "Wrapped %s::%s (%d) -> (%d) in module %s.",
                declaringTypeFullName.c_str(),
                methodName.c_str(), methodDef,
                wrapperMethodDef,
                moduleDef.GetName().c_str());
        }
    }
    
    auto guard = std::unique_lock(_rewritingsMutex);
    _rewritings.emplace(moduleDef.GetModuleId(), rewritingsBuilder);
    return S_OK;
}

HRESULT Profiler::CorProfiler::ImportMethodWrappers(const LibProfiler::AssemblyDef& assemblyDef, const LibProfiler::ModuleDef& moduleDef)
{
    auto guard = std::unique_lock(_methodDescriptorsMutex);
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
    const LibProfiler::ModuleDef& moduleDef,
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
    auto const signature = SerializeMethodSignatureDescriptor(method.signatureDescriptor, moduleDef);
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
        auto guard = std::unique_lock(_rewritingsMutex);
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
    if (static_cast<LibIPC::RecordedEventType>(mapping) == LibIPC::RecordedEventType::NotSpecified)
        return;

    auto guard = std::unique_lock(_customEventLookupsMutex);
    lookup.emplace(std::make_tuple(moduleId, methodDef, original), mapping);
}

HRESULT Profiler::CorProfiler::ImportCustomRecordedEventTypes(const LibProfiler::ModuleDef& moduleDef)
{
    auto guard = std::unique_lock(_rewritingsMutex);
    auto& moduleRewritings = _rewritings.find(moduleDef.GetModuleId())->second;

    for (auto&& methodPointer : _methodDescriptors)
    {
        auto&[
            methodName,
            declaringTypeFullName,
            versionDescriptor,
            signatureDescriptor,
            rewritingDescriptor] = *methodPointer.get();

        // Get declaring type
        mdTypeDef typeDef;
        HRESULT hr = moduleDef.FindTypeDef(declaringTypeFullName, &typeDef);
        if (FAILED(hr))
            continue;

        // Get method
        mdMethodDef methodDef;
        auto const signature = SerializeMethodSignatureDescriptor(signatureDescriptor, moduleDef);
        const PCCOR_SIGNATURE signaturePointer = signature.data();
        hr = moduleDef.FindMethodDef(methodName, signaturePointer, signature.size(), typeDef, &methodDef);
        if (FAILED(hr))
        {
            LOG_F(WARNING, "Could not find method %s::%s in module %s for custom event mapping.",
                declaringTypeFullName.c_str(),
                methodName.c_str(),
                moduleDef.GetName().c_str());
            continue;
        }
        
        auto const wrapperIt = moduleRewritings.find(methodDef);
        auto const hasWrapper = wrapperIt != moduleRewritings.cend();
        auto const sourceToken = hasWrapper ? wrapperIt->second : methodDef;
        
        // Store mappings
        const ModuleID moduleId = moduleDef.GetModuleId();
        if (rewritingDescriptor.methodEnterInterpretation.has_value())
        {
            auto const mapping = rewritingDescriptor.methodEnterInterpretation.value();
            AddCustomEventMapping(
                _customEventOnMethodEntryLookup, 
                moduleId,
                sourceToken, 
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnter),
                mapping);

            AddCustomEventMapping(
                _customEventOnMethodEntryLookup, 
                moduleId,
                sourceToken,
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnterWithArguments),
                mapping);
        }
        if (rewritingDescriptor.methodExitInterpretation.has_value())
        {
            auto const mapping = rewritingDescriptor.methodExitInterpretation.value();
            AddCustomEventMapping(
                _customEventOnMethodExitLookup,
                moduleId, 
                sourceToken,
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodExit),
                mapping);

            AddCustomEventMapping(
                _customEventOnMethodExitLookup,
                moduleId,
                sourceToken,
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodExitWithArguments),
                mapping);
        }

        auto _ = std::unique_lock(_methodDescriptorsMutex);
        _methodDescriptorsLookup.emplace(std::make_pair(moduleDef.GetModuleId(), sourceToken), methodPointer);
        LOG_F(INFO, "Imported custom event on method %s::%s (%d) in module %s.", declaringTypeFullName.c_str(), methodName.c_str(), sourceToken, moduleDef.GetName().c_str());
    }

    return S_OK;
}

HRESULT Profiler::CorProfiler::InjectTypesForProfilingFeatures(LibProfiler::ModuleDef &moduleDef)
{
    constexpr auto injectedTypeFlags = static_cast<CorTypeAttr>(
        CorTypeAttr::tdClass |
        CorTypeAttr::tdPublic |
        CorTypeAttr::tdAbstract |
        CorTypeAttr::tdSealed |
        CorTypeAttr::tdBeforeFieldInit);
    constexpr auto injectedMethodFlags = static_cast<CorMethodAttr>(
        CorMethodAttr::mdPublic |
        CorMethodAttr::mdStatic |
        CorMethodAttr::mdHideBySig);
    constexpr auto injectedMethodImplFlags = static_cast<CorMethodImpl>(
        CorMethodImpl::miManaged |
        CorMethodImpl::miNoInlining);
    
    mdTypeDef systemObjectTypeDef;
    if (FAILED(moduleDef.FindTypeDef("System.Object", &systemObjectTypeDef)))
    {
        LOG_F(ERROR, "Could not find System.Object in module %s for type injection.",
            moduleDef.GetName().c_str());
        return E_FAIL;
    }

    for (auto& [typeFullName, methods] : _configuration.typeInjectionDescriptors)
    {
        mdTypeDef injectedTypeDef;
        if (FAILED(moduleDef.AddTypeDef(
            typeFullName,
            injectedTypeFlags,
            systemObjectTypeDef,
            &injectedTypeDef)))
        {
            LOG_F(ERROR, "Could not inject type %s into module %s.",
                typeFullName.c_str(),
                moduleDef.GetName().c_str());
            continue;
        }

        LOG_F(INFO, "Injected type %s (%d) into module %s.",
            typeFullName.c_str(),
            injectedTypeDef,
            moduleDef.GetName().c_str());

        std::unordered_map<LibIPC::RecordedEventType, mdToken> injectedMethods;
        for (auto& [methodName, eventType, methodSignatureDescriptor] : methods)
        {
            mdMethodDef injectedMethodDef;
            auto const methodSignature = SerializeMethodSignatureDescriptor(
                methodSignatureDescriptor,
                moduleDef);

            if (FAILED(moduleDef.AddMethodDef(
                methodName,
                injectedMethodFlags,
                injectedTypeDef,
                methodSignature.data(),
                methodSignature.size(),
                injectedMethodImplFlags,
                &injectedMethodDef)))
            {
                LOG_F(ERROR, "Could not inject method %s into type %s in module %s.",
                    methodName.c_str(),
                    typeFullName.c_str(),
                    moduleDef.GetName().c_str());
                continue;
            }

            void* rawNewMethodBody;
            if (FAILED(moduleDef.AllocMethodBody(2, &rawNewMethodBody)))
            {
                LOG_F(ERROR, "Could not allocate memory for injected method %s in module %s.",
                    methodName.c_str(),
                    moduleDef.GetName().c_str());
                return E_FAIL;
            }
            const auto newMethodBody = static_cast<BYTE*>(rawNewMethodBody);
            *newMethodBody = static_cast<BYTE>(0b00000110);
            *(newMethodBody + 1) = CEE_RET;
            if (FAILED(_corProfilerInfo->SetILFunctionBody(moduleDef.GetModuleId(), injectedMethodDef, newMethodBody)))
            {
                LOG_F(ERROR, "Could not set method body for injected method %s in module %s.",
                    methodName.c_str(),
                    moduleDef.GetName().c_str());
                return E_FAIL;
            }

            LOG_F(INFO, "Injected method %s (%d) into type %s in module %s.",
                methodName.c_str(),
                injectedMethodDef,
                typeFullName.c_str(),
                moduleDef.GetName().c_str());

            injectedMethods.emplace(eventType, injectedMethodDef);
            auto guard = std::unique_lock(_methodStubsMutex);
            _methodStubs.emplace(std::make_pair(moduleDef.GetModuleId(), injectedMethodDef), true);
        }

        auto guard = std::unique_lock(_injectedMethodsMutex);
        _injectedMethods.emplace(moduleDef.GetModuleId(), injectedMethods);
    }

    return S_OK;
}

HRESULT Profiler::CorProfiler::ImportInjectedTypes(
    const LibProfiler::AssemblyDef &assemblyDef,
    const LibProfiler::ModuleDef &moduleDef)
{
    if (_configuration.typeInjectionDescriptors.empty())
        return S_OK;

    const auto& coreModuleDef = *GetModuleDef(_coreModule).get();
    const auto& coreAssemblyDef = *GetAssemblyDef(coreModuleDef.GetAssemblyId()).get();
    const void* coreAssemblyPublicKeyData;
    ULONG coreAssemblyPublicKeyDataSize;
    ASSEMBLYMETADATA coreAssemblyMetadata{};
    DWORD coreAssemblyFlags;
    if (FAILED(coreAssemblyDef.GetProps(
        &coreAssemblyPublicKeyData,
        &coreAssemblyPublicKeyDataSize,
        &coreAssemblyMetadata,
        &coreAssemblyFlags)))
    {
        LOG_F(ERROR, "Could not obtain core assembly properties for type injection.");
        return E_FAIL;
    }

    mdAssemblyRef coreAssemblyRef;
    BOOL wasCoreAssemblyRefInjected;
    if (FAILED(assemblyDef.AddOrGetAssemblyRef(
        coreAssemblyDef.GetName(),
        coreAssemblyPublicKeyData,
        coreAssemblyPublicKeyDataSize,
        coreAssemblyMetadata,
        coreAssemblyFlags,
        &coreAssemblyRef,
        &wasCoreAssemblyRefInjected)))
    {
        LOG_F(ERROR, "Could not add reference to core assembly for type injection.");
        return E_FAIL;
    }

    for (auto& [typeFullName, methods] : _configuration.typeInjectionDescriptors)
    {
        mdTypeRef typeRef;
        if (FAILED(moduleDef.AddTypeRef(
            coreAssemblyRef,
            typeFullName,
            &typeRef)))
        {
            LOG_F(ERROR, "Could not import injected type %s into module %s.",
                typeFullName.c_str(),
                moduleDef.GetName().c_str());
        }

        std::unordered_map<LibIPC::RecordedEventType, mdToken> injectedMethods;
        for (auto& [methodName, eventType, methodSignatureDescriptor] : methods)
        {
            mdMemberRef methodRef;
            auto const methodSignature = SerializeMethodSignatureDescriptor(
                methodSignatureDescriptor,
                moduleDef);

            if (FAILED(moduleDef.AddMethodRef(
                methodName,
                typeRef,
                methodSignature.data(),
                methodSignature.size(),
                &methodRef)))
            {
                LOG_F(ERROR, "Could not import injected method %s into type %s in module %s.",
                    methodName.c_str(),
                    typeFullName.c_str(),
                    moduleDef.GetName().c_str());
                continue;
            }

            injectedMethods.emplace(eventType, methodRef);
        }

        auto guard = std::unique_lock(_injectedMethodsMutex);
        _injectedMethods.emplace(moduleDef.GetModuleId(), injectedMethods);
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
    auto guard = std::unique_lock(_customEventLookupsMutex);
    const auto mappingIt = lookup.find(std::make_tuple(moduleId, methodDef, original));
    if (mappingIt == lookup.cend())
        return false;

    mapping = mappingIt->second;
    return true;
}

HRESULT Profiler::CorProfiler::PatchMethodBody(const LibProfiler::ModuleDef& moduleDef, mdTypeDef mdTypeDef, mdMethodDef mdMethodDef)
{
    {
        // If we are compiling injected method, skip it
        auto guard = std::unique_lock<std::mutex>(_methodStubsMutex);
        if (_methodStubs.contains(std::make_pair(moduleDef.GetModuleId(), mdMethodDef)))
            return E_FAIL;
    }

    // If there are no rewritings registered for current module, skip it
    auto guardRewritings = std::unique_lock<std::mutex>(_rewritingsMutex);
    auto guardInjections = std::unique_lock<std::mutex>(_injectedMethodsMutex);
    const auto tokensToRewriteIterator = _rewritings.find(moduleDef.GetModuleId());
    const auto injectedMethodsIterator = _injectedMethods.find(moduleDef.GetModuleId());
    if (tokensToRewriteIterator == _rewritings.cend() && injectedMethodsIterator == _injectedMethods.cend())
        return E_FAIL;

    static const std::unordered_map<mdToken, mdToken> emptyTokensMap;
    static const std::unordered_map<LibIPC::RecordedEventType, mdToken> emptyInjectedMethodsMap;
    const auto& tokensToRewrite = tokensToRewriteIterator != _rewritings.cend() 
        ? tokensToRewriteIterator->second 
        : emptyTokensMap;
    const auto& injectedMethods = injectedMethodsIterator != _injectedMethods.cend() 
        ? injectedMethodsIterator->second 
        : emptyInjectedMethodsMap;
    
    if (SUCCEEDED(LibProfiler::PatchMethodBody(
        *_corProfilerInfo,
        _client,
        moduleDef,
        mdMethodDef,
        tokensToRewrite,
        injectedMethods,
        _configuration.enableFieldsAccessInstrumentation)))
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
    auto const indirectIt = std::ranges::find_if(
        descriptor.rewritingDescriptor.arguments,
        [](const Profiler::CapturedArgumentDescriptor& d)
        {
            auto const flags = static_cast<UINT>(d.value.flags);
            constexpr auto indirect = static_cast<UINT>(Profiler::CapturedValueFlags::IndirectLoad);
            return (flags & indirect) != 0;
        });

    return indirectIt != descriptor.rewritingDescriptor.arguments.cend();
}

HRESULT Profiler::CorProfiler::EnterMethod(const FunctionIDOrClientID functionOrClientId, const COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    ModuleID moduleId;
    mdMethodDef methodDef;
    auto const functionId = functionOrClientId.functionID;
    HRESULT hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not resolve functionId %" UINT_PTR_FORMAT " to a method. Error: 0x%x", functionId, hr);
        return E_FAIL;
    }

    // Check if event mapping is available
    USHORT customMethodEnterEvent;
    USHORT customMethodEnterWithArgumentsEvent;
    constexpr auto originalMethodEnterEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnter);
    constexpr auto originalMethodEnterWithArgumentsEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnterWithArguments);
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
    const auto descriptorPointer = GetMethodDescriptor(moduleId, methodDef);
    const auto& descriptor = *descriptorPointer.get();
    if (descriptor.rewritingDescriptor.arguments.empty())
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
    const auto rawArgumentInfos = std::unique_ptr<BYTE[]>(new BYTE[argumentsLength]);
    hr = _corProfilerInfo->GetFunctionEnter3Info(functionId, eltInfo, &frameInfo, &argumentsLength, reinterpret_cast<COR_PRF_FUNCTION_ARGUMENT_INFO *>(rawArgumentInfos.get()));
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not retrieve arguments data for method %d. Error: 0x%x.", methodDef, hr);
        return E_FAIL;
    }

    const auto& argumentInfos = *reinterpret_cast<COR_PRF_FUNCTION_ARGUMENT_INFO *>(rawArgumentInfos.get());
    auto const argumentValuesLength = std::accumulate(
        descriptor.rewritingDescriptor.arguments.cbegin(),
        descriptor.rewritingDescriptor.arguments.cend(),
        0, [](const INT sum, const CapturedArgumentDescriptor& d) { return sum + d.value.size; });
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

    if (descriptor.rewritingDescriptor.returnValue.has_value() || !indirects.empty())
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

HRESULT Profiler::CorProfiler::LeaveMethod(const FunctionIDOrClientID functionOrClientId, const COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    ModuleID moduleId;
    mdMethodDef methodDef;
    auto const functionId = functionOrClientId.functionID;
    HRESULT hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not resolve functionId %" UINT_PTR_FORMAT " to a method. Error: 0x%x", functionId, hr);
        return E_FAIL;
    }

    // Check if event mapping is available
    USHORT customMethodExitEvent;
    USHORT customMethodExitWithArgumentsEvent;
    constexpr auto originalMethodExitEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodExit);
    constexpr auto originalMethodExitWithArgumentsEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodExitWithArguments);
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
    const auto descriptorPtr = GetMethodDescriptor(moduleId, methodDef);
    const auto& descriptor = *descriptorPtr.get();
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
    if (hasReturnValue)
    {
        hr = GetReturnValue(
            descriptor.rewritingDescriptor.returnValue.value(),
            returnValueInfo,
            std::span<BYTE>(returnValue.data(), returnValueInfo.length));
        if (FAILED(hr))
        {
            LOG_F(ERROR, "Could not parse return value data for method %d. Error: 0x%x.", methodDef, hr);
            return E_FAIL;
        }
    }

    // Retrieve by-ref arguments data
    std::vector<BYTE> argumentValues;
    std::vector<BYTE> argumentOffsets;
    if (hasIndirects)
    {
        auto& indirects = ArgsCallStack.top();
        if (!indirects.empty())
        {
            auto const argumentValuesLength = std::accumulate(
                descriptor.rewritingDescriptor.arguments.cbegin(),
                descriptor.rewritingDescriptor.arguments.cend(),
                0, [](const INT sum, const CapturedArgumentDescriptor& d)
                {
                    auto const flags = static_cast<UINT>(d.value.flags);
                    constexpr auto indirect = static_cast<UINT>(CapturedValueFlags::IndirectLoad);
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
                std::span(argumentValues.data(), argumentValues.size()),
                std::span(argumentOffsets.data(), argumentOffsets.size()));
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
    for (const auto&[index, value] : methodDescriptor.rewritingDescriptor.arguments)
    {
        if ((static_cast<UINT>(value.flags) & static_cast<UINT>(CapturedValueFlags::IndirectLoad)) == 0)
            continue;

        UINT argInfo = index << 16 | value.size;
        const UINT_PTR indirectAddress = indirects[indirectsPointer];
        std::memcpy(indirectValues.data(), reinterpret_cast<LPVOID>(indirectAddress), value.size);
        std::memcpy(indirectOffsets.data(), &argInfo, sizeof(UINT));
        indirectValues = indirectValues.subspan(value.size);
        indirectOffsets = indirectOffsets.subspan(sizeof(UINT));
        indirectsPointer++;
    }

    return S_OK;
}

HRESULT Profiler::CorProfiler::GetArgument(
    const CapturedArgumentDescriptor& argument,
    const COR_PRF_FUNCTION_ARGUMENT_RANGE range,
    std::vector<UINT_PTR>& indirects,
    std::span<BYTE>& argValue,
    std::span<BYTE>& argOffset)
{
    auto const flags = argument.value.flags;

    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::IndirectLoad)) != 0)
    {
        // Get pointer to the value
        UINT_PTR pointer;
        std::memcpy(&pointer, reinterpret_cast<LPVOID>(range.startAddress), sizeof(UINT_PTR));
        indirects.push_back(pointer);

        // Read the value
        std::memcpy(argValue.data(), reinterpret_cast<LPVOID>(pointer), argument.value.size);
        UINT argInfo = (argument.index << 16) | argument.value.size;
        std::memcpy(argOffset.data(), &argInfo, sizeof(UINT));
        argValue = argValue.subspan(argument.value.size);
    }
    else
    {
        // Read the value
        const UINT argInfo = (argument.index << 16) | range.length;
        std::memcpy(argValue.data(), reinterpret_cast<LPVOID>(range.startAddress), range.length);
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

HRESULT Profiler::CorProfiler::GetReturnValue(
    const CapturedValueDescriptor& value,
    const COR_PRF_FUNCTION_ARGUMENT_RANGE range,
    const std::span<BYTE>& returnValue)
{
    auto const flags = value.flags;

    if ((static_cast<UINT>(flags) & static_cast<UINT>(CapturedValueFlags::CaptureAsReference)) != 0)
    {
        // Managed reference (object can be later moved by GC)
        ObjectID objectId;
        std::memcpy(&objectId, reinterpret_cast<LPVOID>(range.startAddress), sizeof(ObjectID));
        auto const trackedObjectId = _objectsTracker.GetTrackedObject(objectId);
        std::memcpy(returnValue.data(), &trackedObjectId, sizeof(ObjectID));
    }
    else
    {
        // Read the value
        std::memcpy(returnValue.data(), reinterpret_cast<LPVOID>(range.startAddress), range.length);
    }

    return S_OK;
}

std::shared_ptr<LibProfiler::ModuleDef> Profiler::CorProfiler::GetModuleDef(const ModuleID moduleId)
{
    auto guard = std::unique_lock(_assembliesAndModulesMutex);
    return _modules.find(moduleId)->second;
}

std::shared_ptr<LibProfiler::AssemblyDef> Profiler::CorProfiler::GetAssemblyDef(const AssemblyID assemblyID)
{
    auto guard = std::unique_lock(_assembliesAndModulesMutex);
    return _assemblies.find(assemblyID)->second;
}

std::shared_ptr<Profiler::MethodDescriptor> Profiler::CorProfiler::GetMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef)
{
    auto guard = std::unique_lock(_methodDescriptorsMutex);
    return _methodDescriptorsLookup.find(std::make_pair(moduleId, methodDef))->second;
}

BOOL Profiler::CorProfiler::HasModuleDef(const ModuleID moduleId)
{
    auto guard = std::unique_lock(_assembliesAndModulesMutex);
    return _modules.contains(moduleId);
}

BOOL Profiler::CorProfiler::HasAssemblyDef(const AssemblyID assemblyId)
{
    auto guard = std::unique_lock(_assembliesAndModulesMutex);
    return _assemblies.contains(assemblyId);
}

BOOL Profiler::CorProfiler::HasMethodDescriptor(ModuleID moduleId, mdMethodDef methodDef)
{
    auto guard = std::unique_lock(_methodDescriptorsMutex);
    return _methodDescriptorsLookup.contains(std::make_pair(moduleId, methodDef));
}

std::shared_ptr<Profiler::MethodDescriptor> Profiler::CorProfiler::FindMethodDescriptor(const FunctionID functionId)
{
    ModuleID moduleId;
    mdMethodDef mdMethodDef;
    HRESULT hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &mdMethodDef);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not determine information about Function ID = %" UINT_PTR_FORMAT ". Error: 0x%x.", functionId, hr);
        return { };
    }

    auto guard = std::unique_lock(_methodDescriptorsMutex);
    const auto it = _methodDescriptorsLookup.find(std::make_pair(moduleId, mdMethodDef));
    return (it != _methodDescriptorsLookup.cend()) ? it->second : nullptr;
}

HRESULT Profiler::CorProfiler::AbortAttach(const std::string& reason)
{
    LOG_F(ERROR, "%s Terminating.", reason.c_str());
    _client.Send(LibIPC::Helpers::CreateProfilerAbortInitializeMsg(
        CreateMetadataMsg(),
        reason));
    return E_FAIL;
}

LibIPC::MetadataMsg Profiler::CorProfiler::CreateMetadataMsg() const
{
    ThreadID threadId;
    _corProfilerInfo->GetCurrentThreadID(&threadId);
    return LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId);
}

LibIPC::MetadataMsg Profiler::CorProfiler::CreateMetadataMsg(const UINT64 commandId) const
{
    ThreadID threadId;
    _corProfilerInfo->GetCurrentThreadID(&threadId);
    return LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId, commandId);
}

void Profiler::CorProfiler::OnCreateStackSnapshot(const UINT64 commandId, const UINT64 targetThreadId)
{
    LOG_F(INFO, "Received command to create stack snapshot for thread %" UINT_PTR_FORMAT " (commandId: %lu).", targetThreadId, commandId);

    if (FAILED(CaptureStackTrace(commandId, targetThreadId)))
    {
        LOG_F(ERROR, "Failed to capture stack trace for thread %" UINT_PTR_FORMAT ".", targetThreadId);
    }
}

void Profiler::CorProfiler::OnCreateStackSnapshots(const UINT64 commandId, const std::vector<UINT64>& targetThreadIds)
{
    LOG_F(INFO, "Received command to create stack snapshots for %zu threads (commandId: %lu).", targetThreadIds.size(), commandId);

    std::vector<std::vector<LibProfiler::StackFrame>> frames;
    auto hr = LibProfiler::StackWalker::CaptureStackTraces(_corProfilerInfo, targetThreadIds, frames);

    if (FAILED(hr))
    {
        LOG_F(WARNING, "One or more stack traces failed to capture. Error: 0x%x.", hr);
    }

    std::vector<LibIPC::StackTraceSnapshotMsgArgs> snapshots;
    snapshots.reserve(frames.size());

    for (size_t i = 0; i < frames.size(); ++i)
    {
        std::vector<UINT64> moduleIds;
        std::vector<UINT32> methodTokens;
        moduleIds.reserve(frames[i].size());
        methodTokens.reserve(frames[i].size());

        for (const auto&[moduleId, methodToken] : frames[i])
        {
            moduleIds.push_back(moduleId);
            methodTokens.push_back(methodToken);
        }

        snapshots.emplace_back(targetThreadIds[i], std::move(moduleIds), std::move(methodTokens));
    }

	const auto snapshotsCount = snapshots.size();
    _client.Send(LibIPC::Helpers::CreateStackTraceSnapshotsMsg(CreateMetadataMsg(commandId), std::move(snapshots)));
    LOG_F(INFO, "Sent stack snapshots notification with %zu snapshots (commandId: %lu).", snapshotsCount, commandId);
}

HRESULT Profiler::CorProfiler::CaptureStackTrace(const UINT64 commandId, const ThreadID threadId)
{
    std::vector<UINT64> moduleIds;
    std::vector<UINT32> methodTokens;

    auto hr = LibProfiler::StackWalker::CaptureStackTrace(_corProfilerInfo, threadId, moduleIds, methodTokens);

    if (FAILED(hr))
    {
        LOG_F(ERROR, "StackWalker::CaptureStackTrace failed. Error: 0x%x.", hr);
        return hr;
    }

    _client.Send(LibIPC::Helpers::CreateStackTraceSnapshotMsg(
        CreateMetadataMsg(commandId),
        threadId,
        std::move(moduleIds),
        std::move(methodTokens)));

    LOG_F(INFO, "Sent stack trace snapshot notification for thread %" UINT_PTR_FORMAT " with %zu frames (commandId: %lu).",
        threadId, moduleIds.size(), commandId);

    return S_OK;
}


