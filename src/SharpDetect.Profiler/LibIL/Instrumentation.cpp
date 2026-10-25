// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <cstring>
#include <deque>
#include <ranges>
#include <string>
#include <vector>

#include "../lib/loguru/loguru.hpp"

#include "Instrumentation.h"
#include "MetadataAnalysis.h"
#include "SignatureUtils.h"

bool LibProfiler::ShouldSkipInstrumentation(
	IN const ModuleDef& moduleDef,
	IN const std::vector<std::string>& skipInstrumentationForAssemblies)
{
	for (auto&& skippedNamespacePrefix : skipInstrumentationForAssemblies)
	{
		if (moduleDef.GetName().starts_with(skippedNamespacePrefix))
			return true;
	}

	return false;
}

static bool ShouldSkipFieldAccessInstrumentation(
	IN const LibProfiler::ModuleDef& moduleDef,
	IN const ILInstr& instruction,
	IN const mdToken fieldToken)
{
	BOOL isVolatile;
	if (FAILED(LibProfiler::IsVolatile(instruction, &isVolatile)) || isVolatile)
		return false;

	BOOL isExcluded;
	if (FAILED(LibProfiler::IsFieldExcludedFromRaceAnalysis(moduleDef, fieldToken, &isExcluded)))
		return false;

	return isExcluded;
}

static ILInstr* MakeRoomBefore(
	IN ILRewriter& rewriter,
	IN OUT ILInstr*& access)
{
	const auto head = LibProfiler::GetFirstPrefix(*access);
	const auto movedHead = rewriter.NewILInstr();
	movedHead->m_opcode = head->m_opcode;
	movedHead->m_Arg64 = head->m_Arg64;
	rewriter.InsertAfter(head, movedHead);
	head->m_opcode = CEE_NOP;

	if (head == access)
		access = movedHead;

	return head;
}

static bool ShouldCaptureFieldStack(
	IN const LibProfiler::ModuleDef& moduleDef,
	IN mdToken fieldToken,
	IN BOOL enableStackTraceCollection,
	IN const std::vector<std::string>& stackTraceFieldPatterns)
{
	if (!enableStackTraceCollection)
		return false;

	if (stackTraceFieldPatterns.empty())
		return true;

	std::string fieldFullName;
	if (FAILED(moduleDef.GetFieldFullName(fieldToken, fieldFullName)))
		return false;

	for (auto&& pattern : stackTraceFieldPatterns)
	{
		if (fieldFullName.starts_with(pattern))
			return true;
	}

	return false;
}

static HRESULT ImportLocalVariableSignature(
	IN const LibProfiler::ModuleDef& moduleDef,
	IN const ILRewriter& rewriter,
	IN const mdMethodDef mdMethodDef,
	IN OUT BOOL& importedLocals,
	IN OUT PCCOR_SIGNATURE& localSignature,
	IN OUT ULONG& localSignatureVarsCount,
	IN OUT ULONG& localSignatureByteLength)
{
	if (importedLocals)
		return S_OK;

	auto const signatureToken = rewriter.GetLocalVarSigToken();
	if (signatureToken != 0)
	{
		auto const hr = moduleDef.GetSignatureFromToken(signatureToken, &localSignature, &localSignatureByteLength);
		if (FAILED(hr))
		{
			LOG_F(
				ERROR,
				"Could not obtain local variable signature %d for method %d from module %s. Error: 0x%x",
				signatureToken,
				mdMethodDef,
				moduleDef.GetName().c_str(),
				hr);
			return E_FAIL;
		}
		CorSigUncompressData(localSignature + 1, &localSignatureVarsCount);
	}

	importedLocals = TRUE;
	return S_OK;
}

