// Copyright 2026 Andrej Čižmárik and Contributors
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
		explicit AssemblyDef(ICorProfilerInfo3& corProfilerInfo) :
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
			IN const ASSEMBLYMETADATA& metadata,
			IN DWORD flags, 
			OUT mdAssemblyRef* assemblyRef,
			OUT BOOL* injectedReference) const;

		HRESULT FindAssemblyRef(
			IN const std::string& name,
			OUT mdAssemblyRef* assemblyRef) const;
		
		HRESULT GetProps(
			OUT const void** publicKeyData, 
			OUT ULONG* publicKeyDataLength, 
			OUT ASSEMBLYMETADATA* metadata, 
			OUT DWORD* flags) const;

		HRESULT GetAssemblyRefProps(
			IN mdAssemblyRef assemblyRef,
			OUT std::string& name) const;

		[[nodiscard]] const std::string& GetName() const { return _name; }
		[[nodiscard]] constexpr AssemblyID GetAssemblyId() const { return _assemblyId; }
		[[nodiscard]] const std::vector<AssemblyRef>& GetOriginalReferences() const { return _originalReferences; }
		
	private:
		[[nodiscard]] IMetaDataAssemblyImport& GetMetadataAssemblyImport() const;
		[[nodiscard]] IMetaDataAssemblyEmit& GetMetadataAssemblyEmit() const;

		static BOOL ArePublicKeysEqual(const void* keyData1, ULONG keyData1Length, const AssemblyRef &other);
		BOOL AreMetadataEqual(ASSEMBLYMETADATA& metadata1, AssemblyRef other);
		HRESULT LoadReferences();

		std::string _name;
		AssemblyID _assemblyId;
		ICorProfilerInfo3& _corProfilerInfo;
		ComPtr<IMetaDataAssemblyImport> _metadataAssemblyImport;
		ComPtr<IMetaDataAssemblyEmit> _metadataAssemblyEmit;
		std::vector<AssemblyRef> _originalReferences;
		std::vector<AssemblyRef> _injectedReferences;
	};
}