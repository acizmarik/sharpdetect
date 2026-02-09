// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstring>
#include <ranges>
#include <string>
#include <vector>

#include "../lib/loguru/loguru.hpp"

#include "Instrumentation.h"
#include "MetadataAnalysis.h"

static bool ShouldSkipInstrumentation(
	IN const LibProfiler::ModuleDef& moduleDef,
	IN const std::vector<std::string>& excludedFieldAccessModulePrefixes)
{
	for (auto&& excludedNamespacePrefix : excludedFieldAccessModulePrefixes)
	{
		if (moduleDef.GetName().starts_with(excludedNamespacePrefix))
			return true;
	}

	return false;
}

HRESULT LibProfiler::PatchMethodBody(
	IN ICorProfilerInfo& corProfilerInfo,
	IN LibIPC::Client& client,
	IN const ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const std::unordered_map<mdToken, mdToken>& tokensToPatch,
	IN const std::unordered_map<LibIPC::RecordedEventType, mdToken>& injectedMethods,
	IN const BOOL enableFieldsAccessInstrumentation,
	IN const std::vector<std::string>& excludedFieldAccessModulePrefixes)
{
	if (tokensToPatch.empty() && (!enableFieldsAccessInstrumentation || injectedMethods.empty()))
		return E_FAIL;

	static std::atomic<UINT64> instrumentationMark {0};

	BOOL importedLocals = FALSE;
	PCCOR_SIGNATURE localSignature = nullptr;
	ULONG localSignatureVarsCount = 0;
	ULONG localSignatureByteLength = 0;
	auto addedLocals = std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>{};

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
		if (currentInstruction->m_opcode == CEE_CALL || currentInstruction->m_opcode == CEE_CALLVIRT)
		{
			auto const originalMethodToken = static_cast<mdToken>(currentInstruction->m_Arg32);
			if (tokensToPatch.contains(originalMethodToken))
			{
				auto const newTokenValue = tokensToPatch.find(originalMethodToken)->second;
				currentInstruction->m_Arg32 = static_cast<INT32>(newTokenValue);
				isRewritten = true;
			}
		}

		if (enableFieldsAccessInstrumentation)
		{
			if (ShouldSkipInstrumentation(moduleDef, excludedFieldAccessModulePrefixes))
				continue;

			// Static field access
			if (currentInstruction->m_opcode == CEE_LDSFLD || currentInstruction->m_opcode == CEE_STSFLD)
			{
				auto const mark = instrumentationMark.fetch_add(1);
				ILInstr* nextInstruction = nullptr;

				if (!FAILED(InstrumentStaticFieldAccess(
					corProfilerInfo,
					client,
					rewriter,
					currentInstruction,
					mark,
					moduleDef,
					mdMethodDef,
					injectedMethods,
					&nextInstruction)))
				{
					isRewritten = true;
					currentInstruction = nextInstruction;
				}
			}
			// Instance field access
			if (currentInstruction->m_opcode == CEE_LDFLD || currentInstruction->m_opcode == CEE_STFLD)
			{
				// Import local variable signature if not done yet
				if (!importedLocals)
				{
					auto const signatureToken = rewriter.GetLocalVarSigToken();
					if (signatureToken != 0)
					{
						hr = moduleDef.GetSignatureFromToken(rewriter.GetLocalVarSigToken(), &localSignature, &localSignatureByteLength);
						if (FAILED(hr))
						{
							LOG_F(ERROR, "Could not obtain local variable signature %d for method %d from module %s. Error: 0x%x", rewriter.GetLocalVarSigToken(), mdMethodDef, moduleDef.GetName().c_str(), hr);
							return E_FAIL;
						}
						CorSigUncompressData(localSignature + 1, &localSignatureVarsCount);
					}
					importedLocals = TRUE;
				}

				auto const mark = instrumentationMark.fetch_add(1);
				ILInstr* nextInstruction = nullptr;

				if (!FAILED(InstrumentInstanceFieldAccess(
					corProfilerInfo,
					client,
					rewriter,
					currentInstruction,
					localSignatureVarsCount,
					addedLocals,
					mark,
					moduleDef,
					mdMethodDef,
					injectedMethods,
					&nextInstruction)))
				{
					isRewritten = true;
					currentInstruction = nextInstruction;
				}
			}
		}
	}

	// Apply changes if needed
	if (isRewritten)
	{
		if (!addedLocals.empty())
		{
			hr = AddLocalVariables(moduleDef, rewriter, localSignature, localSignatureByteLength, addedLocals);
			if (FAILED(hr))
			{
				LOG_F(ERROR, "Could not add local variables to method %d from module %s. Error: 0x%x.", mdMethodDef, moduleDef.GetName().c_str(), hr);
				return E_FAIL;
			}
		}

		hr = rewriter.Export();
		if (FAILED(hr))
		{
			LOG_F(ERROR, "Could not export rewritten method body for %d from module %s. Error: 0x%x.", mdMethodDef, moduleDef.GetName().c_str(), hr);
			return E_FAIL;
		}

		mdTypeDef mdTypeDef;
		std::string methodName;
		std::string typeName;
		moduleDef.GetMethodProps(mdMethodDef, &mdTypeDef, methodName, nullptr, nullptr, nullptr);
		moduleDef.GetTypeProps(mdTypeDef, nullptr, typeName);
		LOG_F(INFO, "Patched method %s.%s in module %s.", typeName.c_str(), methodName.c_str(), moduleDef.GetName().c_str());
		return S_OK;
	}

	return E_FAIL;
}