HRESULT LibProfiler::PatchMethodBody(
	IN ICorProfilerInfo& corProfilerInfo,
	IN LibIPC::Client& client,
	IN const ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const std::unordered_map<mdToken, mdToken>& tokensToPatch,
	IN const InjectedMethodsMap& injectedMethods,
	IN const FieldAccessIntrinsicsMap& fieldAccessIntrinsics,
	IN const BOOL enableFieldsAccessInstrumentation,
	IN const std::vector<std::string>& skipInstrumentationForAssemblies,
	IN const BOOL enableStackTraceCollection,
	IN const std::vector<std::string>& stackTraceFieldPatterns)
{
	if (tokensToPatch.empty() && (!enableFieldsAccessInstrumentation || injectedMethods.empty()))
		return E_FAIL;

	static std::atomic<UINT64> instrumentationMark {0};

	BOOL importedLocals = FALSE;
	PCCOR_SIGNATURE localSignature = nullptr;
	ULONG localSignatureVarsCount = 0;
	ULONG localSignatureByteLength = 0;
	auto addedLocals = std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>{};
	auto ownedSignatures = std::deque<std::vector<BYTE>>{};

	// Obtain current method
	ILRewriter rewriter(&corProfilerInfo, nullptr, moduleDef.GetModuleId(), mdMethodDef);
	HRESULT hr = rewriter.Import();
	if (FAILED(hr))
	{
        if (hr != CORPROF_E_FUNCTION_NOT_IL)
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
			auto const fieldToken = static_cast<mdToken>(currentInstruction->m_Arg32);
			if (ShouldSkipInstrumentation(moduleDef, skipInstrumentationForAssemblies))
				continue;

			if (currentInstruction->m_opcode == CEE_CALL && !fieldAccessIntrinsics.empty())
			{
				// All instantiations of a generic method share the parent that was resolved
				auto callToken = static_cast<mdToken>(currentInstruction->m_Arg32);
				if (TypeFromToken(callToken) == mdtMethodSpec)
				{
					mdToken parentToken = mdTokenNil;
					if (FAILED(moduleDef.GetMethodSpecParent(callToken, &parentToken)))
						parentToken = mdTokenNil;

					callToken = parentToken;
				}

				auto const intrinsicIt = fieldAccessIntrinsics.find(callToken);
				if (intrinsicIt != fieldAccessIntrinsics.cend())
				{
					const auto addressInstruction = currentInstruction->m_pArg0Producer;
					auto const addressOpcode = addressInstruction != nullptr ? addressInstruction->m_opcode : CEE_COUNT;
					if (addressOpcode == CEE_LDSFLDA || addressOpcode == CEE_LDFLDA)
					{
						if (FAILED(ImportLocalVariableSignature(
							moduleDef,
							rewriter,
							mdMethodDef,
							importedLocals,
							localSignature,
							localSignatureVarsCount,
							localSignatureByteLength)))
						{
							return E_FAIL;
						}

						auto const mark = instrumentationMark.fetch_add(IntrinsicMarksCount);
						ILInstr* lastInstruction = nullptr;
						if (SUCCEEDED(InstrumentFieldAccessIntrinsic(
							corProfilerInfo,
							client,
							rewriter,
							currentInstruction,
							addressInstruction,
							intrinsicIt->second,
							localSignatureVarsCount,
							addedLocals,
							ownedSignatures,
							mark,
							moduleDef,
							mdMethodDef,
							injectedMethods,
							enableStackTraceCollection,
							stackTraceFieldPatterns,
							&lastInstruction)))
						{
							// Continue after the injected code, so the re-emitted call is not instrumented again
							isRewritten = true;
							currentInstruction = lastInstruction;
							continue;
						}
					}
				}
			}

			// Static field access
			if (currentInstruction->m_opcode == CEE_LDSFLD || currentInstruction->m_opcode == CEE_STSFLD)
			{
				if (ShouldSkipFieldAccessInstrumentation(moduleDef, *currentInstruction, fieldToken))
					continue;

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
					enableStackTraceCollection,
					stackTraceFieldPatterns,
					&nextInstruction)))
					{
						isRewritten = true;
						currentInstruction = nextInstruction;
					}
			}
			// Instance field access
			if (currentInstruction->m_opcode == CEE_LDFLD || currentInstruction->m_opcode == CEE_STFLD)
			{
				if (ShouldSkipFieldAccessInstrumentation(moduleDef, *currentInstruction, fieldToken))
					continue;

				// Import local variable signature if not done yet
				if (FAILED(ImportLocalVariableSignature(
					moduleDef,
					rewriter,
					mdMethodDef,
					importedLocals,
					localSignature,
					localSignatureVarsCount,
					localSignatureByteLength)))
				{
					return E_FAIL;
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
					ownedSignatures,
					mark,
					moduleDef,
					mdMethodDef,
					injectedMethods,
					enableStackTraceCollection,
					stackTraceFieldPatterns,
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
	IN const InjectedMethodsMap& injectedMethods,
	IN const BOOL enableStackTraceCollection,
	IN const std::vector<std::string>& stackTraceFieldPatterns,
	OUT ILInstr** nextInstruction)
{
	auto const isStore = currentInstruction->m_opcode == CEE_STSFLD;
	auto const moduleId = moduleDef.GetModuleId();
	auto const fieldToken = static_cast<mdToken>(currentInstruction->m_Arg32);
	auto const originalOffset = currentInstruction->m_offset;
	ThreadID threadId;
	corProfilerInfo.GetCurrentThreadID(&threadId);

	BOOL isVolatile;
	HRESULT hr = IsVolatile(*currentInstruction, &isVolatile);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not analyze if field access is volatile for field token %d in module %s. Error: 0x%x.", fieldToken, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	const auto captureStack = ShouldCaptureFieldStack(moduleDef, fieldToken, enableStackTraceCollection, stackTraceFieldPatterns);
	const auto eventType = isStore
		? LibIPC::RecordedEventType::StaticFieldWrite
		: LibIPC::RecordedEventType::StaticFieldRead;
	auto const methodIt = injectedMethods.find(eventType);
	const auto helperToken = methodIt != injectedMethods.end()
		? (captureStack ? methodIt->second.withStackCapture : methodIt->second.plain)
		: mdTokenNil;
	if (helperToken == mdTokenNil)
	{
		LOG_F(ERROR, "Could not find injected %s method for event type %d.",
			captureStack ? "stack-capturing" : "plain",
			static_cast<int>(eventType));
		return E_FAIL;
	}

	if (isStore && !isVolatile)
	{
		// LDC.I8 <instrumentation-mark>
		const auto ldcInstruction = rewriter.NewILInstr();
		ldcInstruction->m_opcode = CEE_LDC_I8;
		ldcInstruction->m_Arg64 = static_cast<INT64>(instrumentationMark);
		rewriter.InsertAfter(currentInstruction, ldcInstruction);
		// CALL <injected-method-handler>
		const auto callInstruction = rewriter.NewILInstr();
		callInstruction->m_opcode = CEE_CALL;
		callInstruction->m_Arg32 = static_cast<INT32>(helperToken);
		rewriter.InsertAfter(ldcInstruction, callInstruction);
		// Skip to next non-injected instruction
		currentInstruction = callInstruction;
	}
	else if (isStore)
	{
		// A volatile store releases, so it is reported before the store publishes anything
		const auto headInstruction = MakeRoomBefore(rewriter, currentInstruction);
		// LDC.I8 <instrumentation-mark>
		const auto ldcInstruction = rewriter.NewILInstr();
		ldcInstruction->m_opcode = CEE_LDC_I8;
		ldcInstruction->m_Arg64 = static_cast<INT64>(instrumentationMark);
		rewriter.InsertAfter(headInstruction, ldcInstruction);
		// CALL <injected-method-handler>
		const auto callInstruction = rewriter.NewILInstr();
		callInstruction->m_opcode = CEE_CALL;
		callInstruction->m_Arg32 = static_cast<INT32>(helperToken);
		rewriter.InsertAfter(ldcInstruction, callInstruction);
	}
	else
	{
		// LDC.I8 <instrumentation-mark>
		const auto ldcInstruction = rewriter.NewILInstr();
		ldcInstruction->m_opcode = CEE_LDC_I8;
		ldcInstruction->m_Arg64 = static_cast<INT64>(instrumentationMark);
		rewriter.InsertAfter(currentInstruction, ldcInstruction);
		// CALL <injected-method-handler>
		const auto callInstruction = rewriter.NewILInstr();
		callInstruction->m_opcode = CEE_CALL;
		callInstruction->m_Arg32 = static_cast<INT32>(helperToken);
		rewriter.InsertAfter(ldcInstruction, callInstruction);
		// Skip to next non-injected instruction
		currentInstruction = callInstruction;
	}

	LOG_F(INFO, "Instrumented static field %s access in method %d with stub %d from module %s.",
		isStore ? "write" : "read",
		mdMethodDef,
		helperToken,
		moduleDef.GetName().c_str());

	// Notify analysis of field access instrumentation point
	client.Send(LibIPC::Helpers::CreateFieldAccessInstrumentationMsg(
		LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId),
		moduleId,
		mdMethodDef,
		originalOffset,
		fieldToken,
		instrumentationMark,
		isVolatile ? LibIPC::FieldAccessKind::Volatile : LibIPC::FieldAccessKind::Regular));

	*nextInstruction = currentInstruction;
	return S_OK;
}

