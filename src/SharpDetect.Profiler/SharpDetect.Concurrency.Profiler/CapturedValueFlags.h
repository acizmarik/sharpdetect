// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

namespace Profiler
{
	enum class CapturedValueFlags
	{ 
		None = 0, 
		CaptureAsValue = 1, 
		CaptureAsReference = 2, 
		IndirectLoad = 4
	};
}