// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstring>
#include <string>
#include <vector>

#include "../lib/loguru/loguru.hpp"

#include "Instrumentation.h"

HRESULT LibProfiler::PatchMethodBody(
	IN ICorProfilerInfo& corProfilerInfo, 
	IN const ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const std::unordered_map<mdToken, mdToken>& tokensToPatch)
{
	if (tokensToPatch.empty())
		return E_FAIL;

	// Obtain current method
	ILRewriter rewriter(&corProfilerInfo, nullptr, moduleDef.GetModuleId(), mdMethodDef);
	HRESULT hr = rewriter.Import();
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not import method body for %d from module %s. Error: 0x%x.", mdMethodDef, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// Check if any instructions need to be patched
	BOOL isRewritten = false;
	for (auto currentInstruction = rewriter.GetILList()->m_pNext;
		currentInstruction != rewriter.GetILList();
		currentInstruction = currentInstruction->m_pNext)
	{
		if (currentInstruction->m_opcode != CEE_CALL && currentInstruction->m_opcode != CEE_CALLVIRT)
			continue;

		auto const originalMethodToken = static_cast<mdToken>(currentInstruction->m_Arg32);
		if (tokensToPatch.contains(originalMethodToken))
		{
			auto const newTokenValue = tokensToPatch.find(originalMethodToken)->second;
			currentInstruction->m_Arg32 = static_cast<INT32>(newTokenValue);
			isRewritten = true;
		}
	}

	// Apply changes if needed
	if (isRewritten)
	{
		hr = rewriter.Export();
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not export rewritten method body for %d from module %s. Error: 0x%x.", mdMethodDef, moduleDef.GetName().c_str(), hr);
			return E_FAIL;
		}

		return S_OK;
	}

	return E_FAIL;
}

HRESULT LibProfiler::CreateManagedWrapperMethod(
	IN ICorProfilerInfo& corProfilerInfo, 
	IN ModuleDef& moduleDef, 
	IN const mdTypeDef mdTypeDef,
	IN const mdMethodDef mdWrappedMethodDef,
	OUT mdMethodDef& mdWrapperMethodDef, 
	OUT std::string& wrapperMethodName)
{
	std::string wrappedMethodName;
	CorMethodAttr methodFlags;
	PCCOR_SIGNATURE methodSignature;
	ULONG methodSignatureLength;
	auto hr = moduleDef.GetMethodProps(
		mdWrappedMethodDef, 
		nullptr, 
		wrappedMethodName, 
		&methodFlags, 
		&methodSignature,
		&methodSignatureLength);

	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not retrieve method props for %d from module %s. Error: 0x%x.", mdWrappedMethodDef, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// First byte in signature is enum for calling convention
	auto const isStatic = (methodSignature[0] & CorCallingConvention::IMAGE_CEE_CS_CALLCONV_HASTHIS) == 0;

	// Second byte in signature determines number of arguments
	auto parametersCount = static_cast<BYTE>(methodSignature[1]);
	if (!isStatic)
	{
		// Instance method has hidden this parameter
		parametersCount += 1;
	}

	methodFlags = static_cast<CorMethodAttr>(
		methodFlags | 
		CorMethodAttr::mdSpecialName | 
		CorMethodAttr::mdRTSpecialName);

	constexpr auto methodImplFlags = static_cast<CorMethodImpl>(
		CorMethodImpl::miManaged | 
		CorMethodImpl::miNoInlining | 
		CorMethodImpl::miNoOptimization);

	// Wrapper name will be prefixed with a dot
	// FIXME: use some pattern that is not used by regular tools
	wrapperMethodName = "." + wrappedMethodName;

	// Tiny method header is indicated by mask 0x02
	// Upper 6 bits will be used to indicate code size
	auto header = static_cast<BYTE>(0b00000010);
	std::vector<BYTE> code;

	// Generate instructions
	for (BYTE index = 0; index < parametersCount; index++)
	{
		if (index <= 3)
		{
			// Ldarg.i for every parameter
			code.push_back(CEE_LDARG_0 + index);
		}
		else
		{
			// Ldarg.s
			code.push_back(static_cast<BYTE>(CEE_LDARG_S));
			code.push_back(index);
		}
	}
	// Call <token>
	code.push_back(static_cast<BYTE>(CEE_CALL));
	code.push_back(static_cast<BYTE>(mdWrappedMethodDef));
	code.push_back(static_cast<BYTE>(mdWrappedMethodDef >> 8));
	code.push_back(static_cast<BYTE>(mdWrappedMethodDef >> 16));
	code.push_back(static_cast<BYTE>(mdWrappedMethodDef >> 24));
	// Ret
	code.push_back(static_cast<BYTE>(CEE_RET));

	if (code.size() > 63)
	{
		LOG_F(ERROR, "Support for FAT wrapper methods not implemented.");
		return E_FAIL;
	}

	// Fix header with real code size
	header |= static_cast<BYTE>(code.size() << 2);

	// Allocate method body
	void* rawNewMethodBody;
	hr = moduleDef.AllocMethodBody(code.size() + 1, &rawNewMethodBody);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not allocate memory for method wrapper %s in module %s. Error: 0x%x.", wrapperMethodName.c_str(), moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// Prepare method body
	const auto newMethodBody = static_cast<BYTE*>(rawNewMethodBody);
	*newMethodBody = header;
	std::memcpy(newMethodBody + 1, code.data(), code.size());

	// Create new method
	hr = moduleDef.AddMethodDef(
		wrapperMethodName,
		methodFlags,
		mdTypeDef,
		methodSignature,
		methodSignatureLength,
		methodImplFlags,
		&mdWrapperMethodDef);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not create wrapper method %s in module %s. Error: 0x%x.", wrapperMethodName.c_str(), moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// Set new IL body
	hr = corProfilerInfo.SetILFunctionBody(moduleDef.GetModuleId(), mdWrapperMethodDef, newMethodBody);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not set body for wrapper method %s in module %s. Error: 0x%x.", wrapperMethodName.c_str(), moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	return S_OK;
}