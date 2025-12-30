// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "cor.h"
#include "corprof.h"

#include "ComPtr.h"

namespace LibProfiler
{
	class ModuleDef
	{
	public:
		explicit ModuleDef(ICorProfilerInfo3& corProfilerInfo) :
			_moduleId(),
			_assemblyId(),
			_name(),
			_corProfilerInfo(corProfilerInfo),
			_metadataModuleImport(),
			_metadataModuleEmit(),
			_methodMalloc(),
			_objectTypeDef(mdTypeDefNil),
			_objectCtorDef(mdMethodDefNil),
			_objectCtorRva(0)
		{

		}

		ModuleDef(ModuleDef&& other) noexcept :
			_moduleId(other._moduleId),
			_assemblyId(other._assemblyId),
			_name(other._name),
			_fullPath(other._fullPath),
			_corProfilerInfo(other._corProfilerInfo),
			_metadataModuleImport(std::move(other._metadataModuleImport)),
			_metadataModuleEmit(std::move(other._metadataModuleEmit)),
			_methodMalloc(std::move(other._methodMalloc)),
			_objectTypeDef(other._objectTypeDef),
			_objectCtorDef(other._objectCtorDef),
			_objectCtorRva(other._objectCtorRva)
		{

		}

		ModuleDef& operator=(ModuleDef&&) = delete;
		ModuleDef(const ModuleDef& other) = delete;
		ModuleDef& operator=(const ModuleDef&) = delete;


		HRESULT Initialize(ModuleID moduleId);

		HRESULT AddTypeDef(
			IN const std::string& name, 
			IN CorTypeAttr flags, 
			IN mdToken baseType, 
			OUT mdTypeDef* typeDef) const;

		HRESULT AddMethodRef(
			IN const std::string& name,
			IN mdTypeRef typeRef,
			IN PCCOR_SIGNATURE signature,
			IN ULONG signatureLength,
			OUT mdMemberRef* memberReference) const;

		HRESULT AllocMethodBody(
			IN ULONG size,
			OUT void** body) const;

		HRESULT AddMethodDef(
			IN const std::string& name,
			IN CorMethodAttr flags,
			IN mdTypeDef typeDef,
			IN PCCOR_SIGNATURE signature,
			IN ULONG signatureLength,
			IN CorMethodImpl implFlags,
			OUT mdMethodDef* methodDefinition);

		HRESULT AddTypeRef(
			IN mdAssemblyRef declaringAssemblyRef,
			IN const std::string& typeName,
			OUT mdTypeRef* typeRef) const;

		HRESULT FindTypeDef(
			IN const std::string& name,
			OUT mdTypeDef* typeDef) const;

		HRESULT FindTypeRef(
			IN mdAssemblyRef assemblyRef,
			IN const std::string& name,
			OUT mdTypeRef* typeRef) const;

		HRESULT FindMethodDef(
			IN const std::string& name,
			IN PCCOR_SIGNATURE signature,
			IN ULONG signatureLength,
			IN mdTypeDef typeDef,
			OUT mdMethodDef* methodDef) const;

		HRESULT FindMethodRef(
			IN const std::string& name,
			IN PCCOR_SIGNATURE signature,
			IN ULONG signatureLength,
			IN mdTypeRef typeRef,
			OUT mdMemberRef* methodRef) const;

		HRESULT GetMethodProps(
			IN mdMethodDef methodDef,
			OUT mdTypeDef* typeDef,
			OUT std::string& name,
			OUT CorMethodAttr* flags,
			OUT PCCOR_SIGNATURE* signature,
			OUT ULONG* signatureLength) const;

		HRESULT GetTypeProps(
			IN mdTypeDef typeDef,
			OUT mdToken* extendsTypeDef,
			OUT std::string& name) const;

		HRESULT GetTypeRefProps(
			IN mdTypeRef typeRef,
			OUT mdToken* resolutionScope,
			OUT std::string& name) const;

		HRESULT FindImplementedInterface(
			IN mdTypeDef typeDef,
			IN const std::string& interfaceName,
			OUT mdTypeDef* implementedInterface) const;

		[[nodiscard]] const std::string& GetName() const { return _name; }
		[[nodiscard]] const std::string& GetFullPath() const { return _fullPath; }
		[[nodiscard]] constexpr AssemblyID GetAssemblyId() const { return _assemblyId; }
		[[nodiscard]] constexpr ModuleID GetModuleId() const { return _moduleId; }

	private:
		[[nodiscard]] IMetaDataImport2& GetMetadataImport() const;
		[[nodiscard]] IMetaDataEmit2& GetMetadataEmit() const;
		[[nodiscard]] IMethodMalloc& GetMethodMalloc() const;
		HRESULT GetPlaceHolderMethodRVA(OUT UINT* rva);

		template<typename TToken, typename TEnclosingToken, typename TFindFunc>
		HRESULT FindTypeWithNesting(
			IN const std::string& name,
			IN TEnclosingToken enclosingToken,
			OUT TToken* token,
			TFindFunc findFunc) const;

		static std::string GetFileNameFromPath(const std::string& path);

		ModuleID _moduleId;
		AssemblyID _assemblyId;
		std::string _name;
		std::string _fullPath;
		ICorProfilerInfo3& _corProfilerInfo;
		LibProfiler::ComPtr<IMetaDataImport2> _metadataModuleImport;
		LibProfiler::ComPtr<IMetaDataEmit2> _metadataModuleEmit;
		LibProfiler::ComPtr<IMethodMalloc> _methodMalloc;
		mdTypeDef _objectTypeDef;
		mdMethodDef _objectCtorDef;
		UINT _objectCtorRva;
	};
}