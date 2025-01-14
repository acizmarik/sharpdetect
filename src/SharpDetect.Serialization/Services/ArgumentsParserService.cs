// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using OneOf;
using OperationResult;
using SharpDetect.Core.Events;
using SharpDetect.Core.Events.Profiler;
using SharpDetect.Core.Metadata;
using SharpDetect.Core.Serialization;
using System.Buffers;
using System.Runtime.InteropServices;
using static OperationResult.Helpers;

namespace SharpDetect.Serialization.Services;

internal sealed class ArgumentsParserService : IArgumentsParser
{
    private readonly IMetadataContext _metadataContext;
    private readonly ILogger<ArgumentsParserService> _logger;
    private static readonly ArrayPool<RuntimeArgumentInfo> _arrayPool = ArrayPool<RuntimeArgumentInfo>.Shared;

    public ArgumentsParserService(IMetadataContext metadataContext, ILogger<ArgumentsParserService> logger)
    {
        _metadataContext = metadataContext;
        _logger = logger;
    }

    public Result<RuntimeArgumentList, ArgumentParserErrorType> ParseArguments(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> offsets)
    {
        var resolveMethodResult = _metadataContext
            .GetResolver(metadata.Pid)
            .ResolveMethod(metadata, moduleId, methodToken);

        if (resolveMethodResult.IsError)
        {
            _logger.LogError("Failed to resolve method definition");
            return Error(ArgumentParserErrorType.UnresolvableMethodToken);
        }

        var methodDef = resolveMethodResult.Value;
        return ParseArguments(methodDef, values, offsets);
    }

    public Result<RuntimeArgumentList, ArgumentParserErrorType> ParseArguments(
        MethodDef method,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> offsets)
    {
        var pValues = 0;
        var argInfos = MemoryMarshal.Cast<byte, uint>(offsets);
        var arguments = _arrayPool.Rent(argInfos.Length);

        // Read information about arguments and parse them
        for (var i = 0; i < argInfos.Length; i++)
        {
            // Argument index is stored in upper 16 bits
            var index = (ushort)((argInfos[i] & 0xFFFF0000) >> 16);
            // Argument size is stored in the lower 16 bits
            var size = (ushort)(argInfos[i] & 0x0000FFFF);

            // Retrieve argument
            var argument = ParseArgumentValue(method.Parameters[index].Type, values.Slice(pValues, size));
            arguments[i] = new(index, argument);

            // Move values pointer
            pValues += size;
        }

        return new RuntimeArgumentList(arguments, argInfos.Length);
    }

    private static OneOf<object, TrackedObjectId> ParseArgumentValue(TypeSig parameter, ReadOnlySpan<byte> value)
    {
        // If it is indirect, profiler already resolved the actual value
        var paramSig = parameter.IsByRef ? parameter.Next : parameter;

        // If its not a value type then just load the reference
        if (!paramSig.IsValueType)
        {
            var pointer = new nuint(MemoryMarshal.Read<ulong>(value));
            // If pointer == 0 it is a null
            if (pointer == nuint.Zero)
                return new TrackedObjectId(0);

            // Make sure we do not create new instances for identical pointers
            return new TrackedObjectId(pointer);
        }

        // Otherwise try parse value type
        return paramSig.ElementType switch
        {
            ElementType.Boolean => MemoryMarshal.Read<bool>(value),
            ElementType.Char => MemoryMarshal.Read<char>(value),
            ElementType.I1 => MemoryMarshal.Read<sbyte>(value),
            ElementType.U1 => MemoryMarshal.Read<byte>(value),
            ElementType.I2 => MemoryMarshal.Read<short>(value),
            ElementType.U2 => MemoryMarshal.Read<ushort>(value),
            ElementType.I4 => MemoryMarshal.Read<int>(value),
            ElementType.U4 => MemoryMarshal.Read<uint>(value),
            ElementType.I8 => MemoryMarshal.Read<long>(value),
            ElementType.U8 => MemoryMarshal.Read<ulong>(value),
            ElementType.R4 => MemoryMarshal.Read<float>(value),
            ElementType.R8 => MemoryMarshal.Read<double>(value),
            ElementType.I => MemoryMarshal.Read<nint>(value),
            ElementType.U => MemoryMarshal.Read<nuint>(value),
            _ => throw new NotSupportedException($"Could not parse parameter {paramSig}."),
        };
    }
}