HRESULT LibProfiler::InstrumentStaticFieldAccess(
	IN ICorProfilerInfo& corProfilerInfo,
	IN LibIPC::Client& client,
	IN ILRewriter& rewriter,
	IN ILInstr* currentInstruction,
	IN const UINT64 instrumentationMark,
	IN const ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const std::unordered_map<LibIPC::RecordedEventType, mdToken>& injectedMethods,
	OUT ILInstr** nextInstruction)
{
	auto const isStore = currentInstruction->m_opcode == CEE_STSFLD;
	auto const moduleId = moduleDef.GetModuleId();
	auto const fieldToken = static_cast<mdToken>(currentInstruction->m_Arg32);
	ThreadID threadId;
	corProfilerInfo.GetCurrentThreadID(&threadId);

	// Instrument field access
	const auto eventType = isStore
		? LibIPC::RecordedEventType::StaticFieldWrite
		: LibIPC::RecordedEventType::StaticFieldRead;
	auto const methodIt = injectedMethods.find(eventType);
	if (methodIt == injectedMethods.end())
	{
		LOG_F(ERROR, "Could not find injected method for event type %d.", static_cast<int>(eventType));
		return E_FAIL;
	}

	// LDC.I8 <instrumentation-mark>
	const auto ldcInstruction = rewriter.NewILInstr();
	ldcInstruction->m_opcode = CEE_LDC_I8;
	ldcInstruction->m_Arg64 = static_cast<INT64>(instrumentationMark);
	rewriter.InsertAfter(currentInstruction, ldcInstruction);
	// CALL <injected-method-handler>
	const auto callInstruction = rewriter.NewILInstr();
	callInstruction->m_opcode = CEE_CALL;
	callInstruction->m_Arg32 = static_cast<INT32>(methodIt->second);
	rewriter.InsertAfter(ldcInstruction, callInstruction);
	// Skip to next non-injected instruction
	currentInstruction = callInstruction;

	LOG_F(INFO, "Instrumented static field %s access in method %d with stub %d from module %s.",
		isStore ? "write" : "read",
		mdMethodDef,
		methodIt->second,
		moduleDef.GetName().c_str());

	// Notify analysis of field access instrumentation point
	client.Send(LibIPC::Helpers::CreateFieldAccessInstrumentationMsg(
		LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId),
		moduleId,
		mdMethodDef,
		currentInstruction->m_offset,
		fieldToken,
		instrumentationMark));

	*nextInstruction = currentInstruction;
	return S_OK;
}

