// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

#include "doctest.h"
#include "MethodRewritingDescriptor.h"

namespace
{
    Profiler::MethodRewritingDescriptor Parse(const std::string& json)
    {
        return nlohmann::json::parse(json).get<Profiler::MethodRewritingDescriptor>();
    }
}

TEST_CASE("MethodRewritingDescriptor parses captured arguments")
{
    auto const descriptor = Parse(
    R"({
        "injectHooks": true,
        "injectManagedWrapper": false,
        "arguments": [ { "index": 1, "value": { "size": 8, "flags": 2 } } ]
    })");

    REQUIRE(descriptor.arguments.size() == 1);
    CHECK(descriptor.arguments[0].index == 1);
    CHECK(descriptor.arguments[0].value.size == 8);
}

TEST_CASE("MethodRewritingDescriptor tolerates a null arguments field")
{
    auto const descriptor = Parse(
    R"({
        "injectHooks": true,
        "injectManagedWrapper": false,
        "arguments": null
    })");

    CHECK(descriptor.arguments.empty());
}

TEST_CASE("MethodRewritingDescriptor tolerates a missing arguments field")
{
    auto const descriptor = Parse(
    R"({
        "injectHooks": true,
        "injectManagedWrapper": false
    })");

    CHECK(descriptor.arguments.empty());
}

TEST_CASE("MethodRewritingDescriptor defaults optional fields")
{
    auto const descriptor = Parse(
    R"({
        "injectHooks": true,
        "injectManagedWrapper": false,
        "arguments": []
    })");

    CHECK(descriptor.emitExitEvent == TRUE);
    CHECK(descriptor.captureStackTraceOnEnter == FALSE);
    CHECK_FALSE(descriptor.returnValue.has_value());
    CHECK_FALSE(descriptor.methodEnterInterpretation.has_value());
    CHECK_FALSE(descriptor.methodExitInterpretation.has_value());
}
