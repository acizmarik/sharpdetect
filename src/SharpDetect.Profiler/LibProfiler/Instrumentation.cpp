// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstring>
#include <string>
#include <vector>

#include "../lib/loguru/loguru.hpp"

#include "Instrumentation.h"
#include "Instruction.h"
#include "MethodBodyHelpers.h"

HRESULT LibProfiler::PatchMethodBody(
	IN ICorProfilerInfo& corProfilerInfo, 
	IN ModuleDef& moduleDef, 
	IN mdMethodDef mdMethodDef, 
	IN const std::unordered_map<mdToken, mdToken>& tokensToPatch)
{
	if (tokensToPatch.size() == 0)
		return E_FAIL;

	// Obtain current method
	LPCBYTE methodBody;
	ULONG methodBodyLength;
	auto hr = corProfilerInfo.GetILFunctionBody(
		moduleDef.GetModuleId(), 
		mdMethodDef, 
		&methodBody, 
		&methodBodyLength);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not retrieve method body for %d from module %s. Error: 0x%x.", mdMethodDef, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// Check if any instructions need to be patched
	INT ip = 0;
	std::vector<Instruction> instructionsToPatch;
	UINT headerSize;
	UINT codeSize;
	std::tie(headerSize, codeSize) = ReadHeaderInfo(methodBody, ip);
	while (ip < codeSize + headerSize)
	{
		auto instruction = ReadInstruction(methodBody, ip);
		auto& opCode = instruction.GetOpCode();

		if (opCode.GetCode() != Code::Call && opCode.GetCode() != Code::Callvirt)
			continue;

		auto originalMethodToken = mdToken(instruction.GetOperand().value().Arg32);
		if (tokensToPatch.find(originalMethodToken) == tokensToPatch.cend())
			continue;

		instructionsToPatch.push_back(std::move(instruction));
	}

	// Patch body if needed
	if (instructionsToPatch.size() > 0)
	{
		// Allocate new method
		void* newMethodBody = nullptr;
		hr = moduleDef.AllocMethodBody(methodBodyLength, &newMethodBody);
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not allocate patched method for %d from module %s. Error: 0x%x.", mdMethodDef, moduleDef.GetName().c_str(), hr);
			return E_FAIL;
		}

		// Copy original method
		std::memcpy(newMethodBody, methodBody, methodBodyLength);

		// Patch found instructions
		for (auto&& instruction : instructionsToPatch)
		{
			auto const originalToken = mdToken(instruction.GetOperand().value().Arg32);
			auto const newTokenValue = (*tokensToPatch.find(originalToken)).second;

			// Rewrite operand
			auto operandOffset = static_cast<BYTE*>(newMethodBody) + instruction.GetOffset() + 1;
			std::memcpy(operandOffset, &newTokenValue, sizeof(INT));
		}

		// Apply new method
		hr = corProfilerInfo.SetILFunctionBody(
			moduleDef.GetModuleId(), 
			mdMethodDef, 
			static_cast<LPCBYTE>(newMethodBody));
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not set method body for %d from module %s. Error: 0x%x.", mdMethodDef, moduleDef.GetName().c_str(), hr);
			return E_FAIL;
		}

		return S_OK;
	}

	return E_FAIL;
}

HRESULT LibProfiler::CreateManagedWrapperMethod(
	IN ICorProfilerInfo& corProfilerInfo, 
	IN ModuleDef& moduleDef, 
	IN mdTypeDef mdTypeDef, 
	IN mdMethodDef mdWrappedMethodDef, 
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
	// Second byte in signature determines number of arguments
	auto const parametersCount = static_cast<BYTE>(methodSignature[1]);

	methodFlags = static_cast<CorMethodAttr>(
		methodFlags | 
		CorMethodAttr::mdSpecialName | 
		CorMethodAttr::mdRTSpecialName);

	auto const methodImplFlags = static_cast<CorMethodImpl>(
		CorMethodImpl::miIL | 
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
			code.push_back(static_cast<BYTE>(Code::Ldarg_0) + index);
		}
		else
		{
			// Ldarg.s
			code.push_back(static_cast<BYTE>(Code::Ldarg_S));
			code.push_back(index);
		}
	}
	// Call <token>
	code.push_back(static_cast<BYTE>(Code::Call));
	code.push_back((BYTE)(mdWrappedMethodDef));
	code.push_back((BYTE)(mdWrappedMethodDef >> 8));
	code.push_back((BYTE)(mdWrappedMethodDef >> 16));
	code.push_back((BYTE)(mdWrappedMethodDef >> 24));
	// Ret
	code.push_back(static_cast<BYTE>(Code::Ret));

	if (code.size() > 63)
	{
		LOG_F(ERROR, "Support for FAT wrapper methods not implemented.");
		return E_FAIL;
	}

	// Fix header with real code size
	header |= (BYTE)(code.size() << 2);

	// Allocate method body
	void* rawNewMethodBody;
	hr = moduleDef.AllocMethodBody(code.size() + 1, &rawNewMethodBody);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not allocate memory for method wrapper %s in module %s. Error: 0x%x.", wrapperMethodName.c_str(), moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	// Prepare method body
	auto newMethodBody = static_cast<BYTE*>(rawNewMethodBody);
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