HRESULT LibProfiler::InstrumentInstanceFieldAccess(
	IN ICorProfilerInfo& corProfilerInfo,
	IN LibIPC::Client& client,
	IN ILRewriter& rewriter,
	IN ILInstr* currentInstruction,
	IN const UINT16 importedLocalsCount,
	IN std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>& addedLocals,
	IN const UINT64 instrumentationMark,
	IN const ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const std::unordered_map<LibIPC::RecordedEventType, mdToken>& injectedMethods,
	OUT ILInstr** nextInstruction)
{
	auto const isStore = currentInstruction->m_opcode == CEE_STFLD;
	auto const moduleId = moduleDef.GetModuleId();
	auto const fieldToken = static_cast<mdToken>(currentInstruction->m_Arg32);
	ThreadID threadId;
	corProfilerInfo.GetCurrentThreadID(&threadId);

	// Analyze field
	PCCOR_SIGNATURE fieldSignature;
	ULONG fieldSignatureLength;
	BOOL isDeclaringTypeValueType;
	BOOL isVolatile;
	HRESULT hr = AnalyzeFieldAccess(moduleDef, fieldToken, *currentInstruction, &fieldSignature, &fieldSignatureLength, &isDeclaringTypeValueType, &isVolatile);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not analyze field for token %d in module %s. Error: 0x%x.", fieldToken, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	if (isDeclaringTypeValueType)
	{
		// Note: value types can be either passed by:
		// 1) reference - in this case we have no way to capture managed pointers
		// 2) value - in this case all threads operate on a copy. Data races are not possible.
		return E_FAIL;
	}

	// Prepare method token for instance field access
	const auto fieldAccessEventType = isStore
		? LibIPC::RecordedEventType::InstanceFieldWrite
		: LibIPC::RecordedEventType::InstanceFieldRead;
	auto const fieldAccessMethodIt = injectedMethods.find(fieldAccessEventType);
	if (fieldAccessMethodIt == injectedMethods.end())
	{
		LOG_F(ERROR, "Could not find injected method for event type %d.", static_cast<int>(fieldAccessEventType));
		return E_FAIL;
	}

	if (isVolatile)
		currentInstruction = currentInstruction->m_pPrev;

	// STLOC <local-index> (this stores written value)
	if (isStore)
	{
		addedLocals.emplace_back(fieldSignature + 1, fieldSignatureLength - 1);
		const auto stlocInstruction = rewriter.NewILInstr();
		stlocInstruction->m_opcode = CEE_STLOC;
		stlocInstruction->m_Arg16 = static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 1));
		rewriter.InsertBefore(currentInstruction, stlocInstruction);
	}
	// STLOC <local-index> (this stores field instance)
	PCCOR_SIGNATURE instanceSignature = g_ObjectTypeSignature;
	addedLocals.emplace_back(instanceSignature, 1);
	const auto stlocInstruction = rewriter.NewILInstr();
	stlocInstruction->m_opcode = CEE_STLOC;
	stlocInstruction->m_Arg16 = static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 1));
	rewriter.InsertBefore(currentInstruction, stlocInstruction);
	// LDLOC <local-index> (this loads field instance)
	const auto ldlocInstanceInstruction = rewriter.NewILInstr();
	ldlocInstanceInstruction->m_opcode = CEE_LDLOC;
	ldlocInstanceInstruction->m_Arg16 = static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 1));
	rewriter.InsertBefore(currentInstruction, ldlocInstanceInstruction);
	// LDLOC <local-index> (this loads written value)
	if (isStore)
	{
		const auto ldlocValueInstruction = rewriter.NewILInstr();
		ldlocValueInstruction->m_opcode = CEE_LDLOC;
		ldlocValueInstruction->m_Arg16 = static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 2));
		rewriter.InsertBefore(currentInstruction, ldlocValueInstruction);
	}
	// <FIELD-ACCESS> (LDFLD/STRFLD)
	if (isVolatile)
		currentInstruction = currentInstruction->m_pNext;
	// LDC.I8 <instrumentation-mark>
	const auto ldcInstruction = rewriter.NewILInstr();
	ldcInstruction->m_opcode = CEE_LDC_I8;
	ldcInstruction->m_Arg64 = static_cast<INT64>(instrumentationMark);
	rewriter.InsertAfter(currentInstruction, ldcInstruction);
	const auto ldLocInstanceInstruction2 = rewriter.NewILInstr();
	ldLocInstanceInstruction2->m_opcode = CEE_LDLOC;
	ldLocInstanceInstruction2->m_Arg16 = static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 1));
	rewriter.InsertAfter(ldcInstruction, ldLocInstanceInstruction2);
	// CALL <injected-method-handler>
	const auto callInstruction = rewriter.NewILInstr();
	callInstruction->m_opcode = CEE_CALL;
	callInstruction->m_Arg32 = static_cast<INT32>(fieldAccessMethodIt->second);
	rewriter.InsertAfter(ldLocInstanceInstruction2, callInstruction);
	// Skip to next non-injected instruction
	currentInstruction = callInstruction;

	LOG_F(INFO, "Instrumented instance field %s access in method %d with stub %d from module %s.",
		isStore ? "write" : "read",
		mdMethodDef,
		fieldAccessMethodIt->second,
		moduleDef.GetName().c_str());

	// Notify analysis of field access instrumentation point
	client.Send(LibIPC::Helpers::CreateFieldAccessInstrumentationMsg(
		LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId),
		moduleId,
		mdMethodDef,
		currentInstruction->m_offset,
		fieldToken,
		instrumentationMark));

	*nextInstruction = currentInstruction;
	return S_OK;
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

