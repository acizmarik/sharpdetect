// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <fstream>
#include <memory>
#include <numeric>
#include <stack>
#include <string>
#include <utility>
#include <vector>

#include "../lib/json/single_include/nlohmann/json.hpp"
#include "../lib/loguru/loguru.hpp"

#include "../LibIPC/Client.h"
#include "../LibIPC/Messages.h"
#include "../LibMetadata/AssemblyDef.h"
#include "../LibMetadata/ModuleDef.h"
#include "../LibIL/Instrumentation.h"
#include "../LibProfilerCore/PAL.h"
#include "../LibProfilerCore/StackWalker.h"
#include "../LibMetadata/WString.h"
#include "CorProfiler.h"

Profiler::CorProfiler* ProfilerInstance;
thread_local std::stack<std::vector<UINT_PTR>> ArgsCallStack;

Profiler::CorProfiler::CorProfiler(const Configuration &configuration) :
    _configuration(configuration),
    _client(
        LibIPC::QueueEndpoint{
            configuration.commandQueueName,
            configuration.commandQueueFile.value_or(std::string()),
            configuration.commandQueueSize,
            configuration.commandSemaphoreName},
        LibIPC::QueueEndpoint{
            configuration.sharedMemoryName,
            configuration.sharedMemoryFile.value_or(std::string()),
            configuration.sharedMemorySize,
            configuration.sharedMemorySemaphoreName},
        LibIPC::RegistrationEndpoint{
            configuration.registrationQueueName,
            configuration.registrationQueueFile.value_or(std::string()),
            configuration.registrationQueueSize}),
    _coreModule(0),
    _pid(static_cast<UINT32>(LibProfiler::PAL_GetCurrentPid())),
    _threadIdCacheEpoch(0),
    _argumentCapture(_corProfilerInfo, _objectsTracker),
    _typeInjector(
        _corProfilerInfo,
        _client,
        _configuration,
        _coreModule,
        _metadataStore,
        _methodDescriptorRegistry,
        _rewriteRegistry)
{
    _terminating = false;
    ProfilerInstance = this;
}

PROFILER_STUB EnterStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
PROFILER_STUB LeaveStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);
PROFILER_STUB TailcallStub(FunctionIDOrClientID functionId, COR_PRF_ELT_INFO eltInfo);

UINT_PTR STDMETHODCALLTYPE FunctionMapper(const FunctionID funcId, void* clientData, BOOL* pbHookFunction)
{
    // Resolve the per-function ELT decision once during JIT compilation
    // Enter/Exit hooks use precomputed decision
    auto const decision = ProfilerInstance->GetEltDecision(funcId, pbHookFunction);
    if (decision == nullptr)
        return funcId;

    return reinterpret_cast<UINT_PTR>(decision);
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

    _methodDescriptorRegistry.Import(_configuration.methodDescriptors, majorVersion, minorVersion, buildVersion);

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
        hr = _corProfilerInfo->SetEnterLeaveFunctionHooks3WithInfo(EnterStub, LeaveStub, TailcallStub);
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

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::Shutdown()
{
    _terminating = true;
    _client.Shutdown();
    return LibProfiler::CorProfilerBase::Shutdown();
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

    // Invalidate cached ThreadIDs: after a destroy, an OS thread could re-attach and
    // receive a different runtime thread (see GetCurrentThreadIdCached)
    _threadIdCacheEpoch.fetch_add(1, std::memory_order_release);

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
    _client.SendPriority(LibIPC::Helpers::CreateGarbageCollectionStartMsg(CreateMetadataMsg()));
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
        _client.SendPriority(LibIPC::Helpers::CreateGarbageCollectedTrackedObjectsMsg(
            CreateMetadataMsg(),
            std::move(removedTrackedObjectIds)));
    }

    const auto newSize = _objectsTracker.GetTrackedObjectsCount();
    _client.SendPriority(LibIPC::Helpers::CreateGarbageCollectionFinishMsg(CreateMetadataMsg(), oldSize, newSize));
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

    if (!_metadataStore.HasModuleDef(moduleId))
    {
        LOG_F(ERROR, "Could not resolve Module ID = %" UINT_PTR_FORMAT " for method TOK = %d.", moduleId, mdMethodDef);
        return E_FAIL;
    }
    const auto moduleDefPtr = _metadataStore.GetModuleDef(moduleId);
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

    _metadataStore.Add(moduleId, moduleDefPtr, assemblyDef.GetAssemblyId(), assemblyDefPtr);

    if (_coreModule == 0)
    {
        _coreModule = moduleDef.GetModuleId();
        LOG_F(INFO, "Identified core module: %s with handle (%" UINT_PTR_FORMAT ")", moduleDef.GetName().c_str(), moduleDef.GetModuleId());
    }

    LOG_F(INFO, "Loaded assembly: %s with handle (%" UINT_PTR_FORMAT ")", assemblyDef.GetName().c_str(), assemblyDef.GetAssemblyId());
    LOG_F(INFO, "Loaded module: %s with handle (%" UINT_PTR_FORMAT ")", moduleDef.GetFullPath().c_str(), moduleDef.GetModuleId());

    if (_coreModule == moduleId)
    {
        _typeInjector.InjectTypesForProfilingFeatures(moduleDef);
    }
    else
    {
        _typeInjector.ImportInjectedTypes(assemblyDef, moduleDef);
    }

    _typeInjector.WrapAnalyzedExternMethods(moduleDef);
    _typeInjector.ImportMethodWrappers(assemblyDef, moduleDef);
    _typeInjector.ImportCustomRecordedEventTypes(moduleDef);

    _client.Send(LibIPC::Helpers::CreateAssemblyLoadMsg(CreateMetadataMsg(), assemblyDef.GetAssemblyId(), assemblyDef.GetName()));
    _client.Send(LibIPC::Helpers::CreateModuleLoadMsg(CreateMetadataMsg(), moduleDef.GetModuleId(), assemblyDef.GetAssemblyId(), moduleDef.GetFullPath()));

    return S_OK;
}

