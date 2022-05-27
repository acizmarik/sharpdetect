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

 // Based on project microsoftarchive/clrprofiler/ILRewrite
 // Original source: https://github.com/microsoftarchive/clrprofiler/tree/master/ILRewrite
 // Copyright (c) .NET Foundation and contributors. All rights reserved.
 // Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "Stdafx.h"
#include "ModuleMetadata.h"
#include <memory>

HRESULT ModuleMetadata::Initialize(ICorProfilerInfo8 * profiler, ModuleID moduleId)
{
	this->moduleId = moduleId;

	auto hr = HRESULT(E_FAIL);
	IMetaDataImport* metadataModuleImport;
	IMetaDataAssemblyImport* metadataAssemblyImport;
	IMetaDataEmit* metadataModuleEmit;
	IMetaDataAssemblyEmit* metadataAssemblyEmit;
	IMethodMalloc* methodAllocator;
	IfFailRet(profiler->GetModuleMetaData(moduleId, CorOpenFlags::ofRead, IID_IMetaDataImport, (IUnknown**)&metadataModuleImport));
	IfFailRet(profiler->GetModuleMetaData(moduleId, CorOpenFlags::ofRead, IID_IMetaDataAssemblyImport, (IUnknown**)&metadataAssemblyImport));
	IfFailRet(profiler->GetModuleMetaData(moduleId, CorOpenFlags::ofRead | CorOpenFlags::ofWrite, IID_IMetaDataEmit, (IUnknown**)&metadataModuleEmit));
	IfFailRet(profiler->GetModuleMetaData(moduleId, CorOpenFlags::ofRead | CorOpenFlags::ofWrite, IID_IMetaDataAssemblyEmit, (IUnknown**)&metadataAssemblyEmit));
	IfFailRet(profiler->GetILFunctionBodyAllocator(moduleId, &methodAllocator));
	MetadataAssemblyImport = std::move(CComPtr<IMetaDataAssemblyImport>(metadataAssemblyImport));
	MetadataAssemblyEmit = std::move(CComPtr<IMetaDataAssemblyEmit>(metadataAssemblyEmit));
	MetadataModuleImport = std::move(CComPtr<IMetaDataImport>(metadataModuleImport));
	MetadataModuleEmit = std::move(CComPtr<IMetaDataEmit>(metadataModuleEmit));
	MethodAllocator = std::move(CComPtr<IMethodMalloc>(methodAllocator));
	IfFailRet(LoadModuleInformation(profiler));
	return hr;
}

HRESULT ModuleMetadata::AddCoreLibraryAssemblyRef(mdAssemblyRef assemblyRef)
{
	auto lock = std::unique_lock<std::mutex>(assemblyReferenesMutex);
	assemblyReferences.emplace("System.Private.CoreLib.dll"_W, assemblyRef);
	return S_OK;
}

HRESULT ModuleMetadata::AddTypeDef(const WSTRING& name, DWORD flags, mdToken baseType, mdToken* interfaces, mdToken& typeDef)
{
	return MetadataModuleEmit->DefineTypeDef(name.c_str(), flags, baseType, interfaces, &typeDef);
}

HRESULT ModuleMetadata::AddMethodDef(const ICorProfilerInfo8& profiler, const WSTRING& name, DWORD flags, mdTypeDef type, PCCOR_SIGNATURE signature, ULONG signatureLength, mdMethodDef& methodDef)
{
	COR_SIGNATURE objectCtorSignature[] =
	{
		IMAGE_CEE_CS_CALLCONV_HASTHIS, 0, ELEMENT_TYPE_VOID
	};

	auto hr = HRESULT(E_FAIL);
	auto typeToken = mdTypeDef(mdTypeDefNil);
	auto methodToken = mdMethodDef(mdMethodDefNil);
	// Find code RVA for System.Object::.ctor() as CLR does not like to receive 0...
	auto codeRVA = ULONG(0);
	IfFailRet(FindTypeDef("System.Object"_W, mdTokenNil, std::ref(typeToken)));
	IfFailRet(FindMethodDef(".ctor"_W, typeToken, objectCtorSignature, sizeof(objectCtorSignature), std::ref(methodToken)));
	IfFailRet(MetadataModuleImport->GetMethodProps(methodToken, nullptr, nullptr, 0, nullptr, nullptr, nullptr, nullptr, &codeRVA, nullptr));
	
	// Generate new method
	IfFailRet(MetadataModuleEmit->DefineMethod(type, name.c_str(), flags, signature, signatureLength, 
		codeRVA, miIL | miManaged | miPreserveSig, &methodDef));
	
	// Generate parameters
	WSTRINGSTREAM sstream;
	for (auto paramIndex = 1; paramIndex <= signature[1]; paramIndex++)
	{
		sstream.clear();
		sstream << "arg_"_W << paramIndex;

		auto mdParam = mdParamDef(mdParamDefNil);
		IfFailRet(MetadataModuleEmit->DefineParam(methodDef, paramIndex, sstream.str().c_str(), 0, ELEMENT_TYPE_VOID, 0, -1, &mdParam));
	}

	return S_OK;
}