static mdToken FindFieldAccessHelper(
	IN const LibProfiler::InjectedMethodsMap& injectedMethods,
	IN const LibIPC::RecordedEventType eventType,
	IN const BOOL captureStack)
{
	auto const methodIt = injectedMethods.find(eventType);
	if (methodIt == injectedMethods.end())
		return mdTokenNil;

	return captureStack ? methodIt->second.withStackCapture : methodIt->second.plain;
}

static HRESULT ResolveComparandSignature(
	IN const LibProfiler::ModuleDef& moduleDef,
	IN const mdToken callToken,
	OUT std::vector<BYTE>& comparandSignature)
{
	mdToken memberRef = callToken;
	LibProfiler::TypeArgs methodArgs;
	if (TypeFromToken(callToken) == mdtMethodSpec)
	{
		PCCOR_SIGNATURE instantiation;
		ULONG instantiationLength;
		if (FAILED(moduleDef.GetMethodSpecProps(callToken, &memberRef, &instantiation, &instantiationLength)) ||
			!LibProfiler::ParseMethodSpecGenericArgs(instantiation, instantiationLength, methodArgs))
		{
			return E_FAIL;
		}
	}

	if (TypeFromToken(memberRef) != mdtMemberRef)
		return E_FAIL;

	PCCOR_SIGNATURE signature;
	ULONG signatureLength;
	if (FAILED(moduleDef.GetMemberRefSignature(memberRef, &signature, &signatureLength)))
		return E_FAIL;

	// CompareExchange(ref location, value, comparand)
	constexpr unsigned comparandParameterIndex = 2;
	return LibProfiler::GetMethodParameterSigType(signature, signatureLength, comparandParameterIndex, methodArgs, comparandSignature)
		? S_OK
		: E_FAIL;
}