HRESULT Profiler::CorProfiler::PatchMethodBody(const LibProfiler::ModuleDef& moduleDef, mdTypeDef mdTypeDef, mdMethodDef mdMethodDef)
{
    // If we are compiling injected method, skip it
    if (_rewriteRegistry.IsStub(moduleDef.GetModuleId(), mdMethodDef))
        return E_FAIL;

    // If there are no rewritings registered for current module, skip it
    const auto patchData = _rewriteRegistry.GetModulePatchData(moduleDef.GetModuleId());
    if (!patchData.hasAny)
        return E_FAIL;

    const auto& tokensToRewrite = patchData.tokensToRewrite;
    const auto& injectedMethods = patchData.injectedMethods;

    if (SUCCEEDED(LibProfiler::PatchMethodBody(
        *_corProfilerInfo,
        _client,
        moduleDef,
        mdMethodDef,
        tokensToRewrite,
        injectedMethods,
        _configuration.enableFieldsAccessInstrumentation,
        _configuration.skipInstrumentationForAssemblies,
        _configuration.enableStackTraceCollection,
        _configuration.stackTraceCollectionForFields)))
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

Profiler::GenericCaptureState Profiler::CorProfiler::ClassifyGenericValueCapture(
    const FunctionID functionId,
    const COR_PRF_FRAME_INFO frameInfo,
    const Profiler::MethodDescriptor& descriptor)
{
    auto const resolveModuleDef = [this](const ModuleID moduleId) -> std::shared_ptr<LibProfiler::ModuleDef>
    {
        return _metadataStore.HasModuleDef(moduleId) ? _metadataStore.GetModuleDef(moduleId) : nullptr;
    };

    auto state = GenericCaptureState::Allow;
    auto const classify = [&](const Profiler::ArgumentTypeDescriptor& type)
    {
        size_t offset = 0;
        if (!type.elementTypes.empty() && type.elementTypes[offset] == ELEMENT_TYPE_BYREF)
            ++offset;

        if (type.elementTypes.size() < offset + 2 || type.elementTypes[offset] != ELEMENT_TYPE_VAR)
            return;

        auto const typeArgIndex = static_cast<ULONG32>(type.elementTypes[offset + 1]);
        switch (LibProfiler::ClassifyClassGenericArgument(*_corProfilerInfo, resolveModuleDef, functionId, frameInfo, typeArgIndex))
        {
            case LibProfiler::GenericTypeArgKind::Reference:
                break;
            case LibProfiler::GenericTypeArgKind::Value:
                state = GenericCaptureState::Suppress;
                break;
            case LibProfiler::GenericTypeArgKind::Unknown:
                if (state != GenericCaptureState::Suppress)
                    state = GenericCaptureState::Unresolved;
                break;
        }
    };

    constexpr auto captureAsReference = static_cast<UINT>(CapturedValueFlags::CaptureAsReference);

    if (descriptor.rewritingDescriptor.returnValue.has_value() &&
        (static_cast<UINT>(descriptor.rewritingDescriptor.returnValue->flags) & captureAsReference) != 0)
    {
        classify(descriptor.signatureDescriptor.returnType);
    }

    auto const hasThis =
        (static_cast<UINT>(descriptor.signatureDescriptor.callingConvention) &
         static_cast<UINT>(CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS)) != 0;

    for (const auto& [index, value] : descriptor.rewritingDescriptor.arguments)
    {
        if (state == GenericCaptureState::Suppress)
            break;

        if ((static_cast<UINT>(value.flags) & captureAsReference) == 0)
            continue;

        if (hasThis && index == 0)
            continue;

        auto const parameterIndex = static_cast<size_t>(hasThis ? index - 1 : index);
        if (parameterIndex >= descriptor.signatureDescriptor.argumentTypeElements.size())
            continue;

        classify(descriptor.signatureDescriptor.argumentTypeElements[parameterIndex]);
    }

    return state;
}

COR_PRF_FRAME_INFO Profiler::CorProfiler::GetFrameInfo(
    const EltDecision& decision,
    const COR_PRF_ELT_INFO eltInfo,
    const EltCallbackKind callback) const
{
    COR_PRF_FRAME_INFO frameInfo { };
    if (callback == EltCallbackKind::Enter)
    {
        ULONG argumentsLength = 0;
        _corProfilerInfo->GetFunctionEnter3Info(decision.functionId, eltInfo, &frameInfo, &argumentsLength, nullptr);
    }
    else
    {
        COR_PRF_FUNCTION_ARGUMENT_RANGE returnValueInfo { };
        _corProfilerInfo->GetFunctionLeave3Info(decision.functionId, eltInfo, &frameInfo, &returnValueInfo);
    }

    return frameInfo;
}

bool Profiler::CorProfiler::ShouldSuppressGenericCapture(
    EltDecision& decision,
    const COR_PRF_ELT_INFO eltInfo,
    const EltCallbackKind callback)
{
    switch (decision.genericCapture.load(std::memory_order_relaxed))
    {
        case GenericCaptureState::Allow:
            return false;
        case GenericCaptureState::Suppress:
            return true;
        case GenericCaptureState::Unresolved:
            break;
    }

    if (decision.descriptor == nullptr)
        return false;

    auto const frameInfo = GetFrameInfo(decision, eltInfo, callback);
    auto state = ClassifyGenericValueCapture(decision.functionId, frameInfo, *decision.descriptor);
    if (state == GenericCaptureState::Unresolved)
    {
        if (frameInfo == 0)
            return true;

        state = GenericCaptureState::Suppress;
    }

    decision.genericCapture.store(state, std::memory_order_relaxed);
    return state != GenericCaptureState::Allow;
}

Profiler::EltDecision* Profiler::CorProfiler::GetEltDecision(const FunctionID functionId, BOOL* pbHookFunction)
{
    *pbHookFunction = FALSE;

    ModuleID moduleId;
    mdMethodDef methodDef;
    if (FAILED(_corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef)))
        return nullptr;

    // Inject hooks only for methods where we explicitly requested them
    const auto descriptorPointer = _methodDescriptorRegistry.TryGet(moduleId, methodDef);
    const auto shouldInjectHooks = descriptorPointer != nullptr && descriptorPointer->rewritingDescriptor.injectHooks;
    if (!shouldInjectHooks)
        return nullptr;

    const auto& descriptor = *descriptorPointer.get();
    auto const genericCapture = ClassifyGenericValueCapture(functionId, 0, descriptor);
    if (genericCapture == GenericCaptureState::Suppress)
        return nullptr;

    // An unresolved instantiation is settled by the first ELT callback, which has a frame info at hand
    *pbHookFunction = TRUE;

    auto guard = std::lock_guard(_eltDecisionMutex);
    if (const auto it = _eltDecisionLookup.find(functionId); it != _eltDecisionLookup.cend())
        return it->second;

    constexpr auto originalMethodEnterEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnter);
    constexpr auto originalMethodEnterWithArgumentsEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnterWithArguments);
    constexpr auto originalMethodExitEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodExit);
    constexpr auto originalMethodExitWithArgumentsEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodExitWithArguments);

    const auto enterMappings = _rewriteRegistry.FindMethodEnterMappings(
        moduleId, methodDef, originalMethodEnterEvent, originalMethodEnterWithArgumentsEvent);
    const auto exitMappings = _rewriteRegistry.FindMethodExitMappings(
        moduleId, methodDef, originalMethodExitEvent, originalMethodExitWithArgumentsEvent);

    auto& decision = _eltDecisions.emplace_back();
    decision.functionId = functionId;
    decision.moduleId = moduleId;
    decision.methodDef = methodDef;
    decision.descriptor = descriptorPointer.get();
    decision.enterEventId = enterMappings.hasEvent ? enterMappings.eventMapping : originalMethodEnterEvent;
    decision.enterWithArgsEventId = enterMappings.hasWithArgsEvent ? enterMappings.withArgsEventMapping : originalMethodEnterWithArgumentsEvent;
    decision.exitEventId = exitMappings.hasEvent ? exitMappings.eventMapping : originalMethodExitEvent;
    decision.exitWithArgsEventId = exitMappings.hasWithArgsEvent ? exitMappings.withArgsEventMapping : originalMethodExitWithArgumentsEvent;
    decision.hasArguments = !descriptor.rewritingDescriptor.arguments.empty();
    decision.hasReturnValue = descriptor.rewritingDescriptor.returnValue.has_value();
    decision.hasIndirects = HasIndirects(descriptor);
    decision.emitExitEvent = descriptor.rewritingDescriptor.emitExitEvent;
    decision.captureStackTraceOnEnter = descriptor.rewritingDescriptor.captureStackTraceOnEnter;
    decision.genericCapture.store(genericCapture, std::memory_order_relaxed);

    auto* stored = &decision;
    _eltDecisionLookup.emplace(functionId, stored);
    return stored;
}

