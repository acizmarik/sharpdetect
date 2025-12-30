// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;
using SharpDetect.E2ETests.Utils;
using SharpDetect.TemporalAsserts.TemporalOperators;

namespace SharpDetect.E2ETests;

public static class TemporalAssertionBuilders
{
    public static EventuallyOperator<ulong, RecordedEventType> EventuallyEventType(RecordedEventType type)
    {
        return new(EventMatchers.EventType(type));
    }

    public static EventuallyOperator<ulong, RecordedEventType> EventuallyMethodEnter(
        string methodName,
        TestHappensBeforePlugin plugin)
    {
        return new(EventMatchers.MethodEnter(methodName, plugin));
    }

    public static EventuallyOperator<ulong, RecordedEventType> EventuallyMethodExit(
        string methodName,
        TestHappensBeforePlugin plugin)
    {
        return new(EventMatchers.MethodExit(methodName, plugin));
    }
}

