// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "ArgumentTypeDescriptor.h"

void Profiler::to_json(nlohmann::json& json, const ArgumentTypeDescriptor& descriptor)
{
    json["elementTypes"] = descriptor.elementTypes;
    
    if (!descriptor.typeName.has_value())
        json["typeName"] = descriptor.typeName.value();
}

void Profiler::from_json(const nlohmann::json& json, ArgumentTypeDescriptor& descriptor)
{
    descriptor.elementTypes = json.at("elementTypes").get<std::vector<CorElementType>>();

	auto const typeNameIt = json.find("typeName");

	if (typeNameIt != json.cend() && !(*typeNameIt).is_null())
		descriptor.typeName = json.at("typeName");
}