// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

namespace Profiler
{
	struct MethodHookInfo
	{
		FunctionID FunctionId;
		bool Hook;
		bool GetInstance;
	};
}