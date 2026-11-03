// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <cstddef>
#include <optional>
#include <vector>

#include "cor.h"
#include "corprof.h"

namespace LibProfiler
{
	// Pairs the exception callbacks of a single thread, so that a frame is reported exactly once it is left
	class ExceptionUnwindTracker
	{
	public:
		static constexpr size_t MaxPendingFrames = 1024;

		[[nodiscard]] std::optional<FunctionID> OnExceptionThrown();
		void OnSearchFilterEnter(FunctionID functionId);
		void OnSearchFilterLeave();
		void OnUnwindFunctionEnter(FunctionID functionId);
		void OnUnwindFinallyEnter(FunctionID functionId);
		void OnUnwindFinallyLeave();
		[[nodiscard]] std::optional<FunctionID> OnUnwindFunctionLeave();
		void OnCatcherEnter(FunctionID functionId);

		[[nodiscard]] size_t GetPendingFramesCount() const { return _frames.size(); }

	private:
		enum class Phase
		{
			Search,
			Unwind,
			UncaughtUnwind
		};

		struct PendingFrame
		{
			FunctionID functionId;
			bool runsFinally;
			bool uncaught;
		};

		struct RunningFilter
		{
			FunctionID functionId;
			size_t frameDepth;
		};

		[[nodiscard]] bool TopFrameIs(FunctionID functionId) const;
		[[nodiscard]] bool IsRunningFilterOf(FunctionID functionId) const;
		[[nodiscard]] std::optional<FunctionID> PopFrameLeftByUncaughtException();
		void AbandonFinallyOf(FunctionID functionId);
		void DropOldestFrame();

		std::vector<PendingFrame> _frames;
		std::vector<RunningFilter> _filters;
		Phase _phase = Phase::Unwind;
	};
}
