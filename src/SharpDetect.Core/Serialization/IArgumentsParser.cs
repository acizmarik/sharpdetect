// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using OperationResult;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Serialization;

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
