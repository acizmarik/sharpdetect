// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const MethodDescriptor& descriptor)
{
    json["methodName"] = descriptor.methodName;
    json["declaringTypeFullName"] = descriptor.declaringTypeFullName;
    json["signatureDescriptor"] = descriptor.signatureDescriptor;
    json["rewritingDescriptor"] = descriptor.rewritingDescriptor;
}

void Profiler::from_json(const nlohmann::json& json, MethodDescriptor& descriptor)
{
    descriptor.methodName = json.at("methodName");
    descriptor.declaringTypeFullName = json.at("declaringTypeFullName");
    descriptor.signatureDescriptor = json.at("signatureDescriptor");
    descriptor.rewritingDescriptor = json.at("rewritingDescriptor");
}
