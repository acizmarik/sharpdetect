#include "ConfigurationAdditionalData.h"

void Profiler::to_json(nlohmann::json& json, const ConfigurationAdditionalData& descriptor)
{
	json["typesToIgnore"] = descriptor.typesToIgnore;
	json["methodsToInclude"] = descriptor.methodsToInclude;
}

void Profiler::from_json(const nlohmann::json& json, ConfigurationAdditionalData& descriptor)
{
	descriptor.typesToIgnore = json.at("typesToIgnore");
	descriptor.methodsToInclude = json.at("methodsToInclude");
}
