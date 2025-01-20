// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <memory>
#include <string>

#include "../lib/loguru/loguru.hpp"

#include "ModuleDef.h"
#include "PAL.h"
#include "WString.h"

HRESULT LibProfiler::ModuleDef::Initialize(ModuleID moduleId)
{
	auto hr = _corProfilerInfo.GetModuleMetaData(moduleId, ofRead, IID_IMetaDataImport2, reinterpret_cast<IUnknown**>(&_metadataModuleImport));
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain IMetaDataImport for %" UINT_PTR_FORMAT ". Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	hr = _corProfilerInfo.GetModuleMetaData(moduleId, ofRead | ofWrite, IID_IMetaDataEmit2, reinterpret_cast<IUnknown**>(&_metadataModuleEmit));
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain IMetaDataEmit for %" UINT_PTR_FORMAT ". Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	hr = _corProfilerInfo.GetILFunctionBodyAllocator(moduleId, &_methodMalloc);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain IMethodMalloc for %" UINT_PTR_FORMAT ". Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	ULONG nameLength;
	hr = _corProfilerInfo.GetModuleInfo2(moduleId, nullptr, 0, &nameLength, nullptr, &_assemblyId, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain module information for %" UINT_PTR_FORMAT ". Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	auto nameBuffer = std::unique_ptr<WCHAR[]>(new WCHAR[nameLength]);
	hr = _corProfilerInfo.GetModuleInfo2(moduleId, nullptr, nameLength, nullptr, nameBuffer.get(), nullptr, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain module path for %" UINT_PTR_FORMAT ". Error: 0x%x.", moduleId, hr);
		return E_FAIL;
	}

	_moduleId = moduleId;
	_fullPath = LibProfiler::ToString(nameBuffer.get());
	_name = GetFileNameFromPath(_fullPath);
	return S_OK;
}

std::string LibProfiler::ModuleDef::GetFileNameFromPath(const std::string& path) {
	auto lastDelimiter = path.find_last_of("/\\");
	if (lastDelimiter == std::string::npos) {
		return path;
	}

	return path.substr(lastDelimiter + 1);
}

HRESULT LibProfiler::ModuleDef::AddTypeDef(IN const std::string& name, IN CorTypeAttr flags, IN mdToken baseType, OUT mdTypeDef* typeDef)
{
	auto& metadataEmit = GetMetadataEmit();
	auto nameWstring = LibProfiler::ToWSTRING(name);
	return metadataEmit.DefineTypeDef(nameWstring.c_str(), flags, baseType, nullptr, typeDef);
}

HRESULT LibProfiler::ModuleDef::AllocMethodBody(IN ULONG size, OUT void** body)
{
	auto& methodMalloc = GetMethodMalloc();
	auto memory = methodMalloc.Alloc(size);
	if (memory == nullptr)
	{
		LOG_F(ERROR, "Could not allocate method body for size=%" ULONG_FORMAT ".", size);
		return E_FAIL;
	}

	*body = memory;
	return S_OK;
}

HRESULT LibProfiler::ModuleDef::AddMethodDef(IN const std::string& name, IN CorMethodAttr flags, IN mdTypeDef typeDef, IN PCCOR_SIGNATURE signature, IN ULONG signatureLength, IN CorMethodImpl implFlags, OUT mdMethodDef* methodDefinition)
{
	UINT rva;
	auto hr = GetPlaceHolderMethodRVA(&rva);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not placeholder RVA. Error: 0x%x.", hr);
		return E_FAIL;
	}

	auto& metadataEmit = GetMetadataEmit();
	auto nameWstring = LibProfiler::ToWSTRING(name);
	hr = metadataEmit.DefineMethod(typeDef, nameWstring.c_str(), flags, signature, signatureLength, rva, implFlags, methodDefinition);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not create method with name=%s. Error: 0x%x.", name.c_str(), hr);
		return E_FAIL;
	}

	return S_OK;
}

HRESULT LibProfiler::ModuleDef::AddTypeRef(IN mdAssemblyRef declaringAssemblyRef, IN const std::string& typeName, OUT mdTypeRef* typeRef)
{
	auto& metadataEmit = GetMetadataEmit();
	auto nameWstring = LibProfiler::ToWSTRING(typeName);
	return metadataEmit.DefineTypeRefByName(declaringAssemblyRef, nameWstring.c_str(), typeRef);
}

HRESULT LibProfiler::ModuleDef::FindTypeDef(IN const std::string& name, OUT mdTypeDef* typeDef)
{
	auto& metadataImport = GetMetadataImport();
	auto nameWstring = LibProfiler::ToWSTRING(name);
	return metadataImport.FindTypeDefByName(nameWstring.c_str(), mdTokenNil, typeDef);
}

HRESULT LibProfiler::ModuleDef::FindTypeRef(IN mdAssemblyRef assemblyRef, IN const std::string& name, OUT mdTypeRef* typeRef)
{
	auto& metadataImport = GetMetadataImport();
	auto nameWstring = LibProfiler::ToWSTRING(name);
	return metadataImport.FindTypeRef(assemblyRef, nameWstring.c_str(), typeRef);
}

HRESULT LibProfiler::ModuleDef::FindMethodDef(IN const std::string& name, IN PCCOR_SIGNATURE signature, IN ULONG signatureLength, IN mdTypeDef typeDef, OUT mdMethodDef* methodDef)
{
	auto& metadataImport = GetMetadataImport();
	auto nameWstring = LibProfiler::ToWSTRING(name);
	return metadataImport.FindMethod(typeDef, nameWstring.c_str(), signature, signatureLength, methodDef);
}

