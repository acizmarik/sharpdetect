// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <span>
#include <vector>

#include "cor.h"
#include "corprof.h"

#include "../LibProfilerCore/ObjectsTracker.h"
#include "../LibDescriptors/CapturedArgumentDescriptor.h"
#include "../LibDescriptors/CapturedValueDescriptor.h"
#include "../LibDescriptors/MethodDescriptor.h"

namespace Profiler
{
	class ArgumentCapture
	{
	public:
		ArgumentCapture(ICorProfilerInfo10*& corProfilerInfo, LibProfiler::ObjectsTracker& objectsTracker);

		HRESULT GetArguments(
			const MethodDescriptor& methodDescriptor,
			std::vector<UINT_PTR>& indirects,
			const COR_PRF_FUNCTION_ARGUMENT_INFO& argumentInfos,
			std::vector<BYTE>& argumentValues,
			std::vector<BYTE>& argumentOffsets);

		HRESULT GetByRefArguments(
			const MethodDescriptor& methodDescriptor,
			const std::vector<UINT_PTR>& indirects,
			std::span<BYTE> indirectValues,
			std::span<BYTE> indirectOffsets);

		HRESULT GetReturnValue(
			const CapturedValueDescriptor& value,
			COR_PRF_FUNCTION_ARGUMENT_RANGE range,
			const std::span<BYTE>& returnValue);

	private:
		HRESULT GetArgument(
			const CapturedArgumentDescriptor& argument,
			COR_PRF_FUNCTION_ARGUMENT_RANGE range,
			std::vector<UINT_PTR>& indirects,
			std::vector<BYTE>& argValues,
			std::vector<BYTE>& argOffsets);

		HRESULT GetReferenceArrayArgument(
			const CapturedArgumentDescriptor& argument,
			COR_PRF_FUNCTION_ARGUMENT_RANGE range,
			std::vector<BYTE>& argValues,
			std::vector<BYTE>& argOffsets);

		ICorProfilerInfo10*& _corProfilerInfo;
		LibProfiler::ObjectsTracker& _objectsTracker;
	};
}