enum class ComparandComparison
{
	// The outcome cannot be computed for every instantiation, so the exchange is modeled as a read-modify-write
	Unsupported,
	// The returned value is compared with the comparand using ceq
	Value,
	// CompareExchange compares floating-point values bitwise (NaN can match, -0.0 does not match 0.0), unlike ceq
	Bits32,
	Bits64
};

static ComparandComparison ClassifyComparand(
	IN const LibProfiler::ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const std::vector<BYTE>& comparandSignature)
{
	if (comparandSignature.empty())
		return ComparandComparison::Unsupported;

	switch (comparandSignature[0])
	{
		case ELEMENT_TYPE_BOOLEAN:
		case ELEMENT_TYPE_CHAR:
		case ELEMENT_TYPE_I1:
		case ELEMENT_TYPE_U1:
		case ELEMENT_TYPE_I2:
		case ELEMENT_TYPE_U2:
		case ELEMENT_TYPE_I4:
		case ELEMENT_TYPE_U4:
		case ELEMENT_TYPE_I8:
		case ELEMENT_TYPE_U8:
		case ELEMENT_TYPE_I:
		case ELEMENT_TYPE_U:
		case ELEMENT_TYPE_STRING:
		case ELEMENT_TYPE_OBJECT:
		case ELEMENT_TYPE_CLASS:
		case ELEMENT_TYPE_SZARRAY:
		case ELEMENT_TYPE_ARRAY:
			return ComparandComparison::Value;

		case ELEMENT_TYPE_R4:
			return ComparandComparison::Bits32;

		case ELEMENT_TYPE_R8:
			return ComparandComparison::Bits64;

		case ELEMENT_TYPE_GENERICINST:
			return comparandSignature.size() > 1 && comparandSignature[1] == ELEMENT_TYPE_CLASS
				? ComparandComparison::Value
				: ComparandComparison::Unsupported;

		case ELEMENT_TYPE_VAR:
		case ELEMENT_TYPE_MVAR:
		{
			// The generic parameter belongs to the instrumented method or its declaring type
			ULONG index;
			CorSigUncompressData(comparandSignature.data() + 1, &index);
			mdToken owner = mdMethodDef;
			if (comparandSignature[0] == ELEMENT_TYPE_VAR)
			{
				mdTypeDef declaringType;
				std::string methodName;
				if (FAILED(moduleDef.GetMethodProps(mdMethodDef, &declaringType, methodName, nullptr, nullptr, nullptr)))
					return ComparandComparison::Unsupported;

				owner = declaringType;
			}

			// Only a class constraint guarantees that every instantiation compares with ceq
			DWORD attributes;
			if (FAILED(moduleDef.GetGenericParamAttributes(owner, index, &attributes)))
				return ComparandComparison::Unsupported;

			return (attributes & gpReferenceTypeConstraint) != 0
				? ComparandComparison::Value
				: ComparandComparison::Unsupported;
		}

		default:
			// A value type may be an enum or an arbitrary struct, which ceq cannot compare
			return ComparandComparison::Unsupported;
	}
}