HRESULT ModuleMetadata::MakeMethodRef(mdTypeRef type, const WSTRING& methodName, PCCOR_SIGNATURE signature, ULONG cbSignature, mdMemberRef& token)
{
	return MetadataModuleEmit->DefineMemberRef(type, const_cast<WSTRING&>(methodName).c_str(), signature, cbSignature, &token);
}

HRESULT ModuleMetadata::FindAssemblyRef(const WSTRING& name, mdAssemblyRef& token)
{
	auto lock = std::unique_lock<std::mutex>(assemblyReferenesMutex);
	if (!assemblyReferencesLoaded)
		LoadReferences();
	
#ifdef UNIX
	auto separator = '/';
#endif
#ifdef WIN32
	auto separator = '\\';
#endif

	auto assemblyNameStartIndex = name.find_last_of(separator);
	auto assemblyName = name.substr(assemblyNameStartIndex + 1);

	auto result = assemblyReferences.find(assemblyName);
	if (result == assemblyReferences.cend())
		return E_FAIL;

	token = result->second;
	return S_OK;
}

HRESULT ModuleMetadata::LoadReferences()
{
	const auto maxReferencesCount = ULONG(1024);
	const auto maxNameCharactersCount = ULONG(1024);

	auto hr = HRESULT(E_FAIL);
	auto enumerator = HCORENUM(nullptr);	
	auto referencesCount = ULONG(0);
	auto charactersCount = ULONG(0);
	auto referencesBuffer = std::array<mdAssemblyRef, maxReferencesCount>();
	auto nameBuffer = std::array<WCHAR, maxNameCharactersCount>();
	auto publicKeyToken = static_cast<const void**>(nullptr);
	auto publicKeyLength = ULONG(0);
	auto assemblyMetadata = ASSEMBLYMETADATA();
	auto hash = static_cast<const void**>(nullptr);;
	auto hashLength = ULONG(0);
	auto flags = DWORD(0);

	// Load assembly references
	do
	{
		hr = MetadataAssemblyImport->EnumAssemblyRefs(&enumerator, referencesBuffer.data(), maxReferencesCount, &referencesCount);
		if (referencesCount == 0)
			break;

		for (size_t i = 0; i < referencesCount; i++)
		{
			auto result = MetadataAssemblyImport->GetAssemblyRefProps(
				referencesBuffer.at(i), publicKeyToken, &publicKeyLength,
				nameBuffer.data(), maxNameCharactersCount, &charactersCount,
				&assemblyMetadata, 
				hash, &hashLength, &flags);

			auto&& name = WSTRING(nameBuffer.data());
			assemblyReferences.emplace(name, referencesBuffer.at(i));
		}
	} while (hr == S_OK);

	MetadataAssemblyImport->CloseEnum(enumerator);
	assemblyReferencesLoaded = true;
	return S_OK;
}

HRESULT ModuleMetadata::FindMethodDef(const WSTRING& name, mdToken enclosingType, PCCOR_SIGNATURE signature, ULONG signatureLength, mdMethodDef& token)
{
	return MetadataModuleImport->FindMethod(enclosingType, const_cast<WSTRING&>(name).c_str(), signature, signatureLength, &token);
}

HRESULT ModuleMetadata::FindMethodRef(const WSTRING& name, mdTypeRef enclosingType, PCCOR_SIGNATURE signature, ULONG signatureLength, mdMemberRef& token)
{
	return MetadataModuleImport->FindMemberRef(enclosingType, const_cast<WSTRING&>(name).c_str(), signature, signatureLength, &token);
}

HRESULT ModuleMetadata::FindTypeDef(const WSTRING& name, mdToken enclosingType, mdTypeDef& token)
{
	return MetadataModuleImport->FindTypeDefByName(const_cast<WSTRING&>(name).c_str(), enclosingType, &token);
}

HRESULT ModuleMetadata::FindTypeRef(mdAssemblyRef assemblyRef, const WSTRING& name, mdTypeRef& token)
{
	return MetadataModuleImport->FindTypeRef(assemblyRef, const_cast<WSTRING&>(name).c_str(), &token);
}

HRESULT ModuleMetadata::LoadModuleInformation(ICorProfilerInfo8* profiler)
{
	// Load module path
	auto hr = HRESULT(E_FAIL);
	auto pathLength = ULONG(0);
	IfFailRet(profiler->GetModuleInfo2(moduleId, nullptr, 0, &pathLength, nullptr, &assemblyId, nullptr));
	modulePath = WSTRING(pathLength, '-');
	
	// Get module name
	IfFailRet(profiler->GetModuleInfo2(moduleId, nullptr, pathLength, nullptr, &modulePath[0], nullptr, nullptr));
	TrimNullTerminator(modulePath);
#ifdef UNIX
	auto separator = '/';
#endif
#ifdef WIN32
	auto separator = '\\';
#endif
	
	auto index = modulePath.find_last_of(separator);
	moduleName = modulePath.substr(index + 1);

	return S_OK;
}
