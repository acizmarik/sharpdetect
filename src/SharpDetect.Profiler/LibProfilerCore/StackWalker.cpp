// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include <atomic>
#include <cstring>

#ifndef _WIN32
#include <unwind.h>
#else
#include <windows.h>
#endif

#include "../lib/loguru/loguru.hpp"
#include "PAL.h"
#include "StackWalker.h"

using namespace LibProfiler;

namespace
{
	std::atomic s_seedlessSelfWalkBroken { false };

#ifndef _WIN32
	// DWARF register number on x86-64.
	constexpr int DWARF_RBP = 6;

	struct ManagedSeedState
	{
		ICorProfilerInfo10* CorProfilerInfo;
		CONTEXT* Seed;
		bool Found;
	};

	_Unwind_Reason_Code FindManagedSeedCallback(_Unwind_Context* ctx, void* arg)
	{
		auto* state = static_cast<ManagedSeedState*>(arg);
		const uintptr_t ip = _Unwind_GetIP(ctx);
		if (ip == 0)
			return _URC_END_OF_STACK;

		FunctionID functionId = 0;
		if (SUCCEEDED(state->CorProfilerInfo->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(ip), &functionId)) && functionId != 0)
		{
			// Topmost managed frame - build a seed context the CLR can walk from.
			memset(state->Seed, 0, sizeof(CONTEXT));
			state->Seed->ContextFlags = CONTEXT_CONTROL | CONTEXT_INTEGER;
			state->Seed->Rip = static_cast<DWORD64>(ip);
			state->Seed->Rsp = static_cast<DWORD64>(_Unwind_GetCFA(ctx));
			state->Seed->Rbp = static_cast<DWORD64>(_Unwind_GetGR(ctx, DWARF_RBP));
			state->Found = true;
			return _URC_END_OF_STACK;
		}

		return _URC_NO_REASON;
	}

	bool TryCaptureManagedSeed(ICorProfilerInfo10* corProfilerInfo, CONTEXT& seed)
	{
		ManagedSeedState state { corProfilerInfo, &seed, false };
		_Unwind_Backtrace(FindManagedSeedCallback, &state);
		return state.Found;
	}
#else
	bool TryCaptureManagedSeed(ICorProfilerInfo10* corProfilerInfo, CONTEXT& seed)
	{
		CONTEXT ctx;
		RtlCaptureContext(&ctx);

		constexpr int MaxNativeUnwindDepth = 64;
		for (int i = 0; i < MaxNativeUnwindDepth; ++i)
		{
			FunctionID functionId = 0;
			if (SUCCEEDED(corProfilerInfo->GetFunctionFromIP(reinterpret_cast<LPCBYTE>(ctx.Rip), &functionId)) &&
				functionId != 0)
			{
				// Managed frame found
				seed = ctx;
				return true;
			}

			DWORD64 imageBase;
			RUNTIME_FUNCTION* runtimeFunction = RtlLookupFunctionEntry(ctx.Rip, &imageBase, nullptr);
			if (runtimeFunction == nullptr)
			{
				// Leaf function
				ctx.Rip = *reinterpret_cast<DWORD64*>(ctx.Rsp);
				ctx.Rsp += sizeof(DWORD64);
			}
			else
			{
				VOID* handlerData;
				DWORD64 establisherFrame;
				RtlVirtualUnwind(UNW_FLAG_NHANDLER, imageBase, ctx.Rip, runtimeFunction,
					&ctx, &handlerData, &establisherFrame, nullptr);
			}

			if (ctx.Rip == 0)
				break;
		}

		return false;
	}
#endif
}

