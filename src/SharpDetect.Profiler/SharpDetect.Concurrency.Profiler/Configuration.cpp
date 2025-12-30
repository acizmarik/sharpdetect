// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "../LibProfiler/PAL.h"
#include "Configuration.h"

void Profiler::to_json(nlohmann::json& json, const Configuration& descriptor)
{
    json["eventMask"] = descriptor.eventMask;

    json["sharedMemoryName"] = descriptor.sharedMemoryName;
    if (descriptor.sharedMemoryFile.has_value())
        json["sharedMemoryFile"] = descriptor.sharedMemoryFile.value();
    json["sharedMemorySize"] = descriptor.sharedMemorySize;

    json["commandQueueName"] = descriptor.commandQueueName;
    if (descriptor.commandQueueFile.has_value())
        json["commandQueueFile"] = descriptor.commandQueueFile.value();
    json["commandQueueSize"] = descriptor.commandQueueSize;

    json["additionalData"]["methodDescriptors"] = descriptor.methodDescriptors;
    json["additionalData"]["typeInjectionDescriptors"] = descriptor.typeInjectionDescriptors;
    json["additionalData"]["enableFieldsAccessInstrumentation"] = descriptor.enableFieldsAccessInstrumentation;
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

	descriptor.commandQueueName = json.at("commandQueueName");
	descriptor.commandQueueName = descriptor.commandQueueName + "." + std::to_string(LibProfiler::PAL_GetCurrentPid());
    if (json.find("commandQueueFile") != json.cend())
    {
        auto& commandQueueFile = json.at("commandQueueFile");
        if (!commandQueueFile.is_null())
		{
            descriptor.commandQueueFile = commandQueueFile;
			descriptor.commandQueueFile = descriptor.commandQueueFile.value() + "." + std::to_string(LibProfiler::PAL_GetCurrentPid());
		}
    }
    descriptor.commandQueueSize = json.at("commandQueueSize");

    const auto& additionalData = json.at("additionalData");
    descriptor.methodDescriptors = additionalData.at("methodDescriptors").get<std::vector<MethodDescriptor>>();
    descriptor.typeInjectionDescriptors = additionalData.at("typeInjectionDescriptors").get<std::vector<TypeInjectionDescriptor>>();
    descriptor.enableFieldsAccessInstrumentation = additionalData.at("enableFieldsAccessInstrumentation");
}
