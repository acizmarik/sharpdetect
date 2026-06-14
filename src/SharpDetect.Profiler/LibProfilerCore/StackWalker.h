// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <vector>
#include "corprof.h"

namespace LibProfiler
{
	struct StackFrame
	{
		UINT64 ModuleId;
		UINT32 MethodToken;
	};

	class StackWalker
	{
	public:
		static HRESULT CaptureStackTrace(
			ICorProfilerInfo10* corProfilerInfo,
			ThreadID threadId,
			std::vector<UINT64>& moduleIds,
			std::vector<UINT32>& methodTokens);

		static HRESULT CaptureStackTraces(
			ICorProfilerInfo10* corProfilerInfo,
			const std::vector<UINT64>& threadIds,
			std::vector<std::vector<StackFrame>>& frames);

	private:
		struct StackWalkContext
		{
			ICorProfilerInfo10* CorProfilerInfo;
			std::vector<UINT64>* ModuleIds;
			std::vector<UINT32>* MethodTokens;
		};

		static HRESULT STDMETHODCALLTYPE StackSnapshotCallback(
			FunctionID funcId,
			UINT_PTR ip,
			COR_PRF_FRAME_INFO frameInfo,
			ULONG32 contextSize,
			BYTE context[],
			void* clientData);
	};
}

