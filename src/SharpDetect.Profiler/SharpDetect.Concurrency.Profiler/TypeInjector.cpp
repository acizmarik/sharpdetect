// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <string>
#include <unordered_map>
#include <vector>

#include "../lib/loguru/loguru.hpp"

#include "../LibIL/Instrumentation.h"
#include "../LibProfilerCore/PAL.h"

#include "TypeInjector.h"

Profiler::TypeInjector::TypeInjector(
    ICorProfilerInfo10*& corProfilerInfo,
    LibIPC::Client& client,
    const Configuration& configuration,
    const ModuleID& coreModule,
    MetadataStore& metadataStore,
    MethodDescriptorRegistry& methodDescriptorRegistry,
    RewriteRegistry& rewriteRegistry) :
    _corProfilerInfo(corProfilerInfo),
    _client(client),
    _configuration(configuration),
    _coreModule(coreModule),
    _metadataStore(metadataStore),
    _methodDescriptorRegistry(methodDescriptorRegistry),
    _rewriteRegistry(rewriteRegistry)
{
}

LibIPC::MetadataMsg Profiler::TypeInjector::CreateMetadataMsg() const
{
    ThreadID threadId = 0;
    if (FAILED(_corProfilerInfo->GetCurrentThreadID(&threadId)))
        threadId = 0;
    return LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId);
}

static void AppendCompressedToken(mdToken token, std::vector<COR_SIGNATURE>& result)
{
    BYTE bytes[4];
    const ULONG length = CorSigCompressToken(token, bytes);
    result.insert(result.end(), bytes, bytes + length);
}

static void AppendCompressedData(ULONG value, std::vector<COR_SIGNATURE>& result)
{
    BYTE bytes[4];
    const ULONG length = CorSigCompressData(value, bytes);
    result.insert(result.end(), bytes, bytes + length);
}