HRESULT Profiler::CorProfiler::EnterMethod(const FunctionIDOrClientID functionOrClientId, const COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    auto* decision = reinterpret_cast<EltDecision*>(functionOrClientId.clientID);
    if (decision == nullptr)
        return S_OK;

    if (ShouldSuppressGenericCapture(*decision, eltInfo, EltCallbackKind::Enter))
        return S_OK;

    const auto moduleId = decision->moduleId;
    const auto methodDef = decision->methodDef;

    if (decision->descriptor == nullptr || !decision->hasArguments)
    {
        // Notify about method enter without arguments
        _client.Send(LibIPC::Helpers::CreateMethodEnterMsg(
            CreateMetadataMsg(),
            moduleId,
            methodDef,
            decision->enterEventId));
        return S_OK;
    }

    // Retrieve information about arguments
    const auto& descriptor = *decision->descriptor;

    // Retrieve arguments data
    COR_PRF_FRAME_INFO frameInfo { };
    ULONG argumentsLength = 0;
    HRESULT hr = _corProfilerInfo->GetFunctionEnter3Info(decision->functionId, eltInfo, &frameInfo, &argumentsLength, nullptr);
    if (hr != HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER))
    {
        LOG_F(ERROR, "Could not retrieve arguments info for method %d. Error: 0x%x", methodDef, hr);
        return E_FAIL;
    }

    thread_local std::vector<UINT_PTR> indirects;
    indirects.clear();
    thread_local std::vector<BYTE> rawArgumentInfos;
    rawArgumentInfos.resize(argumentsLength);
    hr = _corProfilerInfo->GetFunctionEnter3Info(decision->functionId, eltInfo, &frameInfo, &argumentsLength, reinterpret_cast<COR_PRF_FUNCTION_ARGUMENT_INFO *>(rawArgumentInfos.data()));
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not retrieve arguments data for method %d. Error: 0x%x.", methodDef, hr);
        return E_FAIL;
    }

    const auto& argumentInfos = *reinterpret_cast<COR_PRF_FUNCTION_ARGUMENT_INFO *>(rawArgumentInfos.data());
    thread_local std::vector<BYTE> argumentValues;
    thread_local std::vector<BYTE> argumentOffsets;
    argumentValues.clear();
    argumentOffsets.clear();

    // Reserve estimated capacity (exact for non-array args; arrays will grow dynamically)
    auto const estimatedValuesLength = std::accumulate(
        descriptor.rewritingDescriptor.arguments.cbegin(),
        descriptor.rewritingDescriptor.arguments.cend(),
        0, [](const INT sum, const CapturedArgumentDescriptor& d) { return sum + d.value.size; });
    auto const estimatedOffsetsLength = descriptor.rewritingDescriptor.arguments.size() * sizeof(UINT);
    argumentValues.reserve(estimatedValuesLength);
    argumentOffsets.reserve(estimatedOffsetsLength);

    hr = _argumentCapture.GetArguments(
        descriptor,
        indirects,
        argumentInfos,
        argumentValues,
        argumentOffsets);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not parse arguments data for method %d. Error: 0x%x.", methodDef, hr);
        return E_FAIL;
    }

    if (descriptor.rewritingDescriptor.returnValue.has_value() || !indirects.empty())
        ArgsCallStack.push(indirects);

    // Capture stack trace if required
    thread_local std::vector<BYTE> stackFramesBlob;
    bool hasStackFrames = false;
    if (decision->captureStackTraceOnEnter)
    {
        hasStackFrames = SUCCEEDED(LibProfiler::StackWalker::CaptureCurrentStackTrace(
                _corProfilerInfo,
                1,
                _configuration.stackTraceCollectionMaxDepth,
                stackFramesBlob))
            && !stackFramesBlob.empty();
    }

    // Notify about method enter with arguments
    _client.Send(LibIPC::Helpers::CreateMethodEnterWithArgumentsMsg(
        CreateMetadataMsg(),
        moduleId,
        methodDef,
        decision->enterWithArgsEventId,
        LibIPC::ByteSpanView { argumentValues.data(), argumentValues.size() },
        LibIPC::ByteSpanView { argumentOffsets.data(), argumentOffsets.size() },
        hasStackFrames
            ? std::make_optional(LibIPC::ByteSpanView { stackFramesBlob.data(), stackFramesBlob.size() })
            : std::nullopt));
    return S_OK;
}

