// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "cor.h"

#include "CapturedValueFlags.h"

namespace Profiler
{
	struct CapturedValueDescriptor
	{
		BYTE size;
		CapturedValueFlags flags;
	};

    inline void to_json(nlohmann::json& json, const CapturedValueDescriptor& descriptor)
    {
        json["size"] = descriptor.size;
        json["flags"] = descriptor.flags;
    }

    inline void from_json(const nlohmann::json& json, CapturedValueDescriptor& descriptor)
    {
        descriptor.size = json.at("size");
        descriptor.flags = json.at("flags");
    }
}