HRESULT LibProfiler::InstrumentFieldAccessIntrinsic(
	IN ICorProfilerInfo& corProfilerInfo,
	IN LibIPC::Client& client,
	IN ILRewriter& rewriter,
	IN ILInstr* callInstruction,
	IN ILInstr* addressInstruction,
	IN const FieldAccessIntrinsicKind intrinsicKind,
	IN const UINT16 importedLocalsCount,
	IN std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>& addedLocals,
	IN std::deque<std::vector<BYTE>>& ownedSignatures,
	IN const UINT64 instrumentationMark,
	IN const ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const InjectedMethodsMap& injectedMethods,
	IN const BOOL enableStackTraceCollection,
	IN const std::vector<std::string>& stackTraceFieldPatterns,
	OUT ILInstr** lastInstruction)
{
	auto const moduleId = moduleDef.GetModuleId();
	auto const isInstance = addressInstruction->m_opcode == CEE_LDFLDA;
	auto const fieldToken = static_cast<mdToken>(addressInstruction->m_Arg32);
	auto const originalOffset = addressInstruction->m_offset;
	auto const callOpcode = callInstruction->m_opcode;
	auto const callToken = static_cast<mdToken>(callInstruction->m_Arg32);
	ThreadID threadId;
	corProfilerInfo.GetCurrentThreadID(&threadId);

	auto kind = intrinsicKind;
	std::vector<BYTE> comparandSignature;
	auto comparison = ComparandComparison::Unsupported;
	if (kind == FieldAccessIntrinsicKind::CompareExchange)
	{
		if (SUCCEEDED(ResolveComparandSignature(moduleDef, callToken, comparandSignature)))
			comparison = ClassifyComparand(moduleDef, mdMethodDef, comparandSignature);

		if (comparison == ComparandComparison::Unsupported)
		{
			LOG_F(INFO, "Outcome of call %d in method %d from module %s cannot be computed, modeling it as a read-modify-write.",
				callToken,
				mdMethodDef,
				moduleDef.GetName().c_str());
			kind = FieldAccessIntrinsicKind::ReadModifyWrite;
		}
	}

	auto const isCompareExchange = kind == FieldAccessIntrinsicKind::CompareExchange;
	auto const hasRelease = kind != FieldAccessIntrinsicKind::VolatileRead;
	auto const hasAcquire = kind != FieldAccessIntrinsicKind::VolatileWrite;

	if (isInstance && !addressInstruction->m_objOperandIsObjRef)
	{
		// The object operand is a managed pointer or value type address
		return E_FAIL;
	}

	const auto captureStack = ShouldCaptureFieldStack(moduleDef, fieldToken, enableStackTraceCollection, stackTraceFieldPatterns);
	const auto releaseHelper = FindFieldAccessHelper(
		injectedMethods,
		isInstance ? LibIPC::RecordedEventType::InstanceFieldWrite : LibIPC::RecordedEventType::StaticFieldWrite,
		captureStack);
	const auto acquireHelper = FindFieldAccessHelper(
		injectedMethods,
		isInstance ? LibIPC::RecordedEventType::InstanceFieldRead : LibIPC::RecordedEventType::StaticFieldRead,
		captureStack);
	if ((hasRelease && releaseHelper == mdTokenNil) || (hasAcquire && acquireHelper == mdTokenNil))
	{
		LOG_F(ERROR, "Could not find injected %s field access methods for intrinsic in method %d from module %s.",
			captureStack ? "stack-capturing" : "plain",
			mdMethodDef,
			moduleDef.GetName().c_str());
		return E_FAIL;
	}

	if (callInstruction->m_pPrev->m_opcode == CEE_TAILCALL)
	{
		// Nothing may follow a tail call, so the prefix is dropped; the intrinsic never recurses, so the stack does not grow
		callInstruction->m_pPrev->m_opcode = CEE_NOP;
	}

	ILInstr* tailInstruction = nullptr;
	auto const emit = [&](const unsigned opcode) -> ILInstr*
	{
		auto const instruction = rewriter.NewILInstr();
		instruction->m_opcode = opcode;
		rewriter.InsertAfter(tailInstruction, instruction);
		tailInstruction = instruction;
		return instruction;
	};

	auto const addLocal = [&](const PCCOR_SIGNATURE signature, const ULONG signatureLength) -> INT16
	{
		addedLocals.emplace_back(signature, signatureLength);
		return static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 1));
	};

	INT16 instanceLocalIndex = 0;
	if (isInstance)
	{
		instanceLocalIndex = addLocal(g_ObjectTypeSignature, 1);

		// The address load becomes the head, so branches targeting it still capture the instance
		tailInstruction = MakeRoomBefore(rewriter, addressInstruction);
		emit(CEE_DUP);
		emit(CEE_STLOC)->m_Arg16 = instanceLocalIndex;
		// LDFLDA <field> with its prefixes follows
		tailInstruction = addressInstruction;
	}

	INT16 comparandLocalIndex = 0;
	INT16 resultLocalIndex = 0;
	if (isCompareExchange)
	{
		ownedSignatures.push_back(std::move(comparandSignature));
		auto const& storedSignature = ownedSignatures.back();
		auto const storedLength = static_cast<ULONG>(storedSignature.size());
		comparandLocalIndex = addLocal(storedSignature.data(), storedLength);
		if (comparison != ComparandComparison::Value)
			resultLocalIndex = addLocal(storedSignature.data(), storedLength);
	}

	auto const emitHelperCall = [&](const mdToken helperToken)
	{
		if (isInstance)
			emit(CEE_LDLOC)->m_Arg16 = instanceLocalIndex;

		emit(CEE_CALL)->m_Arg32 = static_cast<INT32>(helperToken);
	};

	if (hasRelease)
	{
		// The call becomes the head, so branches targeting it still report the release
		// The release is reported after all arguments are evaluated, right before the operation
		callInstruction->m_opcode = CEE_NOP;
		tailInstruction = callInstruction;
		if (isCompareExchange)
		{
			// The comparand is the last argument, so it is on top of the stack
			emit(CEE_DUP);
			emit(CEE_STLOC)->m_Arg16 = comparandLocalIndex;
		}

		emit(CEE_LDC_I8)->m_Arg64 = static_cast<INT64>(instrumentationMark + IntrinsicReleaseMarkOffset);
		emitHelperCall(releaseHelper);
		emit(callOpcode)->m_Arg32 = static_cast<INT32>(callToken);
	}
	else
	{
		tailInstruction = callInstruction;
	}

	if (hasAcquire)
	{
		// The acquire is reported after the operation returns, so it follows the write it observed
		if (isCompareExchange)
		{
			// The exchange succeeded when it returned the comparand, which selects the succeeded mark
			emit(CEE_DUP);
			if (comparison == ComparandComparison::Value)
			{
				emit(CEE_LDLOC)->m_Arg16 = comparandLocalIndex;
			}
			else
			{
				// Compare the bit patterns, like the exchange itself
				auto const loadBits = comparison == ComparandComparison::Bits32 ? CEE_LDIND_I4 : CEE_LDIND_I8;
				emit(CEE_STLOC)->m_Arg16 = resultLocalIndex;
				emit(CEE_LDLOCA)->m_Arg16 = resultLocalIndex;
				emit(loadBits);
				emit(CEE_LDLOCA)->m_Arg16 = comparandLocalIndex;
				emit(loadBits);
			}

			emit(CEE_CEQ);
			emit(CEE_CONV_I8);
			emit(CEE_LDC_I8)->m_Arg64 = static_cast<INT64>(instrumentationMark + IntrinsicFailedExchangeMarkOffset);
			emit(CEE_ADD);
		}
		else
		{
			emit(CEE_LDC_I8)->m_Arg64 = static_cast<INT64>(instrumentationMark + IntrinsicAcquireMarkOffset);
		}

		emitHelperCall(acquireHelper);
	}

	*lastInstruction = tailInstruction;

	LOG_F(INFO, "Instrumented %s field access intrinsic in method %d from module %s.",
		isInstance ? "instance" : "static",
		mdMethodDef,
		moduleDef.GetName().c_str());

	// Notify analysis of field access instrumentation points
	auto const notify = [&](const UINT64 markOffset, const LibIPC::FieldAccessKind accessKind)
	{
		client.Send(LibIPC::Helpers::CreateFieldAccessInstrumentationMsg(
			LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId),
			moduleId,
			mdMethodDef,
			originalOffset,
			fieldToken,
			instrumentationMark + markOffset,
			accessKind));
	};

	switch (kind)
	{
		case FieldAccessIntrinsicKind::VolatileRead:
			notify(IntrinsicAcquireMarkOffset, LibIPC::FieldAccessKind::Volatile);
			break;
		case FieldAccessIntrinsicKind::VolatileWrite:
			notify(IntrinsicReleaseMarkOffset, LibIPC::FieldAccessKind::Volatile);
			break;
		case FieldAccessIntrinsicKind::ReadModifyWrite:
			notify(IntrinsicReleaseMarkOffset, LibIPC::FieldAccessKind::Atomic);
			notify(IntrinsicAcquireMarkOffset, LibIPC::FieldAccessKind::Atomic);
			break;
		case FieldAccessIntrinsicKind::CompareExchange:
			notify(IntrinsicReleaseMarkOffset, LibIPC::FieldAccessKind::CompareExchange);
			notify(IntrinsicFailedExchangeMarkOffset, LibIPC::FieldAccessKind::CompareExchangeFailed);
			notify(IntrinsicSucceededExchangeMarkOffset, LibIPC::FieldAccessKind::CompareExchangeSucceeded);
			break;
	}

	return S_OK;
}

