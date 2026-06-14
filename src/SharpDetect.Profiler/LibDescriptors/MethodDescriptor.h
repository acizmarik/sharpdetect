// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#pragma once

#include <optional>
#include <string>

#include "../lib/json/single_include/nlohmann/json.hpp"

#include "MethodVersionDescriptor.h"
#include "MethodSignatureDescriptor.h"
#include "MethodRewritingDescriptor.h"

namespace Profiler
{
    struct MethodDescriptor
    {
        std::string methodName;
        std::string declaringTypeFullName;
        std::optional<MethodVersionDescriptor> versionDescriptor;
        MethodSignatureDescriptor signatureDescriptor;
        MethodRewritingDescriptor rewritingDescriptor;
    };

    void to_json(nlohmann::json& json, const MethodDescriptor& descriptor);
    void from_json(const nlohmann::json& json, MethodDescriptor& descriptor);
}