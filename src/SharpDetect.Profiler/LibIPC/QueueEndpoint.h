// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>

#include "cor.h"

namespace LibIPC
{
	struct QueueEndpoint
	{
		std::string name;
		std::string file;
		UINT size;
		std::string semaphoreName;
	};

	struct RegistrationEndpoint
	{
		std::string name;
		std::string file;
		UINT size;
	};
}
