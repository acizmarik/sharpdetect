// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "cor.h"

#include "CapturedValueDescriptor.h"

namespace Profiler
{
	struct CapturedArgumentDescriptor
	{
		BYTE index;
		CapturedValueDescriptor value;
	};

    inline void to_json(nlohmann::json& json, const CapturedArgumentDescriptor& descriptor)
    {
        json["index"] = descriptor.index;
        json["value"] = descriptor.value;
    }

    inline void from_json(const nlohmann::json& json, CapturedArgumentDescriptor& descriptor)
    {
        descriptor.index = json.at("index");
        descriptor.value = json.at("value");
    }
}