static HRESULT SerializeArgumentTypeDescriptor(
    const Profiler::ArgumentTypeDescriptor& descriptor,
    const LibProfiler::ModuleDef& moduleDef,
    std::vector<COR_SIGNATURE>& result)
{
    const auto& elementTypes = descriptor.elementTypes;

    for (size_t i = 0; i < elementTypes.size(); )
    {
        const auto elementType = elementTypes[i];
        result.push_back(elementType);

        if (elementType == CorElementType::ELEMENT_TYPE_GENERICINST)
        {
            if (i + 1 >= elementTypes.size())
            {
                LOG_F(ERROR, "GENERICINST is missing its generic type definition kind.");
                return E_FAIL;
            }

            result.push_back(elementTypes[i + 1]);

            mdTypeDef typeDef;
            if (FAILED(moduleDef.FindTypeDef(descriptor.typeName.value(), &typeDef)))
            {
                LOG_F(ERROR, "Generic type %s not found in module", descriptor.typeName.value().c_str());
                return E_FAIL;
            }
            AppendCompressedToken(typeDef, result);

            const ULONG genericArgumentCount = static_cast<ULONG>(elementTypes.size() - (i + 2));
            AppendCompressedData(genericArgumentCount, result);

            i += 2;
            continue;
        }

        if (elementType == CorElementType::ELEMENT_TYPE_CLASS || elementType == CorElementType::ELEMENT_TYPE_VALUETYPE)
        {
            mdTypeDef typeDef;
            if (FAILED(moduleDef.FindTypeDef(descriptor.typeName.value(), &typeDef)))
            {
                // FIXME: add support for type references
                LOG_F(ERROR, "Type %s not found in module", descriptor.typeName.value().c_str());
                return E_FAIL;
            }
            AppendCompressedToken(typeDef, result);
        }

        ++i;
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

HRESULT Profiler::TypeInjector::WrapAnalyzedExternMethods(LibProfiler::ModuleDef& moduleDef)
{
    std::unordered_map<mdToken, mdToken> rewritingsBuilder;

    {
        for (auto&& methodPtr : _methodDescriptorRegistry.Descriptors())
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
            _rewriteRegistry.AddStub(moduleDef.GetModuleId(), wrapperMethodDef);

            _client.Send(LibIPC::Helpers::CreateMethodWrapperInjectionMsg(CreateMetadataMsg(), moduleDef.GetModuleId(), typeDef, methodDef, wrapperMethodDef, wrapperMethodName));

            LOG_F(INFO, "Wrapped %s::%s (%d) -> (%d) in module %s.",
                declaringTypeFullName.c_str(),
                methodName.c_str(), methodDef,
                wrapperMethodDef,
                moduleDef.GetName().c_str());
        }
    }

    _rewriteRegistry.AddModuleRewritings(moduleDef.GetModuleId(), rewritingsBuilder);
    return S_OK;
}

HRESULT Profiler::TypeInjector::ImportMethodWrappers(const LibProfiler::AssemblyDef& assemblyDef, const LibProfiler::ModuleDef& moduleDef)
{
    for (auto&& methodPointer : _methodDescriptorRegistry.Descriptors())
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

HRESULT Profiler::TypeInjector::ImportMethodWrapper(
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
    _rewriteRegistry.AddRewriting(moduleDef.GetModuleId(), methodRef, wrapperMethodRef);

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

HRESULT Profiler::TypeInjector::ImportCustomRecordedEventTypes(const LibProfiler::ModuleDef& moduleDef)
{
    const auto moduleRewritings = _rewriteRegistry.GetModuleRewritings(moduleDef.GetModuleId());

    for (auto&& methodPointer : _methodDescriptorRegistry.Descriptors())
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
            _rewriteRegistry.AddMethodEnterMapping(
                moduleId,
                sourceToken,
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnter),
                mapping);

            _rewriteRegistry.AddMethodEnterMapping(
                moduleId,
                sourceToken,
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodEnterWithArguments),
                mapping);
        }
        if (rewritingDescriptor.methodExitInterpretation.has_value())
        {
            auto const mapping = rewritingDescriptor.methodExitInterpretation.value();
            _rewriteRegistry.AddMethodExitMapping(
                moduleId,
                sourceToken,
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodExit),
                mapping);

            _rewriteRegistry.AddMethodExitMapping(
                moduleId,
                sourceToken,
                static_cast<USHORT>(LibIPC::RecordedEventType::MethodExitWithArguments),
                mapping);
        }

        _methodDescriptorRegistry.AddLookup(moduleDef.GetModuleId(), sourceToken, methodPointer);
        LOG_F(INFO, "Imported custom event on method %s::%s (%d) in module %s.", declaringTypeFullName.c_str(), methodName.c_str(), sourceToken, moduleDef.GetName().c_str());
    }

    return S_OK;
}

HRESULT Profiler::TypeInjector::InjectTypesForProfilingFeatures(LibProfiler::ModuleDef &moduleDef)
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

        _client.Send(LibIPC::Helpers::CreateTypeDefinitionInjectionMsg(
            CreateMetadataMsg(),
            moduleDef.GetModuleId(),
            injectedTypeDef,
            typeFullName));

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
            _rewriteRegistry.AddStub(moduleDef.GetModuleId(), injectedMethodDef);
        }

        _rewriteRegistry.AddModuleInjectedMethods(moduleDef.GetModuleId(), injectedMethods);
    }

    return S_OK;
}

HRESULT Profiler::TypeInjector::ImportInjectedTypes(
    const LibProfiler::AssemblyDef &assemblyDef,
    const LibProfiler::ModuleDef &moduleDef)
{
    if (_configuration.typeInjectionDescriptors.empty())
        return S_OK;

    const auto& coreModuleDef = *_metadataStore.GetModuleDef(_coreModule).get();
    const auto& coreAssemblyDef = *_metadataStore.GetAssemblyDef(coreModuleDef.GetAssemblyId()).get();
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

        _rewriteRegistry.AddModuleInjectedMethods(moduleDef.GetModuleId(), injectedMethods);
    }

    return S_OK;
}
