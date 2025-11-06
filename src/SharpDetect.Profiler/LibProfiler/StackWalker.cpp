// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "../lib/loguru/loguru.hpp"
#include "PAL.h"
#include "StackWalker.h"

using namespace LibProfiler;

HRESULT StackWalker::CaptureStackTrace(
	ICorProfilerInfo10* corProfilerInfo,
	ThreadID threadId,
	std::vector<UINT64>& moduleIds,
	std::vector<UINT32>& methodTokens)
{
	if (corProfilerInfo == nullptr)
	{
		LOG_F(ERROR, "CorProfilerInfo is null.");
		return E_POINTER;
	}

	moduleIds.clear();
	methodTokens.clear();

	if (FAILED(corProfilerInfo->SuspendRuntime()))
	{
		LOG_F(ERROR, "Failed to suspend runtime before capturing stack trace.");
		return E_FAIL;
	}

	StackWalkContext context;
	context.CorProfilerInfo = corProfilerInfo;
	context.ModuleIds = &moduleIds;
	context.MethodTokens = &methodTokens;

	HRESULT hr = corProfilerInfo->DoStackSnapshot(
		threadId,
		StackSnapshotCallback,
		0, // flags - walk only managed frames
		&context,
		nullptr,
		0);

	if (FAILED(corProfilerInfo->ResumeRuntime()))
	{
		LOG_F(ERROR, "Failed to resume runtime after capturing stack trace.");
		return E_FAIL;
	}

	if (FAILED(hr))
	{
		LOG_F(ERROR, "DoStackSnapshot failed for thread %" UINT_PTR_FORMAT ". Error: 0x%x.", threadId, hr);
		return hr;
	}

	return S_OK;
}

HRESULT StackWalker::CaptureStackTraces(
	ICorProfilerInfo10* corProfilerInfo,
	const std::vector<UINT64>& threadIds,
	std::vector<std::vector<StackFrame>>& frames)
{
	if (corProfilerInfo == nullptr)
	{
		LOG_F(ERROR, "CorProfilerInfo is null.");
		return E_POINTER;
	}

	if (FAILED(corProfilerInfo->SuspendRuntime()))
	{
		LOG_F(ERROR, "Failed to suspend runtime before capturing stack traces.");
		return E_FAIL;
	}

	frames.clear();
	frames.reserve(threadIds.size());

	HRESULT overallResult = S_OK;
	for (auto threadId : threadIds)
	{
		std::vector<UINT64> moduleIds;
		std::vector<UINT32> methodTokens;

		StackWalkContext context;
		context.CorProfilerInfo = corProfilerInfo;
		context.ModuleIds = &moduleIds;
		context.MethodTokens = &methodTokens;

		HRESULT hr = corProfilerInfo->DoStackSnapshot(
			threadId,
			StackSnapshotCallback,
			0, // flags
			&context,
			nullptr,
			0);

		if (SUCCEEDED(hr))
		{
			std::vector<StackFrame> threadFrames;
			threadFrames.reserve(moduleIds.size());
			for (size_t i = 0; i < moduleIds.size(); ++i)
			{
				threadFrames.push_back({ moduleIds[i], methodTokens[i] });
			}
			frames.push_back(std::move(threadFrames));
		}
		else
		{
			LOG_F(WARNING, "Failed to capture stack trace for thread %" UINT_PTR_FORMAT ". Error: 0x%x.", threadId, hr);
			// Add empty frame list to maintain order
			frames.push_back(std::vector<StackFrame>());
			overallResult = hr;
		}
	}

	if (FAILED(corProfilerInfo->ResumeRuntime()))
	{
		LOG_F(ERROR, "Failed to resume runtime after capturing stack traces.");
		return E_FAIL;
	}

	return overallResult;
}

HRESULT STDMETHODCALLTYPE StackWalker::StackSnapshotCallback(
	FunctionID funcId,
	UINT_PTR ip,
	COR_PRF_FRAME_INFO frameInfo,
	ULONG32 contextSize,
	BYTE context[],
	void* clientData)
{
	auto* walkContext = static_cast<StackWalkContext*>(clientData);

	if (funcId == 0)
		return S_OK;

	ModuleID moduleId;
	mdMethodDef methodToken;
	HRESULT hr = walkContext->CorProfilerInfo->GetFunctionInfo(funcId, nullptr, &moduleId, &methodToken);
	if (SUCCEEDED(hr))
	{
		walkContext->ModuleIds->push_back(moduleId);
		walkContext->MethodTokens->push_back(methodToken);
	}
	else
	{
		LOG_F(WARNING, "Failed to get function info for FunctionID %" UINT_PTR_FORMAT ". Error: 0x%x.", funcId, hr);
	}

	return S_OK;
}

