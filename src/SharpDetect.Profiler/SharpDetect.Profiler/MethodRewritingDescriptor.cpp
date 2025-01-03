// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodRewritingDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const MethodRewritingDescriptor& descriptor)
{
    json["injectHooks"] = descriptor.injectHooks;
    json["injectManagedWrapper"] = descriptor.injectManagedWrapper;
    json["arguments"] = descriptor.arguments;
    if (descriptor.returnValue.has_value())
        json["returnValue"] = descriptor.returnValue.value();
    if (descriptor.methodEnterInterpretation.has_value())
        json["methodEnterInterpretation"] = descriptor.methodEnterInterpretation.value();
    if (descriptor.methodExitInterpretation.has_value())
        json["methodExitInterpretation"] = descriptor.methodExitInterpretation.value();
}

void Profiler::from_json(const nlohmann::json& json, MethodRewritingDescriptor& descriptor)
{
    descriptor.injectHooks = json.at("injectHooks");
    descriptor.injectManagedWrapper = json.at("injectManagedWrapper");
    descriptor.arguments = json.at("arguments");
    auto const returnValueIt = json.find("returnValue");

    if (returnValueIt != json.cend() && !(*returnValueIt).is_null())
        descriptor.returnValue = json.at("returnValue");

    if (json.find("methodEnterInterpretation") != json.cend())
    {
        auto& enterInterpretation = json.at("methodEnterInterpretation");
        if (!enterInterpretation.is_null())
            descriptor.methodEnterInterpretation = enterInterpretation;
    }
        
    if (json.find("methodExitInterpretation") != json.cend())
    {
        auto& exitInterpretation = json.at("methodExitInterpretation");
        if (!exitInterpretation.is_null())
            descriptor.methodExitInterpretation = exitInterpretation;
    }
}
