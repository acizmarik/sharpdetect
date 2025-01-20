// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <memory>
#include <string>
#include <vector>

#include "cor.h"
#include "corprof.h"

#include "ComPtr.h"
#include "AssemblyRef.h"

namespace LibProfiler
{
	class AssemblyDef
	{
	public:
		AssemblyDef(ICorProfilerInfo3& corProfilerInfo) : 
			_name(), 
			_assemblyId(), 
			_corProfilerInfo(corProfilerInfo), 
			_metadataAssemblyImport(), 
			_metadataAssemblyEmit(), 
			_originalReferences(), 
			_injectedReferences()
		{

		}

		AssemblyDef(AssemblyDef&& other) noexcept :
			_name(other._name),
			_assemblyId(other._assemblyId),
			_corProfilerInfo(other._corProfilerInfo),
			_metadataAssemblyImport(std::move(other._metadataAssemblyImport)),
			_metadataAssemblyEmit(std::move(other._metadataAssemblyEmit)),
			_originalReferences(std::move(other._originalReferences)),
			_injectedReferences(std::move(other._injectedReferences))
		{

		}

		AssemblyDef& operator=(AssemblyDef&&) = delete;
		AssemblyDef(const AssemblyDef& other) = delete;
		AssemblyDef& operator=(const AssemblyDef&) = delete;

		HRESULT Initialize(ModuleID moduleId);

		HRESULT AddOrGetAssemblyRef(
			IN const std::string& name, 
			IN const void* publicKeyData,
			IN ULONG publicKeyDataLength,
			IN ASSEMBLYMETADATA& metadata,
			IN DWORD flags, 
			OUT mdAssemblyRef* assemblyRef,
			OUT BOOL* injectedReference);

		HRESULT FindAssemblyRef(
			IN const std::string& name,
			OUT mdAssemblyRef* assemblyRef);
		
		HRESULT GetProps(
			OUT const void** publicKeyData, 
			OUT ULONG* publicKeyDataLength, 
			OUT ASSEMBLYMETADATA* metadata, 
			OUT DWORD* flags);

		HRESULT GetAssemblyRefProps(
			IN mdAssemblyRef assemblyRef,
			OUT std::string& name);

		const std::string& GetName() const { return _name; }
		const AssemblyID GetAssemblyId() const { return _assemblyId; }
		const std::vector<LibProfiler::AssemblyRef>& GetOriginalReferences() const { return _originalReferences; }
		
	private:
		IMetaDataAssemblyImport& GetMetadataAssemblyImport() const;
		IMetaDataAssemblyEmit& GetMetadataAssemblyEmit() const;
		BOOL ArePublicKeysEqual(const void* keyData1, ULONG keyData1Length, LibProfiler::AssemblyRef other);
		BOOL AreMetadataEqual(ASSEMBLYMETADATA& metadata1, LibProfiler::AssemblyRef other);
		HRESULT LoadReferences();

		std::string _name;
		AssemblyID _assemblyId;
		ICorProfilerInfo3& _corProfilerInfo;
		LibProfiler::ComPtr<IMetaDataAssemblyImport> _metadataAssemblyImport;
		LibProfiler::ComPtr<IMetaDataAssemblyEmit> _metadataAssemblyEmit;
		std::vector<LibProfiler::AssemblyRef> _originalReferences;
		std::vector<LibProfiler::AssemblyRef> _injectedReferences;
	};
}