HRESULT LibProfiler::ModuleDef::FindMethodRef(IN const std::string& name, IN PCCOR_SIGNATURE signature, IN ULONG signatureLength, IN mdTypeRef typeRef, OUT mdMemberRef* methodRef)
{
	auto& metadataImport = GetMetadataImport();
	auto nameWstring = LibProfiler::ToWSTRING(name);
	return metadataImport.FindMemberRef(typeRef, nameWstring.c_str(), signature, signatureLength, methodRef);
}

HRESULT LibProfiler::ModuleDef::AddMethodRef(IN const std::string& name, IN mdTypeRef typeRef, IN PCCOR_SIGNATURE signature, IN ULONG signatureLength, OUT mdMemberRef* memberReference)
{
	auto& metadataEmit = GetMetadataEmit();
	auto nameWstring = LibProfiler::ToWSTRING(name);
	return metadataEmit.DefineMemberRef(typeRef, nameWstring.c_str(), signature, signatureLength, memberReference);
}

IMetaDataImport2& LibProfiler::ModuleDef::GetMetadataImport() const
{
	return *_metadataModuleImport;
}

IMetaDataEmit2& LibProfiler::ModuleDef::GetMetadataEmit() const
{
	return *_metadataModuleEmit;
}

IMethodMalloc& LibProfiler::ModuleDef::GetMethodMalloc() const
{
	return *_methodMalloc;
}

HRESULT LibProfiler::ModuleDef::GetMethodProps(
	IN mdMethodDef methodDef,
	OUT mdTypeDef* typeDef,
	OUT std::string& name,
	OUT CorMethodAttr* flags,
	OUT PCCOR_SIGNATURE* signature,
	OUT ULONG* signatureLength)
{
	auto& metadataImport = GetMetadataImport();
	ULONG methodNameLength;
	DWORD methodFlags;
	auto hr = metadataImport.GetMethodProps(methodDef, typeDef, nullptr, 0, &methodNameLength, &methodFlags, signature, signatureLength, nullptr, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not get method props for mdMethodDef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x.", methodDef, _moduleId, hr);
		return E_FAIL;
	}

	auto nameBuffer = std::unique_ptr<WCHAR[]>(new WCHAR[methodNameLength]);
	hr = metadataImport.GetMethodProps(methodDef, nullptr, nameBuffer.get(), methodNameLength, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not get method name for mdMethodDef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x.", methodDef, _moduleId, hr);
		return E_FAIL;
	}
		
	*flags = static_cast<CorMethodAttr>(methodFlags);
	name = LibProfiler::ToString(nameBuffer.get());
	return S_OK;
}

HRESULT LibProfiler::ModuleDef::GetTypeProps(
	IN mdTypeDef typeDef,
	OUT std::string& name)
{
	auto& metadataImport = GetMetadataImport();
	ULONG typeNameLength;
	auto hr = metadataImport.GetTypeDefProps(typeDef, nullptr, 0, &typeNameLength, nullptr, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not get type props for mdTypeDef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x.", typeDef, _moduleId, hr);
		return E_FAIL;
	}

	auto nameBuffer = std::unique_ptr<WCHAR[]>(new WCHAR[typeNameLength]);
	hr = metadataImport.GetTypeDefProps(typeDef, nameBuffer.get(), typeNameLength, nullptr, nullptr, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not get type name for mdTypeDef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x.", typeDef, _moduleId, hr);
		return E_FAIL;
	}

	name = LibProfiler::ToString(nameBuffer.get());
	return S_OK;
}

HRESULT LibProfiler::ModuleDef::GetTypeRefProps(
	IN mdTypeRef typeRef,
	OUT mdToken* resolutionScope)
{
	auto& metadataImport = GetMetadataImport();
	auto hr = metadataImport.GetTypeRefProps(typeRef, resolutionScope, nullptr, 0, nullptr);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not get type ref props for mdTypeRef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x", typeRef, _moduleId, hr);
		return E_FAIL;
	}

	return S_OK;
}

HRESULT LibProfiler::ModuleDef::GetPlaceHolderMethodRVA(OUT UINT* rva)
{
	// Note: for some reason CLR does not like receiving 0 as RVA for a method
	// We will provide the implementation later, until then place there System.Object::ctor() to make it happy

	const std::string systemObjectMetadataName = "System.Object";
	const std::string systemObjectCtorMethodName = ".ctor";

	HRESULT hr;

	if (_objectTypeDef == mdTypeDefNil)
	{
		hr = FindTypeDef(systemObjectMetadataName, &_objectTypeDef);
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not find type=%s. Error: 0x%x.", systemObjectMetadataName.c_str(), hr);
			return E_FAIL;
		}
	}

	if (_objectCtorDef == mdMethodDefNil)
	{
		COR_SIGNATURE signature[] = {
			// Calling convention
			CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS,
			// Parameters count
			0,
			// Return type
			CorElementType::ELEMENT_TYPE_VOID
		};

		ULONG signatureLength = sizeof(signature) / sizeof(*signature);
		hr = FindMethodDef(systemObjectCtorMethodName, signature, signatureLength, _objectTypeDef, &_objectCtorDef);
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not find method=%s on type=%s. Error: 0x%x.", systemObjectCtorMethodName.c_str(), systemObjectMetadataName.c_str(), hr);
			return E_FAIL;
		}
	}

	if (_objectCtorRva == 0)
	{
		auto& metadataImport = GetMetadataImport();

		ULONG rva;
		hr = metadataImport.GetMethodProps(_objectCtorDef, nullptr, nullptr, 0, nullptr, nullptr, nullptr, nullptr, &rva, nullptr);
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not obtain RVA for method=%s on type=%s. Error: 0x%x.", systemObjectCtorMethodName.c_str(), systemObjectMetadataName.c_str(), hr);
			return E_FAIL;
		}
	}

	return S_OK;
}
