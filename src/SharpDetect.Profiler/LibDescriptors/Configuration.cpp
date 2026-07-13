// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "../LibProfilerCore/PAL.h"
#include "Configuration.h"

void Profiler::to_json(nlohmann::json& json, const Configuration& descriptor)
{
    json["eventMask"] = descriptor.eventMask;

    json["sharedMemoryName"] = descriptor.sharedMemoryName;
    if (descriptor.sharedMemoryFile.has_value())
        json["sharedMemoryFile"] = descriptor.sharedMemoryFile.value();
    json["sharedMemorySize"] = descriptor.sharedMemorySize;
    json["sharedMemorySemaphoreName"] = descriptor.sharedMemorySemaphoreName;

    json["commandQueueName"] = descriptor.commandQueueName;
    if (descriptor.commandQueueFile.has_value())
        json["commandQueueFile"] = descriptor.commandQueueFile.value();
    json["commandQueueSize"] = descriptor.commandQueueSize;
    json["commandSemaphoreName"] = descriptor.commandSemaphoreName;

    json["registrationQueueName"] = descriptor.registrationQueueName;
    if (descriptor.registrationQueueFile.has_value())
        json["registrationQueueFile"] = descriptor.registrationQueueFile.value();
    json["registrationQueueSize"] = descriptor.registrationQueueSize;

    json["additionalData"]["methodDescriptors"] = descriptor.methodDescriptors;
    json["additionalData"]["typeInjectionDescriptors"] = descriptor.typeInjectionDescriptors;
    json["additionalData"]["enableFieldsAccessInstrumentation"] = descriptor.enableFieldsAccessInstrumentation;
    json["additionalData"]["skipInstrumentationForAssemblies"] = descriptor.skipInstrumentationForAssemblies;
    json["additionalData"]["enableStackTraceCollection"] = descriptor.enableStackTraceCollection;
    json["additionalData"]["stackTraceCollectionMaxDepth"] = descriptor.stackTraceCollectionMaxDepth;
    json["additionalData"]["stackTraceCollectionForFields"] = descriptor.stackTraceCollectionForFields;
}

void Profiler::from_json(const nlohmann::json& json, Configuration& descriptor)
{
    descriptor.eventMask = json.at("eventMask");

    const auto pidSuffix = "." + std::to_string(LibProfiler::PAL_GetCurrentPid());

    descriptor.sharedMemoryName = json.at("sharedMemoryName");
    descriptor.sharedMemoryName = descriptor.sharedMemoryName + pidSuffix;
    if (json.find("sharedMemoryFile") != json.cend())
    {
        auto& sharedMemoryFile = json.at("sharedMemoryFile");
        if (!sharedMemoryFile.is_null())
            descriptor.sharedMemoryFile = sharedMemoryFile.get<std::string>() + pidSuffix;
    }
    descriptor.sharedMemorySize = json.at("sharedMemorySize");
    descriptor.sharedMemorySemaphoreName = json.at("sharedMemorySemaphoreName");
    descriptor.sharedMemorySemaphoreName = descriptor.sharedMemorySemaphoreName + pidSuffix;

	descriptor.commandQueueName = json.at("commandQueueName");
	descriptor.commandQueueName = descriptor.commandQueueName + pidSuffix;
    if (json.find("commandQueueFile") != json.cend())
    {
        auto& commandQueueFile = json.at("commandQueueFile");
        if (!commandQueueFile.is_null())
		{
            descriptor.commandQueueFile = commandQueueFile;
			descriptor.commandQueueFile = descriptor.commandQueueFile.value() + pidSuffix;
		}
    }
    descriptor.commandQueueSize = json.at("commandQueueSize");
    descriptor.commandSemaphoreName = json.at("commandSemaphoreName");
    descriptor.commandSemaphoreName = descriptor.commandSemaphoreName + pidSuffix;

    // The registration table is shared by all profiled processes (no PID suffix)
    descriptor.registrationQueueName = json.at("registrationQueueName");
    if (json.find("registrationQueueFile") != json.cend())
    {
        auto& registrationQueueFile = json.at("registrationQueueFile");
        if (!registrationQueueFile.is_null())
            descriptor.registrationQueueFile = registrationQueueFile;
    }
    descriptor.registrationQueueSize = json.at("registrationQueueSize");

    const auto& additionalData = json.at("additionalData");
    descriptor.methodDescriptors = additionalData.at("methodDescriptors").get<std::vector<MethodDescriptor>>();
    descriptor.typeInjectionDescriptors = additionalData.at("typeInjectionDescriptors").get<std::vector<TypeInjectionDescriptor>>();
    descriptor.enableFieldsAccessInstrumentation = additionalData.at("enableFieldsAccessInstrumentation");
    if (additionalData.contains("skipInstrumentationForAssemblies"))
        descriptor.skipInstrumentationForAssemblies = additionalData.at("skipInstrumentationForAssemblies").get<std::vector<std::string>>();
    if (additionalData.contains("enableStackTraceCollection"))
        descriptor.enableStackTraceCollection = additionalData.at("enableStackTraceCollection");
    if (additionalData.contains("stackTraceCollectionMaxDepth"))
        descriptor.stackTraceCollectionMaxDepth = additionalData.at("stackTraceCollectionMaxDepth");
    if (additionalData.contains("stackTraceCollectionForFields"))
        descriptor.stackTraceCollectionForFields = additionalData.at("stackTraceCollectionForFields").get<std::vector<std::string>>();
}
