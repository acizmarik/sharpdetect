// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>
#include <string>
#include <vector>

#include "cor.h"
#include "../lib/json/single_include/nlohmann/json.hpp"

namespace Profiler
{
    struct ArgumentTypeDescriptor
    {
        std::vector<CorElementType> elementTypes;
        std::optional<std::string> typeName;

        ArgumentTypeDescriptor()
            : typeName("")
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

