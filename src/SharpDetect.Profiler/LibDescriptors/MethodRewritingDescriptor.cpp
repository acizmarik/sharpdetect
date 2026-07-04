// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodRewritingDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const MethodRewritingDescriptor& descriptor)
{
    json["injectHooks"] = descriptor.injectHooks;
    json["injectManagedWrapper"] = descriptor.injectManagedWrapper;
    json["emitExitEvent"] = descriptor.emitExitEvent;
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

    auto const emitExitEventIt = json.find("emitExitEvent");
    if (emitExitEventIt != json.cend() && !emitExitEventIt->is_null())
        descriptor.emitExitEvent = *emitExitEventIt;
    else
        descriptor.emitExitEvent = TRUE;
        
    auto const returnValueIt = json.find("returnValue");

    if (returnValueIt != json.cend() && !returnValueIt->is_null())
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
