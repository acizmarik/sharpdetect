/*
 * Copyright (C) 2020, Andrej Čižmárik
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#include "Stdafx.h"
#include "InstrumentationContext.h"

using namespace SharpDetect::Common::Messages;

HRESULT InstrumentationContext::CreateHelperMethods(ModuleMetadata& coreLibMetadata)
{
	auto hr = E_FAIL;

	/* We need to start by injecting a static class EventsDispatcher
	 * - this class will be the declaring type for all helper methods
	 * - it must match exactly the expected implementation by the managed modules
	 */
	auto mdTokenObject = mdTypeDef(mdTokenNil);
	coreLibMetadata.FindTypeDef("System.Object"_W, mdTokenNil, mdTokenObject);
	auto eventsDispatcherFlags = 
		CorTypeAttr::tdPublic | 
		CorTypeAttr::tdAutoClass | 
		CorTypeAttr::tdAnsiClass | 
		CorTypeAttr::tdAbstract | 
		CorTypeAttr::tdSealed | 
		CorTypeAttr::tdBeforeFieldInit;
	coreLibMetadata.AddTypeDef("EventsDispatcher"_W, eventsDispatcherFlags, mdTokenObject, nullptr, mdTokenEventsDispatcherType);
	auto message = MessageFactory::TypeInjected(GetCurrentThreadId(), coreLibMetadata.GetModuleId(), mdTokenEventsDispatcherType);
	client.SendNotification(std::move(message));

	/* Now we can generate individual helper methods
	 * - the methods will have trivial implementation (they will be used for arguments obervation) 
	 * - again this must exactly match the expected implementation by the managed modules
	 */

	std::vector<COR_SIGNATURE> fieldAccessSignature = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 2, ELEMENT_TYPE_VOID, ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_I8 };
	hr = CreateHelperMethod(coreLibMetadata, "FieldAccess"_W, SharpDetect::Common::Messages::MethodType::FIELD_ACCESS, 
		fieldAccessSignature, sizeof(fieldAccessSignature), mdTokenFieldAccessMethod);
	LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not inject helper method");
	
	std::vector<COR_SIGNATURE> instanceAccessSignature = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_OBJECT };
	hr = CreateHelperMethod(coreLibMetadata, "FieldInstanceRefAccess"_W, SharpDetect::Common::Messages::MethodType::FIELD_INSTANCE_ACCESS, 
		instanceAccessSignature, sizeof(instanceAccessSignature), mdTokenFieldInstanceRefAccessMethod);
	LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not inject helper method");

	std::vector<COR_SIGNATURE> arrayAccessSignature = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_OBJECT };
	hr = CreateHelperMethod(coreLibMetadata, "ArrayInstanceRefAccess"_W, SharpDetect::Common::Messages::MethodType::ARRAY_INSTANCE_ACCESS,
		arrayAccessSignature, sizeof(arrayAccessSignature), mdTokenArrayInstanceRefAccessMethod);
	LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not inject helper method");

	std::vector<COR_SIGNATURE> arrayElementAccessSignature = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 2, ELEMENT_TYPE_VOID, ELEMENT_TYPE_BOOLEAN, ELEMENT_TYPE_I8 };
	hr = CreateHelperMethod(coreLibMetadata, "ArrayElementAccess"_W, SharpDetect::Common::Messages::MethodType::ARRAY_ELEMENT_ACCESS,
		arrayElementAccessSignature, sizeof(arrayElementAccessSignature), mdTokenArrayElementAccessMethod);
	LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not inject helper method");

	std::vector<COR_SIGNATURE> indexAccessSignature = { IMAGE_CEE_CS_CALLCONV_DEFAULT, 1, ELEMENT_TYPE_VOID, ELEMENT_TYPE_I4 };
	hr = CreateHelperMethod(coreLibMetadata, "ArrayIndexAccess"_W, SharpDetect::Common::Messages::MethodType::ARRAY_INDEX_ACCESS,
		indexAccessSignature, sizeof(indexAccessSignature), mdTokenArrayIndexAccessMethod);
	LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not inject helper method");

	return S_OK;
}

