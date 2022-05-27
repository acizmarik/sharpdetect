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

#ifndef MODULEMETADATA_HEADER_GUARD
#define MODULEMETADATA_HEADER_GUARD

#include "cor.h"
#include "corhlpr.h"
#include "corprof.h"
#include <mutex>
#include <array>
#include <unordered_map>
#include "CComPtr.h"
#include "wstring.h"

class ModuleMetadata
{
public:

	CComPtr<IMetaDataAssemblyImport> MetadataAssemblyImport;
	CComPtr<IMetaDataAssemblyEmit> MetadataAssemblyEmit;
	CComPtr<IMetaDataImport> MetadataModuleImport;
	CComPtr<IMetaDataEmit> MetadataModuleEmit;
	CComPtr<IMethodMalloc> MethodAllocator;

	HRESULT Initialize(ICorProfilerInfo8* profiler, ModuleID moduleId);

	~ModuleMetadata() = default;
	ModuleMetadata(const ModuleMetadata&) = delete;
	ModuleMetadata& operator= (const ModuleMetadata&) = delete;
	ModuleMetadata& operator= (ModuleMetadata&&) = delete;

	ModuleMetadata()
		: MetadataAssemblyImport(nullptr),
		MetadataAssemblyEmit(nullptr),
		MetadataModuleImport(nullptr),
		MetadataModuleEmit(nullptr),
		MethodAllocator(nullptr),
		assemblyId(-1),
		coreLibraryReference(mdAssemblyNil),
		moduleDirty(false),
		moduleId(-1)
	{

	}

	ModuleMetadata(ModuleMetadata&& other) noexcept
	{
		this->MetadataAssemblyImport.Reset(std::move(other.MetadataAssemblyImport.Release()));
		this->MetadataAssemblyEmit.Reset(std::move(other.MetadataAssemblyEmit.Release()));
		this->MetadataModuleImport.Reset(std::move(other.MetadataModuleImport.Release()));
		this->MetadataModuleEmit.Reset(std::move(other.MetadataModuleEmit.Release()));
		this->MethodAllocator.Reset(std::move(other.MethodAllocator.Release()));
		this->coreLibraryReference = other.coreLibraryReference;
		this->modulePath = std::move(other.modulePath);
		this->moduleName = std::move(other.moduleName);
		this->moduleId = other.moduleId;
		this->moduleDirty = other.moduleDirty;
	}
	
	HRESULT AddCoreLibraryAssemblyRef(mdAssemblyRef assemblyRef);
	HRESULT AddTypeDef(const WSTRING& name, DWORD flags, mdToken baseType, mdToken* interfaces, mdToken& typeDef);
	HRESULT AddMethodDef(const ICorProfilerInfo8& profiler, const WSTRING& name, DWORD flags, mdTypeDef type, PCCOR_SIGNATURE signature, ULONG signatureLength, mdMethodDef& methodDef);
	
	HRESULT MakeTypeRef(ModuleMetadata& metadata, const WSTRING& typeName, mdTypeRef& token);
	HRESULT MakeMethodRef(mdTypeRef type, const WSTRING& methodName, PCCOR_SIGNATURE signature, ULONG cbSignature, mdMemberRef& token);

	HRESULT FindAssemblyRef(const WSTRING& name, mdAssemblyRef& token);
	HRESULT FindMethodDef(const WSTRING& name, mdToken enclosingType, PCCOR_SIGNATURE signature, ULONG cbSignature, mdMethodDef& token);
	HRESULT FindMethodRef(const WSTRING& name, mdTypeRef enclosingType, PCCOR_SIGNATURE signature, ULONG cbSignature, mdMemberRef& token);
	HRESULT FindTypeDef(const WSTRING& name, mdToken enclosingType, mdTypeDef& token);
	HRESULT FindTypeRef(mdAssemblyRef assemblyRef, const WSTRING& name, mdTypeRef& token);

	const WSTRING& GetName() const { return moduleName; }
	const WSTRING& GetModulePath() const { return modulePath; }
	const ModuleID GetModuleId() const { return moduleId; }

private:

	HRESULT LoadModuleInformation(ICorProfilerInfo8* profiler);
	HRESULT LoadReferences();

	std::unordered_map<WSTRING, mdAssemblyRef> assemblyReferences;

	std::mutex assemblyReferenesMutex;
	mdAssemblyRef coreLibraryReference;
	WSTRING modulePath;
	WSTRING moduleName;
	AssemblyID assemblyId;
	ModuleID moduleId;	
	bool moduleDirty;
	bool assemblyReferencesLoaded = false;
};

#endif