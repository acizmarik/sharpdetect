// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "MethodSignatureDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const MethodSignatureDescriptor& descriptor)
{
    json["callingConvention"] = descriptor.callingConvention;
    json["parametersCount"] = descriptor.parametersCount;
    json["genericParametersCount"] = descriptor.genericParametersCount;
    json["returnType"] = descriptor.returnType;
    json["argumentTypeElements"] = descriptor.argumentTypeElements;
}

void Profiler::from_json(const nlohmann::json& json, MethodSignatureDescriptor& descriptor)
{
    descriptor.callingConvention = json.at("callingConvention");
    descriptor.parametersCount = json.at("parametersCount");

    auto const genericParametersCountIt = json.find("genericParametersCount");
    descriptor.genericParametersCount = (genericParametersCountIt != json.cend() && !genericParametersCountIt->is_null())
        ? genericParametersCountIt->get<BYTE>()
        : static_cast<BYTE>(0);
    descriptor.returnType = json.at("returnType").get<ArgumentTypeDescriptor>();
    descriptor.argumentTypeElements = json.at("argumentTypeElements").get<std::vector<ArgumentTypeDescriptor>>();
}
