// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodSignatureDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const MethodSignatureDescriptor& descriptor)
{
    json["callingConvention"] = descriptor.callingConvention;
    json["parametersCount"] = descriptor.parametersCount;
    json["returnType"] = descriptor.returnType;
    json["argumentTypeElements"] = descriptor.argumentTypeElements;
}

void Profiler::from_json(const nlohmann::json& json, MethodSignatureDescriptor& descriptor)
{
    descriptor.callingConvention = json.at("callingConvention");
    descriptor.parametersCount = json.at("parametersCount");
    descriptor.returnType = json.at("returnType").get<ArgumentTypeDescriptor>();
    descriptor.argumentTypeElements = json.at("argumentTypeElements").get<std::vector<ArgumentTypeDescriptor>>();
}
