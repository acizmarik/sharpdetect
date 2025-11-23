// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <algorithm>
#include <cstring>
#include <memory>

#include "../lib/loguru/loguru.hpp"

#include "AssemblyDef.h"
#include "WString.h"
#include "PAL.h"

HRESULT LibProfiler::AssemblyDef::Initialize(const ModuleID moduleId)
{
	auto hr = _corProfilerInfo.GetModuleInfo2(moduleId, nullptr, 0, nullptr, nullptr, &_assemblyId, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain assembly ID for %" UINT_PTR_FORMAT " Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	hr = _corProfilerInfo.GetModuleMetaData(moduleId, ofRead, IID_IMetaDataAssemblyImport, reinterpret_cast<IUnknown**>(&_metadataAssemblyImport));
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain IMetaDataAssemblyImport for %" UINT_PTR_FORMAT ". Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	hr = _corProfilerInfo.GetModuleMetaData(moduleId, ofRead | ofWrite, IID_IMetaDataAssemblyEmit, reinterpret_cast<IUnknown**>(&_metadataAssemblyEmit));
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain IMetaDataAssemblyEmit for %" UINT_PTR_FORMAT ". Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	auto& metadataImport = GetMetadataAssemblyImport();

	mdAssembly mdAssembly;
	hr = metadataImport.GetAssemblyFromScope(&mdAssembly);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain assemblyId from scope. Error: 0x%x.", hr);
		return E_FAIL;
	}

	ULONG nameLength;
	hr = metadataImport.GetAssemblyProps(mdAssembly, nullptr, nullptr, nullptr, nullptr, 0, &nameLength, nullptr, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain assembly name length for scope=%d. Error: 0x%x.", mdAssembly, hr);
		return E_FAIL;
	}

	const auto nameBuffer = std::unique_ptr<WCHAR[]>(new WCHAR[nameLength]);
	hr = metadataImport.GetAssemblyProps(mdAssembly, nullptr, nullptr, nullptr, nameBuffer.get(), nameLength, nullptr, nullptr, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain assembly name scope=%d. Error: 0x%x.", mdAssembly, hr);
		return E_FAIL;
	}

	_name = ToString(nameBuffer.get());
	hr = LoadReferences();
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not load references for assembly %s. Error: 0x%x.", _name.c_str(), hr);
		return E_FAIL;
	}

	return S_OK;
}

HRESULT LibProfiler::AssemblyDef::AddOrGetAssemblyRef(
	IN const std::string& name,
	IN const void* publicKeyData,
	IN const ULONG publicKeyDataLength,
	IN const ASSEMBLYMETADATA& metadata,
	IN const DWORD flags,
	OUT mdAssemblyRef* assemblyRef,
	OUT BOOL* injectedReference) const
{
	// Check if assembly reference already exists
	if (SUCCEEDED(FindAssemblyRef(name, assemblyRef)))
	{
		*injectedReference = false;
		return S_OK;
	}

	// Otherwise create a new assembly reference
	auto& metadataAssemblyEmit = GetMetadataAssemblyEmit();
	const auto nameWstring = ToWSTRING(name);
	auto hr = metadataAssemblyEmit.DefineAssemblyRef(publicKeyData, publicKeyDataLength, nameWstring.c_str(), &metadata, nullptr, 0, flags, assemblyRef);
	if (FAILED(hr))
	{
		*injectedReference = false;
		LOG_F(ERROR, "Could not create assembly ref for assembly name=%s. Error: 0x%x.", name.c_str(), hr);
		return E_FAIL;
	}

	*injectedReference = true;
	return S_OK;
}

HRESULT LibProfiler::AssemblyDef::FindAssemblyRef(IN const std::string& name, OUT mdAssemblyRef* assemblyRef) const
{
	// Search in original references
	auto it = std::ranges::find_if(_originalReferences, [&name](const AssemblyRef& ref) { return ref.GetName() == name; });
	if (it != _originalReferences.cend())
	{
		*assemblyRef = it->GetMdAssemblyRef();
		return S_OK;
	}

	// Search in injected references
	it = std::ranges::find_if(_injectedReferences, [&name](const AssemblyRef& ref) { return ref.GetName() == name; });
	if (it != _injectedReferences.cend())
	{
		*assemblyRef = it->GetMdAssemblyRef();
		return S_OK;
	}

	// Not found
	return E_FAIL;
}

