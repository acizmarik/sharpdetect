// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "../lib/loguru/loguru.hpp"

#include "MetadataAnalysis.h"
#include "PAL.h"

HRESULT LibProfiler::IsValueType(
	IN const ModuleDef& moduleDef,
	IN const mdTypeDef typeDef,
	OUT BOOL* isValueType)
{
	constexpr auto systemValueTypeName = "System.ValueType";
	std::string typeName;
	mdToken baseTypeToken;

	HRESULT hr = moduleDef.GetTypeProps(typeDef, &baseTypeToken, typeName);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not get type props for mdTypeDef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x.",
			typeDef,
			moduleDef.GetModuleId(),
			GetLastError());
		return E_FAIL;
	}

	if (TypeFromToken(baseTypeToken) == mdtTypeDef)
	{
		std::string baseTypeName;
		hr = moduleDef.GetTypeProps(baseTypeToken, nullptr, baseTypeName);
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not get type props for base type mdTypeDef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x.",
				baseTypeToken,
				moduleDef.GetModuleId(),
				GetLastError());
			return E_FAIL;
		}

		*isValueType = baseTypeName == systemValueTypeName;
	}
	else if (TypeFromToken(baseTypeToken) == mdtTypeRef)
	{
		std::string baseTypeName;
		hr = moduleDef.GetTypeRefProps(baseTypeToken, nullptr, baseTypeName);
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not get type ref props for base type mdTypeRef=%d from module=%" UINT_PTR_FORMAT ". Error: 0x%x.", baseTypeToken, moduleDef.GetModuleId(), GetLastError());
			return E_FAIL;
		}

		*isValueType = baseTypeName == systemValueTypeName;
	}
	else if (TypeFromToken(baseTypeToken) == mdtTypeSpec)
	{
		*isValueType = false;
	}

	return S_OK;
}

HRESULT LibProfiler::IsVolatile(
	IN const ILInstr& instruction,
	OUT BOOL* isVolatile)
{
	*isVolatile = instruction.m_pPrev != nullptr && instruction.m_pPrev->m_opcode == CEE_VOLATILE;
	return S_OK;
}

HRESULT LibProfiler::AnalyzeFieldAccess(
	IN const ModuleDef &moduleDef,
	IN const mdToken fieldToken,
	IN const ILInstr& instruction,
	OUT PCCOR_SIGNATURE *signature,
	OUT ULONG *signatureLength,
	OUT BOOL *isDeclaringTypeValueType,
	OUT BOOL *isVolatile)
{
	// Obtain field signature and declaring type
	HRESULT hr;
	mdToken declaringTypeToken;
	if (TypeFromToken(fieldToken) == mdtFieldDef)
	{
		hr = moduleDef.GetFieldProps(fieldToken, &declaringTypeToken, signature, signatureLength);
	}
	else if (TypeFromToken(fieldToken) == mdtMemberRef)
	{
		hr = moduleDef.GetFieldRefProps(fieldToken, &declaringTypeToken, signature, signatureLength);
	}
	else
	{
		LOG_F(ERROR, "Unsupported token type for field token %d in module %s.", fieldToken, moduleDef.GetName().c_str());
		return E_FAIL;
	}
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain field signature and declaring type for token %d in module %s. Error: 0x%x.", fieldToken, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// Analyze if declaring type is value type
	hr = IsValueType(moduleDef, declaringTypeToken, isDeclaringTypeValueType);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not analyze if declaring type is value type for field token %d in module %s. Error: 0x%x.", fieldToken, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// Analyze if field is volatile
	hr = IsVolatile(instruction, isVolatile);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not analyze if field access is volatile for field token %d in module %s. Error: 0x%x.", fieldToken, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	return S_OK;
}