HRESULT LibProfiler::AddLocalVariables(
	IN const ModuleDef& moduleDef,
	IN ILRewriter& rewriter,
	IN PCCOR_SIGNATURE oldSignature,
	IN const ULONG oldSignatureLength,
	IN const std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>& addedLocals)
{
	ULONG oldLocalsCount = 0;
	ULONG oldLocalsLength = 0;
	PCCOR_SIGNATURE oldLocals = nullptr;
	BYTE callingConvention = IMAGE_CEE_CS_CALLCONV_LOCAL_SIG;

	// Fetch information about old signature (if exists)
	if (oldSignature != nullptr && oldSignatureLength > 0)
	{
		const PCCOR_SIGNATURE originalSignature = oldSignature;
		callingConvention = oldSignature[0];
		oldSignature = oldSignature + 1;
		oldLocals = oldSignature + CorSigUncompressData(oldSignature, &oldLocalsCount);
		oldLocalsLength = oldSignatureLength - static_cast<ULONG>(oldLocals - originalSignature);
	}

	// Calculate new signature length
	const ULONG newLocalCount = oldLocalsCount + static_cast<ULONG>(addedLocals.size());
	ULONG newSignatureLength = 1;
	BYTE temporaryWriteBuffer[4]; // Max size of compressed data is 4 bytes
	newSignatureLength += CorSigCompressData(newLocalCount, temporaryWriteBuffer);
	newSignatureLength += oldLocalsLength;
	for (const auto &length: addedLocals | std::views::values)
		newSignatureLength += length;

	// Create new signature
	std::vector<BYTE> newSignature(newSignatureLength);
	auto newSignatureData = newSignature.data();
	*newSignatureData++ = callingConvention;
	newSignatureData += CorSigCompressData(newLocalCount, newSignatureData);
	if (oldLocals != nullptr && oldLocalsLength > 0)
	{
		std::memcpy(newSignatureData, oldLocals, oldLocalsLength);
		newSignatureData += oldLocalsLength;
	}
	for (auto&& [signature, length] : addedLocals)
	{
		std::memcpy(newSignatureData, signature, length);
		newSignatureData += length;
	}

	// Emit new signature
	mdSignature newSignatureToken;
	const HRESULT hr = moduleDef.GetTokenFromSignature(
		newSignature.data(),
		newSignature.size(),
		&newSignatureToken);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not emit new local variable signature for method %d in module %s. Error: 0x%x.",
			rewriter.GetMethodSigToken(),
			moduleDef.GetName().c_str(),
			hr);
		return E_FAIL;
	}

	// Set new signature in rewriter
	rewriter.SetLocalVarSigToken(newSignatureToken);

	return S_OK;
}