HRESULT LibProfiler::InstrumentInstanceFieldAccess(
	IN ICorProfilerInfo& corProfilerInfo,
	IN LibIPC::Client& client,
	IN ILRewriter& rewriter,
	IN ILInstr* currentInstruction,
	IN const UINT16 importedLocalsCount,
	IN std::vector<std::pair<PCCOR_SIGNATURE, ULONG>>& addedLocals,
	IN std::deque<std::vector<BYTE>>& ownedSignatures,
	IN const UINT64 instrumentationMark,
	IN const ModuleDef& moduleDef,
	IN const mdMethodDef mdMethodDef,
	IN const InjectedMethodsMap& injectedMethods,
	IN const BOOL enableStackTraceCollection,
	IN const std::vector<std::string>& stackTraceFieldPatterns,
	OUT ILInstr** nextInstruction)
{
	auto const isStore = currentInstruction->m_opcode == CEE_STFLD;
	auto const moduleId = moduleDef.GetModuleId();
	auto const fieldToken = static_cast<mdToken>(currentInstruction->m_Arg32);
	auto const originalOffset = currentInstruction->m_offset;
	ThreadID threadId;
	corProfilerInfo.GetCurrentThreadID(&threadId);

	if (!currentInstruction->m_objOperandIsObjRef)
	{
		// The object operand is a managed pointer or value type address.
		// Value types can be either passed by:
		// 1) reference - in this case we have no way to capture managed pointers
		// 2) value - in this case all threads operate on a copy. Data races are not possible.
		return E_FAIL;
	}

	// Obtain field signature (needed for the added local's type)
	PCCOR_SIGNATURE fieldSignature;
	ULONG fieldSignatureLength;
	mdToken declaringTypeToken;
	HRESULT hr;
	if (TypeFromToken(fieldToken) == mdtFieldDef)
	{
		hr = moduleDef.GetFieldProps(fieldToken, &declaringTypeToken, &fieldSignature, &fieldSignatureLength);
	}
	else if (TypeFromToken(fieldToken) == mdtMemberRef)
	{
		hr = moduleDef.GetFieldRefProps(fieldToken, &declaringTypeToken, &fieldSignature, &fieldSignatureLength);
	}
	else
	{
		LOG_F(ERROR, "Unsupported token type for field token %d in module %s.", fieldToken, moduleDef.GetName().c_str());
		return E_FAIL;
	}
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not obtain field signature for token %d in module %s. Error: 0x%x.", fieldToken, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	BOOL isVolatile;
	hr = IsVolatile(*currentInstruction, &isVolatile);
	if (FAILED(hr))
	{
		LOG_F(ERROR, "Could not analyze if field access is volatile for field token %d in module %s. Error: 0x%x.", fieldToken, moduleDef.GetName().c_str(), hr);
		return E_FAIL;
	}

	const auto captureStack = ShouldCaptureFieldStack(moduleDef, fieldToken, enableStackTraceCollection, stackTraceFieldPatterns);
	const auto fieldAccessEventType = isStore
		? LibIPC::RecordedEventType::InstanceFieldWrite
		: LibIPC::RecordedEventType::InstanceFieldRead;
	auto const fieldAccessMethodIt = injectedMethods.find(fieldAccessEventType);
	const auto helperToken = fieldAccessMethodIt != injectedMethods.end()
		? (captureStack ? fieldAccessMethodIt->second.withStackCapture : fieldAccessMethodIt->second.plain)
		: mdTokenNil;
	if (helperToken == mdTokenNil)
	{
		LOG_F(ERROR, "Could not find injected %s method for event type %d.",
			captureStack ? "stack-capturing" : "plain",
			static_cast<int>(fieldAccessEventType));
		return E_FAIL;
	}

	// For STFLD we need a temporary local whose type comes from the field signature.
	// If the field signature contains ELEMENT_TYPE_VAR (generic type params like !0), we must resolve it
	const BYTE* fieldSigForLocal = fieldSignature + 1;
	ULONG fieldSigForLocalLen = fieldSignatureLength - 1;

	if (isStore)
	{
		TypeArgs typeArgs;
		if (TypeFromToken(declaringTypeToken) == mdtTypeSpec)
		{
			PCCOR_SIGNATURE typeSpecSig;
			ULONG typeSpecSigLen;
			hr = moduleDef.GetTypeSpecProps(declaringTypeToken, &typeSpecSig, &typeSpecSigLen);
			if (FAILED(hr))
				return E_FAIL;

			if (!ParseTypeSpecGenericArgs(typeSpecSig, typeSpecSigLen, typeArgs))
			{
				LOG_F(
				    ERROR,
				    "Could not parse TypeSpec generic arguments for token %d in module %s.",
					declaringTypeToken,
					moduleDef.GetName().c_str());
				return E_FAIL;
			}
		}

		std::vector<BYTE> resolvedSig;
		switch (ResolveSigType(fieldSigForLocal, fieldSigForLocalLen, typeArgs, resolvedSig))
		{
			case SigTypeResolution::Unchanged:
				// The field signature is already closed and types the local as it stands
				break;

			case SigTypeResolution::Substituted:
			{
				ownedSignatures.push_back(std::move(resolvedSig));
				auto const& stored = ownedSignatures.back();
				fieldSigForLocal = stored.data();
				fieldSigForLocalLen = static_cast<ULONG>(stored.size());
				break;
			}

			case SigTypeResolution::Failed:
				LOG_F(
				    INFO,
				    "Skipping instance field write instrumentation for field token %d in method %d from module %s. Could not close the generic field signature.",
					fieldToken,
					mdMethodDef,
					moduleDef.GetName().c_str());
				return E_FAIL;
		}
	}

	// The event must reach the queue before the store executes (release-before-store)
	auto accessInstruction = currentInstruction;
	auto tailInstruction = MakeRoomBefore(rewriter, accessInstruction);

	// Add a local to capture the object instance for the handler call
	PCCOR_SIGNATURE instanceSignature = g_ObjectTypeSignature;
	addedLocals.emplace_back(instanceSignature, 1);
	const auto instanceLocalIndex = static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 1));

	if (isStore)
	{
		// STLOC <value-local> (store written value)
		addedLocals.emplace_back(fieldSigForLocal, fieldSigForLocalLen);
		const auto valueLocalIndex = static_cast<INT16>(importedLocalsCount + static_cast<UINT16>(addedLocals.size() - 1));
		const auto stlocValueInstruction = rewriter.NewILInstr();
		stlocValueInstruction->m_opcode = CEE_STLOC;
		stlocValueInstruction->m_Arg16 = valueLocalIndex;
		rewriter.InsertAfter(tailInstruction, stlocValueInstruction);
		tailInstruction = stlocValueInstruction;
		// DUP (duplicate object reference)
		const auto dupInstruction = rewriter.NewILInstr();
		dupInstruction->m_opcode = CEE_DUP;
		rewriter.InsertAfter(tailInstruction, dupInstruction);
		tailInstruction = dupInstruction;
		// STLOC <instance-local> (save object reference copy)
		const auto stlocInstanceInstruction = rewriter.NewILInstr();
		stlocInstanceInstruction->m_opcode = CEE_STLOC;
		stlocInstanceInstruction->m_Arg16 = instanceLocalIndex;
		rewriter.InsertAfter(tailInstruction, stlocInstanceInstruction);
		tailInstruction = stlocInstanceInstruction;
		// LDLOC <value-local> (restore written value)
		const auto ldlocValueInstruction = rewriter.NewILInstr();
		ldlocValueInstruction->m_opcode = CEE_LDLOC;
		ldlocValueInstruction->m_Arg16 = valueLocalIndex;
		rewriter.InsertAfter(tailInstruction, ldlocValueInstruction);
		tailInstruction = ldlocValueInstruction;
		// LDC.I8 <instrumentation-mark>
		const auto ldcInstruction = rewriter.NewILInstr();
		ldcInstruction->m_opcode = CEE_LDC_I8;
		ldcInstruction->m_Arg64 = static_cast<INT64>(instrumentationMark);
		rewriter.InsertAfter(tailInstruction, ldcInstruction);
		tailInstruction = ldcInstruction;
		// LDLOC <instance-local> (restore object reference)
		const auto ldlocInstanceInstruction = rewriter.NewILInstr();
		ldlocInstanceInstruction->m_opcode = CEE_LDLOC;
		ldlocInstanceInstruction->m_Arg16 = instanceLocalIndex;
		rewriter.InsertAfter(tailInstruction, ldlocInstanceInstruction);
		tailInstruction = ldlocInstanceInstruction;
		// CALL <injected-method-handler>
		const auto callInstruction = rewriter.NewILInstr();
		callInstruction->m_opcode = CEE_CALL;
		callInstruction->m_Arg32 = static_cast<INT32>(helperToken);
		rewriter.InsertAfter(tailInstruction, callInstruction);
	}
	else
	{
		// DUP (duplicate object reference)
		const auto dupInstruction = rewriter.NewILInstr();
		dupInstruction->m_opcode = CEE_DUP;
		rewriter.InsertAfter(tailInstruction, dupInstruction);
		tailInstruction = dupInstruction;
		// STLOC <instance-local> (save object reference copy)
		const auto stlocInstanceInstruction = rewriter.NewILInstr();
		stlocInstanceInstruction->m_opcode = CEE_STLOC;
		stlocInstanceInstruction->m_Arg16 = instanceLocalIndex;
		rewriter.InsertAfter(tailInstruction, stlocInstanceInstruction);
	}

	// <FIELD-ACCESS> (LDFLD/STFLD) with its prefixes follows
	if (isStore)
	{
		currentInstruction = accessInstruction;
	}
	else
	{
		// Reads keep emitting after the load (acquire side)
		// LDC.I8 <instrumentation-mark>
		const auto ldcInstruction = rewriter.NewILInstr();
		ldcInstruction->m_opcode = CEE_LDC_I8;
		ldcInstruction->m_Arg64 = static_cast<INT64>(instrumentationMark);
		rewriter.InsertAfter(accessInstruction, ldcInstruction);
		// LDLOC <instance-local> (restore object reference)
		const auto ldlocInstanceInstruction = rewriter.NewILInstr();
		ldlocInstanceInstruction->m_opcode = CEE_LDLOC;
		ldlocInstanceInstruction->m_Arg16 = instanceLocalIndex;
		rewriter.InsertAfter(ldcInstruction, ldlocInstanceInstruction);
		// CALL <injected-method-handler>
		const auto callInstruction = rewriter.NewILInstr();
		callInstruction->m_opcode = CEE_CALL;
		callInstruction->m_Arg32 = static_cast<INT32>(helperToken);
		rewriter.InsertAfter(ldlocInstanceInstruction, callInstruction);
		// Skip to next non-injected instruction
		currentInstruction = callInstruction;
	}

	LOG_F(INFO, "Instrumented instance field %s access in method %d with stub %d from module %s.",
		isStore ? "write" : "read",
		mdMethodDef,
		helperToken,
		moduleDef.GetName().c_str());

	// Notify analysis of field access instrumentation point
	client.Send(LibIPC::Helpers::CreateFieldAccessInstrumentationMsg(
		LibIPC::Helpers::CreateMetadataMsg(LibProfiler::PAL_GetCurrentPid(), threadId),
		moduleId,
		mdMethodDef,
		originalOffset,
		fieldToken,
		instrumentationMark,
		isVolatile ? LibIPC::FieldAccessKind::Volatile : LibIPC::FieldAccessKind::Regular));

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