HRESULT Profiler::CorProfiler::LeaveMethod(const FunctionIDOrClientID functionOrClientId, const COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    auto* decision = reinterpret_cast<EltDecision*>(functionOrClientId.clientID);
    if (decision == nullptr || !decision->emitExitEvent)
        return S_OK;

    if (ShouldSuppressGenericCapture(*decision, eltInfo, EltCallbackKind::Leave))
        return S_OK;

    const auto moduleId = decision->moduleId;
    const auto methodDef = decision->methodDef;

    if (decision->descriptor == nullptr || (!decision->hasReturnValue && !decision->hasIndirects))
    {
        // Notify about method leave without arguments
        _client.Send(LibIPC::Helpers::CreateMethodExitMsg(
            CreateMetadataMsg(),
            moduleId,
            methodDef,
            decision->exitEventId));
        return S_OK;
    }

    // Retrieve information about arguments
    const auto& descriptor = *decision->descriptor;
    auto const hasIndirects = decision->hasIndirects;
    auto const hasReturnValue = decision->hasReturnValue;

    // Retrieve return value data
    COR_PRF_FRAME_INFO frameInfo;
    COR_PRF_FUNCTION_ARGUMENT_RANGE returnValueInfo;
    HRESULT hr = _corProfilerInfo->GetFunctionLeave3Info(decision->functionId, eltInfo, &frameInfo, &returnValueInfo);
    if (FAILED(hr))
    {
        LOG_F(ERROR, "Could not retrieve return value info for method %d. Error: 0x%x", methodDef, hr);
        return E_FAIL;
    }
    thread_local std::vector<BYTE> returnValue;
    returnValue.assign(returnValueInfo.length, 0);
    if (hasReturnValue)
    {
        hr = _argumentCapture.GetReturnValue(
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
    thread_local std::vector<BYTE> argumentValues;
    thread_local std::vector<BYTE> argumentOffsets;
    argumentValues.clear();
    argumentOffsets.clear();
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
            hr = _argumentCapture.GetByRefArguments(
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
        decision->exitWithArgsEventId,
        LibIPC::ByteSpanView { returnValue.data(), returnValue.size() },
        LibIPC::ByteSpanView { argumentValues.data(), argumentValues.size() },
        LibIPC::ByteSpanView { argumentOffsets.data(), argumentOffsets.size() }));

    return S_OK;
}

HRESULT STDMETHODCALLTYPE Profiler::CorProfiler::ExceptionUnwindFunctionEnter(const FunctionID functionId)
{
    if (_terminating)
        return S_OK;

    ModuleID moduleId;
    mdMethodDef methodDef;
    HRESULT hr = _corProfilerInfo->GetFunctionInfo(functionId, nullptr, &moduleId, &methodDef);
    if (FAILED(hr))
        return S_OK;

    constexpr auto originalMethodExitEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodExit);
    constexpr auto originalMethodExitWithArgumentsEvent = static_cast<USHORT>(LibIPC::RecordedEventType::MethodExitWithArguments);
    auto const [
        hasCustomMethodExitEvent,
        customMethodExitEvent,
        hasCustomMethodExitWithArgumentsEvent,
        customMethodExitWithArgumentsEvent] =
        _rewriteRegistry.FindMethodExitMappings(
            moduleId,
            methodDef,
            originalMethodExitEvent,
            originalMethodExitWithArgumentsEvent);

    if (!hasCustomMethodExitEvent && !hasCustomMethodExitWithArgumentsEvent)
        return S_OK;

    // Balance the per-thread by-ref/return-value argument stack that EnterMethod pushed
    const auto descriptorPointer = _methodDescriptorRegistry.TryGet(moduleId, methodDef);
    if (descriptorPointer)
    {
        const auto& descriptor = *descriptorPointer.get();
        if ((descriptor.rewritingDescriptor.returnValue.has_value() || HasIndirects(descriptor)) && !ArgsCallStack.empty())
            ArgsCallStack.pop();
    }

    auto const interpretation = hasCustomMethodExitEvent ? customMethodExitEvent : customMethodExitWithArgumentsEvent;
    _client.Send(LibIPC::Helpers::CreateMethodUnwoundMsg(
        CreateMetadataMsg(),
        moduleId,
        methodDef,
        interpretation));
    return S_OK;
}