HRESULT InstrumentationContext::CreateHelperMethod(ModuleMetadata& coreLibMetadata, const WSTRING& name, MethodType type, const std::vector<COR_SIGNATURE>& signature, ULONG cbSignature, mdToken& newToken)
{
	// Generate new helper method
	TinyMethodUser emitterFieldAccess(coreLibMetadata, name, mdTokenEventsDispatcherType);
	emitterFieldAccess.SetAttributes(CorMethodAttr::mdPublic | CorMethodAttr::mdStatic, CorMethodImpl::miIL | CorMethodImpl::miManaged);
	emitterFieldAccess.SetSignature(signature.data(), cbSignature);
	emitterFieldAccess.AddInstruction(InstructionFactory::Ret());
	IfFailRet(emitterFieldAccess.Emit(corProfilerInfo, newToken));
	auto message = MessageFactory::MethodInjected(GetCurrentThreadId(), coreLibMetadata.GetModuleId(), mdTokenEventsDispatcherType, newToken, type);
	client.SendNotification(std::move(message));

	// Store information about helper
	std::lock_guard<std::mutex> lock(helpersMutex);
	helpers.emplace_back(std::make_tuple(name, type, signature, cbSignature));

	return S_OK;
}

HRESULT InstrumentationContext::WrapExternMethod(ModuleMetadata& metadata, mdTypeDef typeToken, mdMethodDef methodToken, UINT16 parametersCount)
{
	auto identifier = std::array<WCHAR, 511>();

	// Read method metadata
	DWORD flags;
	ULONG cchName, cbSignature;
	PCCOR_SIGNATURE signature;
	metadata.MetadataModuleImport->GetMethodProps(
		methodToken, nullptr, identifier.data(), identifier.size(),
		&cchName, &flags, &signature, &cbSignature, nullptr, nullptr);
	auto methodName = WSTRING(identifier.data(), cchName - 1);
	auto methodWrapperName = "."_W + WSTRING(identifier.data(), cchName - 1);

	// Get type name
	metadata.MetadataModuleImport->GetTypeDefProps(
		typeToken, identifier.data(), identifier.size(), &cchName, nullptr, nullptr);
	auto typeName = WSTRING(identifier.data(), cchName - 1);

	// Create its managed wrapper
	// Make sure the name is unique and can not be created from user-code
	// Should be sufficient to prefix the identifier with dot '.' and set SpecialName and RtSpecialName flags
	TinyMethodUser wrapper(metadata, methodWrapperName, typeToken);
	wrapper.SetAttributes(
		CorMethodAttr::mdSpecialName | CorMethodAttr::mdRTSpecialName | flags,
		CorMethodImpl::miIL | CorMethodImpl::miManaged);
	wrapper.SetSignature(signature, cbSignature);
	// Call the external method
	for (auto i = 0; i < parametersCount; i++)
		wrapper.AddInstruction(InstructionFactory::Ldarg(i));
	wrapper.AddInstruction(InstructionFactory::Call(methodToken));
	wrapper.AddInstruction(InstructionFactory::Ret());

	// Emit the wrapper
	mdMethodDef wrapperToken = mdMethodDefNil;
	IfFailRet(wrapper.Emit(corProfilerInfo, wrapperToken));

	// Notify SharpDetect.Core
	auto message = MessageFactory::MethodWrapped(0, metadata.GetModuleId(), typeToken, methodToken, wrapperToken);
	client.SendNotification(std::move(message));

	// Store information about wrapper
	std::lock_guard<std::mutex> lock(wrappersMutex);
	wrappers[metadata.GetModulePath()][typeName][methodName] = std::make_tuple(
		metadata.GetModuleId(), typeToken, methodToken, signature, cbSignature);

	return S_OK;
}

