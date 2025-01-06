// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using dnlib.DotNet;
using OperationResult;
using SharpDetect.Serialization;
using SharpDetect.Events;
using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Serialization;

public interface IArgumentsParser
{
    Result<RuntimeArgumentList, ArgumentParserErrorType> ParseArguments(
        MethodDef method,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> offsets);

    Result<RuntimeArgumentList, ArgumentParserErrorType> ParseArguments(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> offsets);
}
