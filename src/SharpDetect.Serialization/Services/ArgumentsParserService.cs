// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using Microsoft.Extensions.Logging;
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

        try
        {
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
        }
        catch
        {
            _arrayPool.Return(arguments);
            throw;
        }

        return RuntimeArgumentList.Rent(arguments, argInfos.Length);
    }

    private static ArgumentValue ParseArgumentValue(TypeSig parameter, ReadOnlySpan<byte> value)
    {
        // If it is indirect, profiler already resolved the actual value
        var paramSig = parameter.IsByRef ? parameter.Next : parameter;

        // Handle SZArray of reference types (e.g., Task[])
        if (paramSig.IsSZArray)
        {
            return ArgumentValue.TrackedArray(ParseReferenceArrayValue(value));
        }

        // If its not a value type then just load the reference
        if (!paramSig.IsValueType)
        {
            // If pointer == 0 it is a null
            // Otherwise, make sure we do not create new instances for identical pointers
            var pointer = new nuint(MemoryMarshal.Read<ulong>(value));
            return ArgumentValue.Tracked(new TrackedObjectId(pointer));
        }

        // Otherwise try parse value type
        return paramSig.ElementType switch
        {
            ElementType.Boolean => ArgumentValue.Primitive(MemoryMarshal.Read<bool>(value) ? 1UL : 0UL, PrimitiveKind.Boolean),
            ElementType.Char => ArgumentValue.Primitive((ulong)MemoryMarshal.Read<char>(value), PrimitiveKind.Char),
            ElementType.I1 => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<sbyte>(value)), PrimitiveKind.I1),
            ElementType.U1 => ArgumentValue.Primitive(MemoryMarshal.Read<byte>(value), PrimitiveKind.U1),
            ElementType.I2 => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<short>(value)), PrimitiveKind.I2),
            ElementType.U2 => ArgumentValue.Primitive(MemoryMarshal.Read<ushort>(value), PrimitiveKind.U2),
            ElementType.I4 => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<int>(value)), PrimitiveKind.I4),
            ElementType.U4 => ArgumentValue.Primitive(MemoryMarshal.Read<uint>(value), PrimitiveKind.U4),
            ElementType.I8 => ArgumentValue.Primitive(unchecked((ulong)MemoryMarshal.Read<long>(value)), PrimitiveKind.I8),
            ElementType.U8 => ArgumentValue.Primitive(MemoryMarshal.Read<ulong>(value), PrimitiveKind.U8),
            ElementType.R4 => ArgumentValue.Primitive(BitConverter.SingleToUInt32Bits(MemoryMarshal.Read<float>(value)), PrimitiveKind.R4),
            ElementType.R8 => ArgumentValue.Primitive(BitConverter.DoubleToUInt64Bits(MemoryMarshal.Read<double>(value)), PrimitiveKind.R8),
            ElementType.I => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<nint>(value)), PrimitiveKind.I),
            ElementType.U => ArgumentValue.Primitive(MemoryMarshal.Read<nuint>(value), PrimitiveKind.U),
            _ => throw new NotSupportedException($"Could not parse parameter {paramSig}."),
        };
    }

    private static TrackedObjectId[] ParseReferenceArrayValue(ReadOnlySpan<byte> value)
    {
        // Format: [4-byte count][N × sizeof(ulong) tracked object IDs]
        var count = MemoryMarshal.Read<uint>(value);
        var result = new TrackedObjectId[count];
        var offset = sizeof(uint);

        for (var i = 0; i < count; i++)
        {
            var trackedId = new nuint(MemoryMarshal.Read<ulong>(value.Slice(offset, sizeof(ulong))));
            result[i] = new TrackedObjectId(trackedId);
            offset += sizeof(ulong);
        }

        return result;
    }
}
