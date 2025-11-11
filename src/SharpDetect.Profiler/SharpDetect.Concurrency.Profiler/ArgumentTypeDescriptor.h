// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <string>
#include <vector>

#include "cor.h"
#include "../lib/optional/include/tl/optional.hpp"
#include "../lib/json/single_include/nlohmann/json.hpp"

namespace Profiler
{
    struct ArgumentTypeDescriptor
    {
        std::vector<CorElementType> elementTypes;
        tl::optional<std::string> typeName;

        ArgumentTypeDescriptor()
            : elementTypes(), typeName("")
        {

        }

        explicit ArgumentTypeDescriptor(CorElementType type)
            : elementTypes({type}), typeName("")
        {

        }

        ArgumentTypeDescriptor(CorElementType type, const std::string& name)
            : elementTypes({type}), typeName(name)
        {

        }

        explicit ArgumentTypeDescriptor(std::vector<CorElementType> types, const std::string& name = "")
            : elementTypes(std::move(types)), typeName(name)
        {

        }
    };

    void to_json(nlohmann::json& json, const ArgumentTypeDescriptor& descriptor);
    void from_json(const nlohmann::json& json, ArgumentTypeDescriptor& descriptor);
}