HRESULT InstrumentationContext::ImportWrappers(ModuleMetadata& metadata)
{
	std::lock_guard<std::mutex> lock(wrappersMutex);

	for (auto&& assemblyRecords : wrappers)
	{
		auto& assemblyPath = assemblyRecords.first;
		auto assemblyRef = mdAssemblyRefNil;

		// Current assembly does not reference this assembly => no need to import anything
		if (FAILED(metadata.FindAssemblyRef(assemblyPath, assemblyRef)))
			continue;

		for (auto&& typeRecords : assemblyRecords.second)
		{
			auto& typeName = typeRecords.first;
			auto typeRef = mdTypeRefNil;

			// Current module does not reference this type => import it
			if (FAILED(metadata.FindTypeRef(assemblyRef, typeName, typeRef)))
				metadata.MetadataModuleEmit->DefineTypeRefByName(assemblyRef, typeName.c_str(), &typeRef);

			for (auto&& methodData : typeRecords.second)
			{
				auto& methodName = methodData.first;
				auto methodRef = mdMemberRefNil;

				// Unpack method information
				ModuleID moduleId;
				mdTypeDef typeToken;
				mdMethodDef methodToken;
				PCCOR_SIGNATURE methodSignature;
				ULONG cbSignature;
				std::tie(moduleId, typeToken, methodToken, methodSignature, cbSignature) = methodData.second;

				auto wrapperName = "."_W + methodName;
				// Current module does not reference the managed wrapper => import it
				if (FAILED(metadata.FindMethodRef(wrapperName, typeRef, methodSignature, cbSignature, methodRef)))
				{
					// Import wrapper method
					IfFailRet(metadata.MakeMethodRef(typeRef, wrapperName, methodSignature, cbSignature, methodRef));

					// Notify SharpDetect.Core
					auto message = MessageFactory::WrapperMethodReferenced(0, moduleId, typeToken, methodToken,
						metadata.GetModuleId(), typeRef, methodRef);
					client.SendNotification(std::move(message));
				}
			}
		}
	}

	return S_OK;
}

HRESULT InstrumentationContext::ImportHelpers(ModuleMetadata& metadata)
{
	auto hr = E_FAIL;
	auto coreLibraryRef = mdAssemblyRef(mdAssemblyRefNil);
	
	// Add assembly reference for core library if not available
	if (FAILED(metadata.FindAssemblyRef(GetCoreLibraryName(), coreLibraryRef)))
	{
		hr = metadata.MetadataAssemblyEmit->DefineAssemblyRef(corLibPublicKey, corLibPublicKeySize,
			GetCoreLibraryName().c_str(), &corLibMetadata, nullptr, 0, corLibFlags, &coreLibraryRef);
		metadata.AddCoreLibraryAssemblyRef(coreLibraryRef);
		LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not add an assembly reference to core library in assembly " + ToString(metadata.GetModulePath()));
	}

	// Import helper type
	auto eventsDispatcherRef = mdTypeRef(mdTypeRefNil);
	hr = metadata.MetadataModuleEmit->DefineTypeRefByName(coreLibraryRef, "EventsDispatcher"_W.c_str(), &eventsDispatcherRef);
	LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not define type reference to type EventsDispatcher in module " + ToString(metadata.GetModulePath()));

	std::lock_guard<std::mutex> lock(helpersMutex);
	for (auto&& helper : helpers)
	{
		auto [name, type, signature, cbSignature] = helper;

		// Import helper method
		auto methodRef = mdMemberRef(mdMemberRefNil);
		hr = metadata.MetadataModuleEmit->DefineMemberRef(eventsDispatcherRef, name.c_str(), signature.data(), cbSignature, &methodRef);
		LOG_ERROR_AND_RET_IF(FAILED(hr), (&logger), "Could not define member reference to method " + ToString(name));

		// Notify SharpDetect about new member reference
		auto memberReferenceMessage = MessageFactory::HelperMethodReferenced(GetCurrentThreadId(), metadata.GetModuleId(), eventsDispatcherRef, methodRef, type);
		client.SendNotification(std::move(memberReferenceMessage));
	}

	return hr;
}

HRESULT InstrumentationContext::ImportCoreLibInfo(ModuleMetadata& coreLibMetadata)
{
	// Get core library metadata
	auto mda = mdAssembly(mdAssemblyNil);
	ZeroMemory(&corLibMetadata, sizeof(ASSEMBLYMETADATA));
	IfFailRet(coreLibMetadata.MetadataAssemblyImport->GetAssemblyFromScope(&mda));
	IfFailRet(coreLibMetadata.MetadataAssemblyImport->GetAssemblyProps(mda, &corLibPublicKey, &corLibPublicKeySize,
		nullptr, nullptr, 0, nullptr, &corLibMetadata, &corLibFlags));
	
	return S_OK;
}