HRESULT Profiler::CorProfiler::TailcallMethod(FunctionIDOrClientID functionOrClientId, COR_PRF_ELT_INFO eltInfo)
{
    if (_terminating)
        return S_OK;

    LOG_F(WARNING, "Tailcall.");
    return E_NOTIMPL;
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

    return _methodDescriptorRegistry.TryGet(moduleId, mdMethodDef);
}

HRESULT Profiler::CorProfiler::AbortAttach(const std::string& reason)
{
    LOG_F(ERROR, "%s Terminating.", reason.c_str());
    _client.Send(LibIPC::Helpers::CreateProfilerAbortInitializeMsg(
        CreateMetadataMsg(),
        reason));
    return E_FAIL;
}

UINT64 Profiler::CorProfiler::GetCurrentThreadIdCached() const
{
    // A cached ThreadID stays valid only while the OS thread keeps its runtime thread
    // ThreadDestroyed bumps the epoch, forcing revalidation
    thread_local UINT64 cachedThreadId = 0;
    thread_local UINT64 cachedEpoch = 0;

    const auto epoch = _threadIdCacheEpoch.load(std::memory_order_acquire);
    if (cachedThreadId == 0 || cachedEpoch != epoch)
    {
        ThreadID threadId = 0;
        if (FAILED(_corProfilerInfo->GetCurrentThreadID(&threadId)))
            threadId = 0;
        cachedThreadId = threadId;
        cachedEpoch = epoch;
    }

    return cachedThreadId;
}

LibIPC::MetadataMsg Profiler::CorProfiler::CreateMetadataMsg() const
{
    return LibIPC::Helpers::CreateMetadataMsg(_pid, GetCurrentThreadIdCached());
}

LibIPC::MetadataMsg Profiler::CorProfiler::CreateMetadataMsg(const UINT64 commandId) const
{
    return LibIPC::Helpers::CreateMetadataMsg(_pid, GetCurrentThreadIdCached(), commandId);
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

    const auto frameCount = moduleIds.size();
    _client.Send(LibIPC::Helpers::CreateStackTraceSnapshotMsg(
        CreateMetadataMsg(commandId),
        threadId,
        std::move(moduleIds),
        std::move(methodTokens)));

    LOG_F(INFO, "Sent stack trace snapshot notification for thread %" UINT_PTR_FORMAT " with %zu frames (commandId: %lu).",
        threadId, frameCount, commandId);

    return S_OK;
}
