// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "Configuration.h"

void Profiler::to_json(nlohmann::json& json, const Configuration& descriptor)
{
    json["eventMask"] = descriptor.eventMask;
    json["sharedMemoryName"] = descriptor.sharedMemoryName;
    if (descriptor.sharedMemoryFile.has_value())
        json["sharedMemoryFile"] = descriptor.sharedMemoryFile.value();
    json["sharedMemorySize"] = descriptor.sharedMemorySize;
    json["additionalData"] = descriptor.additionalData;
}

void Profiler::from_json(const nlohmann::json& json, Configuration& descriptor)
{
    descriptor.eventMask = json.at("eventMask");
    descriptor.sharedMemoryName = json.at("sharedMemoryName");
    if (json.find("sharedMemoryFile") != json.cend())
    {
        auto& sharedMemoryFile = json.at("sharedMemoryFile");
        if (!sharedMemoryFile.is_null())
            descriptor.sharedMemoryFile = sharedMemoryFile;
    }
    descriptor.sharedMemorySize = json.at("sharedMemorySize");
    descriptor.additionalData = json.at("additionalData").template get<std::vector<MethodDescriptor>>();;
}