HRESULT StackWalker::CaptureStackTrace(
	ICorProfilerInfo10* corProfilerInfo,
	const ThreadID threadId,
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

	StackWalkContext context { };
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
	for (auto&& threadId : threadIds)
	{
		std::vector<UINT64> moduleIds;
		std::vector<UINT32> methodTokens;

		StackWalkContext context { };
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
			frames.emplace_back();
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

HRESULT StackWalker::CaptureCurrentStackTrace(
	ICorProfilerInfo10* corProfilerInfo,
	const ULONG skipFrames,
	const ULONG maxFrames,
	std::vector<BYTE>& framesBlob)
{
	if (corProfilerInfo == nullptr)
	{
		LOG_F(ERROR, "CorProfilerInfo is null.");
		return E_POINTER;
	}

	framesBlob.clear();

	CurrentStackWalkContext context { };
	context.CorProfilerInfo = corProfilerInfo;
	context.FramesBlob = &framesBlob;
	context.SkipFrames = skipFrames;
	context.MaxFrames = maxFrames;
	context.Seen = 0;
	context.Appended = 0;

	HRESULT hr = E_FAIL;
	bool attemptedSeedless = false;
	if (!s_seedlessSelfWalkBroken.load(std::memory_order_relaxed))
	{
		attemptedSeedless = true;
		hr = corProfilerInfo->DoStackSnapshot(
			NULL,
			CurrentStackSnapshotCallback,
			0,
			&context,
			nullptr,
			0);
		if (hr == CORPROF_E_STACKSNAPSHOT_ABORTED)
			hr = S_OK;
	}

	if (FAILED(hr))
	{
		CONTEXT seed;
		if (TryCaptureManagedSeed(corProfilerInfo, seed))
		{
			context.Seen = 0;
			context.Appended = 0;
			framesBlob.clear();
			hr = corProfilerInfo->DoStackSnapshot(
				NULL,
				CurrentStackSnapshotCallback,
				COR_PRF_SNAPSHOT_REGISTER_CONTEXT,
				&context,
				reinterpret_cast<BYTE*>(&seed),
				sizeof(seed));
			if (hr == CORPROF_E_STACKSNAPSHOT_ABORTED)
				hr = S_OK;
				
			if (attemptedSeedless && SUCCEEDED(hr))
				s_seedlessSelfWalkBroken.store(true, std::memory_order_relaxed);
		}
	}

	if (FAILED(hr))
	{
		// A failed walk must never fail the enter event - callers treat an empty blob as "no capture".
		framesBlob.clear();
		return hr;
	}

	return S_OK;
}

HRESULT STDMETHODCALLTYPE StackWalker::CurrentStackSnapshotCallback(
	const FunctionID funcId,
	UINT_PTR ip,
	COR_PRF_FRAME_INFO frameInfo,
	ULONG32 contextSize,
	BYTE context[],
	void* clientData)
{
	auto* walkContext = static_cast<CurrentStackWalkContext*>(clientData);
	if (funcId == 0)
		return S_OK;

	if (walkContext->Seen < walkContext->SkipFrames)
	{
		++walkContext->Seen;
		return S_OK;
	}

	if (walkContext->Appended >= walkContext->MaxFrames)
		return S_FALSE;

	ModuleID moduleId;
	mdMethodDef methodToken;
	HRESULT hr = walkContext->CorProfilerInfo->GetFunctionInfo(funcId, nullptr, &moduleId, &methodToken);
	if (SUCCEEDED(hr))
	{
		const auto moduleId64 = moduleId;
		const auto methodToken32 = methodToken;

		BYTE entry[sizeof(UINT64) + sizeof(UINT32)];
		std::memcpy(entry, &moduleId64, sizeof(UINT64));
		std::memcpy(entry + sizeof(UINT64), &methodToken32, sizeof(UINT32));
		walkContext->FramesBlob->insert(walkContext->FramesBlob->end(), entry, entry + sizeof(entry));

		++walkContext->Appended;
	}
	else
	{
		LOG_F(WARNING, "Failed to get function info for FunctionID %" UINT_PTR_FORMAT ". Error: 0x%x.", funcId, hr);
	}

	if (walkContext->Appended >= walkContext->MaxFrames)
		return S_FALSE;

	return S_OK;
}

HRESULT STDMETHODCALLTYPE StackWalker::StackSnapshotCallback(
	const FunctionID funcId,
	UINT_PTR ip,
	COR_PRF_FRAME_INFO frameInfo,
	ULONG32 contextSize,
	BYTE context[],
	void* clientData)
{
	const auto* walkContext = static_cast<StackWalkContext*>(clientData);

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

