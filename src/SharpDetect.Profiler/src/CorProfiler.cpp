/*
 * Copyright (C) 2020, Andrej Čižmárik
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

 // Based on project microsoftarchive/clrprofiler/ILRewrite
 // Original source: https://github.com/microsoftarchive/clrprofiler/tree/master/ILRewrite
 // Copyright (c) .NET Foundation and contributors. All rights reserved.
 // Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "Stdafx.h"
#include "GlobalHooks.h"
#include "CorProfiler.h"
#include "TinyMethodUser.h"
#include "InstructionFactory.h"
#include "ILGenerator.h"
#include "PAL.h"
#include "MessageFactory.h"
#include <array>
#include <cstdint>
#include <algorithm>
#include <memory>
#include <utility>

using namespace SharpDetect::Common::Messages;

CorProfiler::CorProfiler()
{
}

CorProfiler::~CorProfiler()
{
}

HRESULT STDMETHODCALLTYPE CorProfiler::Initialize(IUnknown *pICorProfilerInfoUnk)
{
	auto hr = HRESULT(E_FAIL);

	// Initialize base
	hr = CorProfilerBase::Initialize(pICorProfilerInfoUnk);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not query ICorProfilerInfo.");

	// Register for profiler notifications
	auto eventMaskRaw = PAL::ReadEnvironmentVariable("SHARPDETECT_Profiling_Flags");
	auto eventMask = std::stoi(eventMaskRaw, 0, 10);
	hr = this->pCorProfilerInfo->SetEventMask(eventMask);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not set event mask " + std::to_string(eventMask));

	// Set global method enter/leave hooks
	hr = pCorProfilerInfo->SetEnterLeaveFunctionHooks3WithInfo(EnterNaked, LeaveNaked, TailcallNaked);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not set enter/leave hooks.");

	// Set mapping function for hooks
	hr = pCorProfilerInfo->SetFunctionIDMapper2(&FunctionIdMapper2, this);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not set function mapper for enter/leave hooks");

	// Other initialization
	pInstrumentationContext = std::make_unique<InstrumentationContext>(*pMessagingClient, *pCorProfilerInfo, *pLogger);

	// Send profiler initialized
	auto message = MessageFactory::ProfilerInitialized(pInstrumentationContext->GetCurrentThreadId());
	pMessagingClient->SendNotification(std::move(message));

	LOG_INFO(pLogger, "Successfully initialized SharpDetect.Profiler.");
	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::Shutdown()
{
	LOG_INFO(pLogger, "Terminating SharpDetect.Profiler ...");

	// Send profiler destroyed
	auto message = MessageFactory::ProfilerDestroyed(pInstrumentationContext->GetCurrentThreadId());
	pMessagingClient->SendNotification(std::move(message));

	LOG_INFO(pLogger, "Terminated SharpDetect.Profiler.");
	pMessagingClient->Shutdown();
	CorProfilerBase::Shutdown();

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ModuleLoadFinished(ModuleID moduleId, HRESULT hrStatus)
{
	// We are not interested in unsuccessful assembly loads
	if (hrStatus != S_OK)
		return S_OK;
	HRESULT hr;

	// Get module metadata
	{
		std::lock_guard<std::mutex> lock(metadataMutex);
		metadata.emplace(std::make_pair(moduleId, std::make_unique<ModuleMetadata>()));
	}

	auto& moduleMetadata = GetModuleMetadata(moduleId);
	hr = moduleMetadata.Initialize(pCorProfilerInfo, moduleId);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not initialize module metadata.");

	// Pack information about module load
	auto message = MessageFactory::ModuleLoaded(pInstrumentationContext->GetCurrentThreadId(), moduleId, moduleMetadata.GetModulePath());
	const auto notificationId = pMessagingClient->GetNewNotificationId();
	auto requestFuture = pMessagingClient->ReceiveRequest(notificationId);
	pMessagingClient->SendNotification(std::move(message), notificationId);
	
	if (moduleMetadata.GetName() == pInstrumentationContext->GetCoreLibraryName())
	{
		// Import assembly props of core library
		hr = pInstrumentationContext->ImportCoreLibInfo(moduleMetadata);
		LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not import info about core library " + ToString(moduleMetadata.GetModulePath()));
		// Generate helper methods in core library
		hr = pInstrumentationContext->CreateHelperMethods(moduleMetadata);
		LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not generate helper methods in core library " + ToString(moduleMetadata.GetModulePath()));
	}
	else
	{
		// Other assemblies need to reference the helper methods
		hr = pInstrumentationContext->ImportHelpers(moduleMetadata);
		LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not import helper methods in " + ToString(moduleMetadata.GetModulePath()));
	}

	// Check if we need to wrap extern methods
	auto request = requestFuture.get();
	if (request.Payload_case() == RequestMessage::PayloadCase::kWrapping)
	{
		auto& wrapping = request.wrapping();
		auto identifier = std::array<WCHAR, 511>();

		// Inject wrappers
		for (auto&& method : wrapping.methodstowrap())
		{
			hr = pInstrumentationContext->WrapExternMethod(moduleMetadata, method.typetoken(), method.functiontoken(), method.parameterscount());
			LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, 
				"Could not inject wrapper for external method " + std::to_string(method.functiontoken()) + 
				" defined by module " + ToString(moduleMetadata.GetModulePath()));
		}
	}

	auto response = MessageFactory::RequestProcessed(pInstrumentationContext->GetCurrentThreadId(), request.requestid(), true);
	pMessagingClient->SendResponse(std::move(response), request.requestid());

	hr = pInstrumentationContext->ImportWrappers(moduleMetadata);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not import wrapper methods in " + ToString(moduleMetadata.GetModulePath()));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ClassLoadFinished(ClassID classId, HRESULT hrStatus)
{
	// We are not interested in unsuccessful class loads
	if (hrStatus != S_OK)
		return S_OK;
	HRESULT hr;

	// Get defining module and class token
	ModuleID moduleId;
	mdTypeDef classToken;
	hr = pCorProfilerInfo->GetClassIDInfo2(classId, &moduleId, &classToken, nullptr, 0, nullptr, nullptr);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not GetClassIdInfo2 about type " + std::to_string(classId));

	// Pack information about class load
	auto message = MessageFactory::TypeLoaded(pInstrumentationContext->GetCurrentThreadId(), moduleId, classToken);
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::JITCompilationStarted(FunctionID functionId, BOOL fIsSafeToBlock)
{
	auto hr = HRESULT(E_FAIL);
	auto functionToken = mdMethodDef(mdMethodDefNil);
	auto classToken = mdTypeDef(mdTypeDefNil);
	auto classId = ClassID(0);
	auto moduleId = ModuleID(0);

	// Get basic function information
	hr = pCorProfilerInfo->GetFunctionInfo2(functionId, 0, &classId, &moduleId, &functionToken, 0, nullptr, nullptr);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not GetFunctionInfo2 about method " + std::to_string(functionId));
	// Get function metadata token
	auto& moduleMetadata = GetModuleMetadata(moduleId);
	auto metadataImport = *&moduleMetadata.MetadataModuleImport;
	hr = metadataImport->GetMethodProps(functionToken, &classToken, nullptr, 0, nullptr, nullptr, nullptr, nullptr, nullptr, nullptr);
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not GetMethodProps for method " + std::to_string(functionId));
	
	// Make sure the client registers that we are waiting for a request
	auto message = MessageFactory::JITCompilationStarted(pInstrumentationContext->GetCurrentThreadId(), moduleId, classToken, functionToken);
	const auto notificationId = pMessagingClient->GetNewNotificationId();
	auto requestFuture = pMessagingClient->ReceiveRequest(notificationId);
	pMessagingClient->SendNotification(std::move(message), notificationId);

	// Hold execution - SharpDetect.Analyser needs to respond whether we are changing the method or not
	auto request = requestFuture.get();
	// Requested instrumentation => edit bytecode before JIT compiles the method
	if (request.Payload_case() == RequestMessage::PayloadCase::kInstrumentation)
	{
		auto& instrumentation = request.instrumentation();

		// Inject enter/leave hooks
		if (instrumentation.injecthooks())
		{
			// Prepare information about captured arguments, if available
			auto argumentInfos = std::vector<std::tuple<UINT16, UINT16, bool>>();
			auto totalArgumentsSize = size_t(0);
			auto totalIndirectArgumentsSize = size_t(0);
			if (instrumentation.argumentinfos().size() > 0)
			{
				auto& data = instrumentation.argumentinfos();
				auto indirects = instrumentation.passingbyrefinfos();
				for (size_t i = 0; i < data.size(); i += 4)
				{
					UINT16 index = data[i + 1] << 8 | data[i];
					UINT16 size = data[i + 3] << 8 | data[i + 2];
					bool isIndirect = (indirects[index / 8] & (1 << index % 8));
					totalArgumentsSize += size;
					if (isIndirect)
						totalIndirectArgumentsSize += size;

					argumentInfos.emplace_back(std::make_tuple(index, size, isIndirect));
				}
			}

			RegisterHook(functionId, moduleId, classToken, functionToken,
				instrumentation.capturearguments(),
				instrumentation.capturereturnvalue(),
				totalArgumentsSize,
				totalIndirectArgumentsSize,
				std::move(argumentInfos));
		}

		// Required instrumentation
		if (instrumentation.bytecode().size() > 0)
		{
			auto bytecode = instrumentation.bytecode().c_str();
			auto size = instrumentation.bytecode().size();

			// Allocate memory for the method
			auto memory = static_cast<LPBYTE>(moduleMetadata.MethodAllocator->Alloc(size));
			LOG_ERROR_AND_RET_IF(memory == nullptr, pLogger, "Could not allocate memory for method " + std::to_string(functionId));
			// Set its bytecode
			std::copy(bytecode, bytecode + size, memory);
			
			// Emit changes
			hr = pCorProfilerInfo->SetILFunctionBody(moduleId, functionToken, memory);
			LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not SetILFunctionBody for method " + std::to_string(functionId));
		}
	}

	auto response = MessageFactory::RequestProcessed(pInstrumentationContext->GetCurrentThreadId(), request.requestid(), true);
	pMessagingClient->SendResponse(std::move(response), request.requestid());

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ThreadCreated(ThreadID threadId)
{
	auto message = MessageFactory::ThreadCreated(pInstrumentationContext->GetCurrentThreadId(), threadId);
	pMessagingClient->SendNotification(std::move(message));
	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::ThreadDestroyed(ThreadID threadId)
{
	auto message = MessageFactory::ThreadDestroyed(pInstrumentationContext->GetCurrentThreadId(), threadId);
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::GarbageCollectionFinished()
{
	// Get generation-bounds information
	auto boundsInfo = GetGenerationBounds();
	auto pBounds = std::move(std::get<0>(boundsInfo));
	auto cBounds = std::get<1>(boundsInfo);
	auto hr = (cBounds != -1) ? S_OK : E_FAIL;
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not get generation bounds information.");

	// Pack information about garbage collection finished
	auto message = MessageFactory::GarbageCollectionFinished(
		pInstrumentationContext->GetCurrentThreadId(),
		reinterpret_cast<BYTE*>(pBounds.get()), cBounds * sizeof(COR_PRF_GC_GENERATION_RANGE));

	// Make sure the client registers that we are waiting for a request
	const auto notificationId = pMessagingClient->GetNewNotificationId();
	auto requestFuture = pMessagingClient->ReceiveRequest(notificationId);
	pMessagingClient->SendNotification(std::move(message), notificationId);

	// Hold execution until we receive a response
	auto request = requestFuture.get();
	LOG_ERROR_IF(request.Payload_case() != RequestMessage::PayloadCase::kContinueExecution, pLogger, "Unexpected request. Continuing execution...");

	// Acknowledge that we are continuing execution
	auto response = MessageFactory::RequestProcessed(pInstrumentationContext->GetCurrentThreadId(), request.requestid(), true);
	pMessagingClient->SendResponse(std::move(response), request.requestid());

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::GarbageCollectionStarted(int cGenerations, BOOL generationCollected[], COR_PRF_GC_REASON reason)
{
	// Get generation-bounds information
	auto boundsInfo = GetGenerationBounds();
	auto pBounds = std::move(std::get<0>(boundsInfo));
	auto cBounds = std::get<1>(boundsInfo);
	auto hr = (cBounds != -1) ? S_OK : E_FAIL;
	LOG_ERROR_AND_RET_IF(FAILED(hr), pLogger, "Could not get generation bounds information.");

	// Pack information about garbage collection started
	auto message = MessageFactory::GarbageCollectionStarted(
		pInstrumentationContext->GetCurrentThreadId(),
		reinterpret_cast<BYTE*>(generationCollected), cGenerations * sizeof(BOOL),
		reinterpret_cast<BYTE*>(pBounds.get()), cBounds * sizeof(COR_PRF_GC_GENERATION_RANGE));
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

std::tuple<std::unique_ptr<COR_PRF_GC_GENERATION_RANGE[]>, ULONG> CorProfiler::GetGenerationBounds()
{
	// Get generation-bounds information
	std::unique_ptr<COR_PRF_GC_GENERATION_RANGE[]> pBoundsInfo;
	auto cBoundsInfo = ULONG(0);
	auto hr = pCorProfilerInfo->GetGenerationBounds(0, &cBoundsInfo, nullptr);

	if (!FAILED(hr))
	{
		// Get generation-bounds segments information
		pBoundsInfo = std::make_unique<COR_PRF_GC_GENERATION_RANGE[]>(cBoundsInfo);
		hr = pCorProfilerInfo->GetGenerationBounds(cBoundsInfo, &cBoundsInfo, pBoundsInfo.get());
	}

	if (FAILED(hr))
	{
		// Signalize errors as -1 array size
		cBoundsInfo = -1;
	}

	return std::make_tuple(std::move(pBoundsInfo), cBoundsInfo);
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeSuspendStarted(COR_PRF_SUSPEND_REASON suspendReason)
{
	auto message = MessageFactory::RuntimeSuspendStarted(pInstrumentationContext->GetCurrentThreadId(), suspendReason);
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeSuspendFinished()
{
	auto message = MessageFactory::RuntimeSuspendFinished(pInstrumentationContext->GetCurrentThreadId());
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT __stdcall CorProfiler::RuntimeResumeStarted()
{
	auto message = MessageFactory::RuntimeResumeStarted(pInstrumentationContext->GetCurrentThreadId());
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT __stdcall CorProfiler::RuntimeResumeFinished()
{
	auto message = MessageFactory::RuntimeResumeFinished(pInstrumentationContext->GetCurrentThreadId());
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeThreadSuspended(ThreadID threadId)
{
	auto message = MessageFactory::RuntimeThreadSuspended(pInstrumentationContext->GetCurrentThreadId(), threadId);
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::RuntimeThreadResumed(ThreadID threadId)
{
	auto message = MessageFactory::RuntimeThreadResumed(pInstrumentationContext->GetCurrentThreadId(), threadId);
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::MovedReferences2(ULONG cMovedObjectIDRanges, ObjectID oldObjectIDRangeStart[], ObjectID newObjectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
	auto constexpr ptrSize = sizeof(ObjectID);
	auto byteSize = cMovedObjectIDRanges * ptrSize;
	auto message = MessageFactory::MovedReferences(
		pInstrumentationContext->GetCurrentThreadId(),
		reinterpret_cast<BYTE*>(oldObjectIDRangeStart), byteSize,
		reinterpret_cast<BYTE*>(newObjectIDRangeStart), byteSize,
		reinterpret_cast<BYTE*>(cObjectIDRangeLength), byteSize);
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

HRESULT STDMETHODCALLTYPE CorProfiler::SurvivingReferences2(ULONG cSurvivingObjectIDRanges, ObjectID objectIDRangeStart[], SIZE_T cObjectIDRangeLength[])
{
	auto constexpr ptrSize = sizeof(ObjectID);
	auto byteSize = cSurvivingObjectIDRanges * ptrSize;
	auto message = MessageFactory::SurvivingReferences(
		pInstrumentationContext->GetCurrentThreadId(),
		reinterpret_cast<BYTE*>(objectIDRangeStart), byteSize,
		reinterpret_cast<BYTE*>(cObjectIDRangeLength), byteSize);
	pMessagingClient->SendNotification(std::move(message));

	return S_OK;
}

void CorProfiler::EnterMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo)
{
	// Get information about the method
	auto functionId = FunctionID(0);
	auto pRawFunctionInfo = (void*)functionIDOrClientID.clientID;
	auto pFunctionInfo = static_cast<FunctionInfo*>(pRawFunctionInfo);
	pCorProfilerInfo->GetFunctionFromToken(pFunctionInfo->ModuleId, pFunctionInfo->FunctionToken, &functionId);

	// If we do not track arguments, we can just exit
	if (!pFunctionInfo->CaptureArguments)
	{
		// Send notification
		auto message = MessageFactory::MethodCalled(
			pInstrumentationContext->GetCurrentThreadId(),
			pFunctionInfo->ModuleId,
			pFunctionInfo->ClassToken,
			pFunctionInfo->FunctionToken);
		pMessagingClient->SendNotification(std::move(message));
		return;
	}

	// Get information about arguments
	COR_PRF_FRAME_INFO frameInfo;
	auto cbArgumentInfo = ULONG(0);
	pCorProfilerInfo->GetFunctionEnter3Info(functionId, eltInfo, &frameInfo, &cbArgumentInfo, nullptr);
	
	// Retrieve spilled arguments from memory
	auto pArgumentInfos = std::make_unique<COR_PRF_FUNCTION_ARGUMENT_INFO[]>(cbArgumentInfo);
	pCorProfilerInfo->GetFunctionEnter3Info(functionId, eltInfo, &frameInfo, &cbArgumentInfo, pArgumentInfos.get());
	
	// Copy arguments
	const auto cbArgumentValues = pFunctionInfo->TotalArgumentValuesSize;
	const auto cbArgumentInfos = pFunctionInfo->ArgumentInfos.size() * sizeof(UINT32);
	auto pArgumentValues = std::make_unique<BYTE[]>(cbArgumentValues);
	auto pArgumentOffsets = std::make_unique<BYTE[]>(cbArgumentInfos);
	auto pArgumentValue = pArgumentValues.get();
	auto pArgumentOffset = pArgumentOffsets.get();
	auto indirectAddrs = std::vector<BYTE*>();
	GetArguments(*pFunctionInfo, indirectAddrs, pArgumentInfos.get(), pArgumentValues.get(), pArgumentOffsets.get());

	// Save information about by-ref arguments for the method leave callback
	pFunctionInfo->GetStack().emplace(std::move(indirectAddrs));

	// Send notification
	auto message = MessageFactory::MethodCalledWithArguments(
		pInstrumentationContext->GetCurrentThreadId(),
		pFunctionInfo->ModuleId,
		pFunctionInfo->ClassToken,
		pFunctionInfo->FunctionToken,
		pArgumentValues.get(),
		cbArgumentValues,
		pArgumentOffsets.get(),
		cbArgumentInfos);
	pMessagingClient->SendNotification(std::move(message));
}

void CorProfiler::LeaveMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo)
{
	// Get information about the method
	auto functionId = FunctionID(0);
	auto pRawFunctionInfo = (void*)functionIDOrClientID.clientID;
	auto pFunctionInfo = static_cast<FunctionInfo*>(pRawFunctionInfo);
	pCorProfilerInfo->GetFunctionFromToken(pFunctionInfo->ModuleId, pFunctionInfo->FunctionToken, &functionId);

	// If we do not track return value, we can just exit
	if (!pFunctionInfo->CaptureReturnValue)
	{
		// Send notification
		auto message = MessageFactory::MethodReturned(
			pInstrumentationContext->GetCurrentThreadId(),
			pFunctionInfo->ModuleId,
			pFunctionInfo->ClassToken,
			pFunctionInfo->FunctionToken);
		pMessagingClient->SendNotification(std::move(message));
		return;
	}

	// Get information about return value
	COR_PRF_FRAME_INFO frameInfo;
	COR_PRF_FUNCTION_ARGUMENT_RANGE returnValueInfo;
	pCorProfilerInfo->GetFunctionLeave3Info(functionId, eltInfo, &frameInfo, &returnValueInfo);
	auto pReturnValue = std::make_unique<BYTE[]>(returnValueInfo.length);
	std::memcpy(pReturnValue.get(), (void*)returnValueInfo.startAddress, returnValueInfo.length);

	// Get information about indirects
	auto& indirects = pFunctionInfo->GetStack().top();
	auto cbArgumentValues = size_t(0);
	auto cbArgumentInfos = size_t(0);
	std::unique_ptr<BYTE[]> pArgumentValues = nullptr;
	std::unique_ptr<BYTE[]> pArgumentOffsets = nullptr;
	if (indirects.size() > 0)
	{
		// Copy arguments
		cbArgumentValues = pFunctionInfo->TotalIndirectArgumentValuesSize;
		cbArgumentInfos = indirects.size() * sizeof(UINT32);
		pArgumentValues = std::make_unique<BYTE[]>(cbArgumentValues);
		pArgumentOffsets = std::make_unique<BYTE[]>(cbArgumentInfos);
		GetByRefArguments(*pFunctionInfo, indirects, pArgumentValues.get(), pArgumentOffsets.get());
	}
	
	// Pop information about by-ref arguments
	pFunctionInfo->GetStack().pop();

	// Send notification
	auto message = MessageFactory::MethodReturnedWithReturnValue(
		pInstrumentationContext->GetCurrentThreadId(),
		pFunctionInfo->ModuleId,
		pFunctionInfo->ClassToken,
		pFunctionInfo->FunctionToken,
		pReturnValue.get(),
		returnValueInfo.length,
		pArgumentValues.get(),
		cbArgumentValues,
		pArgumentOffsets.get(),
		cbArgumentInfos);
	pMessagingClient->SendNotification(std::move(message));
}

void CorProfiler::TailcallMethod(FunctionIDOrClientID functionIDOrClientID, COR_PRF_ELT_INFO& eltInfo)
{
}

void CorProfiler::GetArguments(const FunctionInfo& functionInfo, std::vector<BYTE*>& indirectAddrs, COR_PRF_FUNCTION_ARGUMENT_INFO* pArgInfos, BYTE* pArgValues, BYTE* pArgOffsets)
{
	// Copy arguments
	auto pArgumentValue = pArgValues;
	auto pArgumentOffset = pArgOffsets;
	for (auto i = 0; i < functionInfo.ArgumentInfos.size(); i++)
	{
		auto [argIndex, argSize, isIndirectLoad] = functionInfo.ArgumentInfos[i];
		auto& range = pArgInfos->ranges[argIndex];

		if (isIndirectLoad)
		{
			// Get pointer to the value
			auto pointerToValue = UINT_PTR(0);
			std::memcpy(&pointerToValue, (void*)range.startAddress, range.length);
			indirectAddrs.push_back((BYTE*)pointerToValue);

			// Read the value
			std::memcpy(pArgumentValue, (void*)pointerToValue, argSize);
			UINT32 argInfo = (argIndex << 16) | (argSize);
			std::memcpy(pArgumentOffset, (void*)&argInfo, sizeof(UINT32));
			pArgumentValue += argSize;
		}
		else
		{
			// Directly read the value
			UINT32 argInfo = (argIndex << 16) | (range.length);
			std::memcpy(pArgumentValue, (void*)range.startAddress, range.length);
			std::memcpy(pArgumentOffset, (void*)&argInfo, sizeof(UINT32));
			pArgumentValue += range.length;
		}

		pArgumentOffset += sizeof(UINT32);
	}
}



void CorProfiler::GetByRefArguments(const FunctionInfo& functionInfo, const std::vector<BYTE*>& indirectAddrs, BYTE* pArgValues, BYTE* pArgOffsets)
{
	// Copy arguments
	auto pArgumentValue = pArgValues;
	auto pArgumentOffset = pArgOffsets;
	auto indirectsCount = 0;
	for (auto i = 0; i < functionInfo.ArgumentInfos.size(); i++)
	{
		auto [argIndex, argSize, isIndirectLoad] = functionInfo.ArgumentInfos[i];
		if (!isIndirectLoad)
			continue;

		UINT32 argInfo = (argIndex << 16) | (argSize);
		std::memcpy(pArgumentValue, (void*)indirectAddrs[indirectsCount], argSize);
		std::memcpy(pArgumentOffset, (void*)&argInfo, sizeof(UINT32));
		pArgumentValue += argSize;
		pArgumentOffset += sizeof(UINT32);
		indirectsCount++;
	}
}

ModuleMetadata& CorProfiler::GetModuleMetadata(ModuleID module)
{
	std::lock_guard<std::mutex> lock(metadataMutex);
	return *metadata[module];
}