HRESULT LibProfiler::AssemblyDef::GetProps(
	OUT const void** publicKeyData,
	OUT ULONG* publicKeyDataLength,
	OUT ASSEMBLYMETADATA* metadata,
	OUT DWORD* flags) const
{
	auto& metadataImport = GetMetadataAssemblyImport();

	mdAssembly mdAssembly;
	auto hr = metadataImport.GetAssemblyFromScope(&mdAssembly);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain assemblyId from scope. Error: 0x%x.", hr);
		return E_FAIL;
	}

	hr = metadataImport.GetAssemblyProps(mdAssembly, publicKeyData, publicKeyDataLength, nullptr, nullptr, 0, nullptr, metadata, flags);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain basic assembly properties for mdAssembly=%d. Error: 0x%x.", mdAssembly, hr);
		return E_FAIL;
	}

	return S_OK;
}

IMetaDataAssemblyImport& LibProfiler::AssemblyDef::GetMetadataAssemblyImport() const
{
	return *_metadataAssemblyImport;
}

IMetaDataAssemblyEmit& LibProfiler::AssemblyDef::GetMetadataAssemblyEmit() const
{
	return *_metadataAssemblyEmit;
}

BOOL LibProfiler::AssemblyDef::ArePublicKeysEqual(
	const void* keyData1,
	const ULONG keyData1Length,
	const AssemblyRef &other)
{
	if (keyData1Length != other.GetPublicKeyDataLength())
		return false;

	return memcmp(keyData1, other.GetPublicKeyData(), other.GetPublicKeyDataLength()) == 0;
}

HRESULT LibProfiler::AssemblyDef::GetAssemblyRefProps(const mdAssemblyRef assemblyRef, std::string& name) const
{
	auto& metadataAssemblyImport = GetMetadataAssemblyImport();
	
	ULONG nameLength;
	auto hr = metadataAssemblyImport.GetAssemblyRefProps(
		assemblyRef,
		nullptr ,
		nullptr,
		nullptr,
		0,
		&nameLength,
		nullptr,
		nullptr,
		nullptr,
		nullptr);
	if (FAILED(hr))
	{
		LOG_F(WARNING, "Could not get assembly ref name length for %d in assembly=%s. Error: 0x%x.", assemblyRef, _name.c_str(), hr);
		return E_FAIL;
	}

	const auto nameBuffer = std::unique_ptr<WCHAR[]>(new WCHAR[nameLength]);
	hr = metadataAssemblyImport.GetAssemblyRefProps(
		assemblyRef,
		nullptr,
		nullptr,
		nameBuffer.get(),
		nameLength,
		nullptr,
		nullptr,
		nullptr,
		nullptr,
		nullptr);
	if (FAILED(hr))
	{
		LOG_F(WARNING, "Could not get assembly ref name for %d in assembly=%s. Error: 0x%x.", assemblyRef, _name.c_str(), hr);
		return E_FAIL;
	}

	name = ToString(nameBuffer.get());
	return S_OK;
}

HRESULT LibProfiler::AssemblyDef::LoadReferences()
{
	auto& metadataAssemblyImport = GetMetadataAssemblyImport();

	HRESULT hr;
	ULONG count;
	HCORENUM enumerator = nullptr;
	mdAssemblyRef assemblyRef;

	do
	{
		hr = metadataAssemblyImport.EnumAssemblyRefs(&enumerator, &assemblyRef, 1, &count);
		if (FAILED(hr) || count == 0)
			break;

		const void* publicKeyData;
		ULONG publicKeyDataLength;
		ULONG nameLength;
		DWORD flags;
		hr = metadataAssemblyImport.GetAssemblyRefProps(
			assemblyRef,
			&publicKeyData,
			&publicKeyDataLength,
			nullptr,
			0,
			&nameLength,
			nullptr,
			nullptr /* hash value */,
			nullptr /* hash value length */,
			&flags);

		if (FAILED(hr))
		{
			LOG_F(WARNING, "Could not read information about assembly reference=%d for assembly=%s. Error: 0x%x.", assemblyRef, _name.c_str(), hr);
			continue;
		}

		auto nameBuffer = std::unique_ptr<WCHAR[]>(new WCHAR[nameLength]);
		hr = metadataAssemblyImport.GetAssemblyRefProps(
			assemblyRef,
			nullptr,
			nullptr,
			nameBuffer.get(),
			nameLength,
			nullptr,
			nullptr,
			nullptr,
			nullptr,
			nullptr);

		if (FAILED(hr))
		{
			LOG_F(WARNING, "Could not read name of assembly reference=%d for assembly=%s. Error: 0x%x.", assemblyRef, _name.c_str(), hr);
			continue;
		}

		const auto name = LibProfiler::ToString(nameBuffer.get());
		_originalReferences.push_back(std::move(AssemblyRef(assemblyRef, name, publicKeyData, publicKeyDataLength, flags)));

	} while (SUCCEEDED(hr));

	if (enumerator != nullptr)
		metadataAssemblyImport.CloseEnum(enumerator);

	return S_OK;
}
