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

internal sealed class ArgumentsParserService(IMetadataContext metadataContext, ILogger<ArgumentsParserService> logger)
    : IArgumentsParser
{
    private static readonly ArrayPool<RuntimeArgumentInfo> ArrayPool = ArrayPool<RuntimeArgumentInfo>.Shared;
    private readonly Dictionary<MethodDef, ParameterKind[]> _parameterKinds = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<(uint Pid, ModuleId ModuleId, MdMethodDef MethodToken), (MethodDef Method, ParameterKind[] ParameterKinds)> _resolvedMethods = [];

    public Result<RuntimeArgumentList, ArgumentParserErrorType> ParseArguments(
        RecordedEventMetadata metadata,
        ModuleId moduleId,
        MdMethodDef methodToken,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> offsets)
    {
        var key = (metadata.Pid, moduleId, methodToken);
        if (!_resolvedMethods.TryGetValue(key, out var resolved))
        {
            var resolveMethodResult = metadataContext
                .GetResolver(metadata.Pid)
                .ResolveMethod(metadata, moduleId, methodToken);

            if (resolveMethodResult.IsError)
            {
                logger.LogError("Failed to resolve method definition");
                return Error(ArgumentParserErrorType.UnresolvableMethodToken);
            }

            var methodDef = resolveMethodResult.Value;
            resolved = (methodDef, GetParameterKinds(methodDef));
            _resolvedMethods.Add(key, resolved);
        }

        return ParseArguments(resolved.Method, resolved.ParameterKinds, values, offsets);
    }

    public Result<RuntimeArgumentList, ArgumentParserErrorType> ParseArguments(
        MethodDef method,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> offsets)
    {
        return ParseArguments(method, GetParameterKinds(method), values, offsets);
    }

    private static Result<RuntimeArgumentList, ArgumentParserErrorType> ParseArguments(
        MethodDef method,
        ParameterKind[] parameterKinds,
        ReadOnlySpan<byte> values,
        ReadOnlySpan<byte> offsets)
    {
        var pValues = 0;
        var argInfos = MemoryMarshal.Cast<byte, uint>(offsets);
        var arguments = ArrayPool.Rent(argInfos.Length);

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
                var argument = ParseArgumentValue(method, index, parameterKinds[index], values.Slice(pValues, size));
                arguments[i] = new(index, argument);

                // Move values pointer
                pValues += size;
            }
        }
        catch
        {
            ArrayPool.Return(arguments);
            throw;
        }

        return RuntimeArgumentList.Rent(arguments, argInfos.Length);
    }

    private ParameterKind[] GetParameterKinds(MethodDef method)
    {
        if (_parameterKinds.TryGetValue(method, out var kinds))
            return kinds;

        var parameters = method.Parameters;
        kinds = new ParameterKind[parameters.Count];
        for (var i = 0; i < kinds.Length; i++)
            kinds[i] = ClassifyParameter(parameters[i].Type);

        _parameterKinds.Add(method, kinds);
        return kinds;
    }

    private static ParameterKind ClassifyParameter(TypeSig parameter)
    {
        // Descriptors must dereference every captured by-ref and pointer, so the profiler sends their targets
        var paramSig = parameter.IsByRef || parameter.IsPointer ? parameter.Next : parameter;

        // Handle SZArray of reference types (e.g., Task[])
        if (paramSig.IsSZArray)
            return ParameterKind.ReferenceArray;

        // If its not a value type then just load the reference
        if (!paramSig.IsValueType)
            return ParameterKind.Reference;

        return paramSig.ElementType switch
        {
            ElementType.Boolean => ParameterKind.Boolean,
            ElementType.Char => ParameterKind.Char,
            ElementType.I1 => ParameterKind.I1,
            ElementType.U1 => ParameterKind.U1,
            ElementType.I2 => ParameterKind.I2,
            ElementType.U2 => ParameterKind.U2,
            ElementType.I4 => ParameterKind.I4,
            ElementType.U4 => ParameterKind.U4,
            ElementType.I8 => ParameterKind.I8,
            ElementType.U8 => ParameterKind.U8,
            ElementType.R4 => ParameterKind.R4,
            ElementType.R8 => ParameterKind.R8,
            ElementType.I => ParameterKind.I,
            ElementType.U => ParameterKind.U,
            _ => ParameterKind.Unsupported,
        };
    }

    private static ArgumentValue ParseArgumentValue(MethodDef method, int index, ParameterKind kind, ReadOnlySpan<byte> value)
    {
        return kind switch
        {
            ParameterKind.ReferenceArray => ArgumentValue.TrackedArray(ParseReferenceArrayValue(value)),
            // If pointer == 0 it is a null
            // Otherwise, make sure we do not create new instances for identical pointers
            ParameterKind.Reference => ArgumentValue.Tracked(new TrackedObjectId(new nuint(MemoryMarshal.Read<ulong>(value)))),
            ParameterKind.Boolean => ArgumentValue.Primitive(MemoryMarshal.Read<bool>(value) ? 1UL : 0UL, PrimitiveKind.Boolean),
            ParameterKind.Char => ArgumentValue.Primitive((ulong)MemoryMarshal.Read<char>(value), PrimitiveKind.Char),
            ParameterKind.I1 => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<sbyte>(value)), PrimitiveKind.I1),
            ParameterKind.U1 => ArgumentValue.Primitive(MemoryMarshal.Read<byte>(value), PrimitiveKind.U1),
            ParameterKind.I2 => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<short>(value)), PrimitiveKind.I2),
            ParameterKind.U2 => ArgumentValue.Primitive(MemoryMarshal.Read<ushort>(value), PrimitiveKind.U2),
            ParameterKind.I4 => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<int>(value)), PrimitiveKind.I4),
            ParameterKind.U4 => ArgumentValue.Primitive(MemoryMarshal.Read<uint>(value), PrimitiveKind.U4),
            ParameterKind.I8 => ArgumentValue.Primitive(unchecked((ulong)MemoryMarshal.Read<long>(value)), PrimitiveKind.I8),
            ParameterKind.U8 => ArgumentValue.Primitive(MemoryMarshal.Read<ulong>(value), PrimitiveKind.U8),
            ParameterKind.R4 => ArgumentValue.Primitive(BitConverter.SingleToUInt32Bits(MemoryMarshal.Read<float>(value)), PrimitiveKind.R4),
            ParameterKind.R8 => ArgumentValue.Primitive(BitConverter.DoubleToUInt64Bits(MemoryMarshal.Read<double>(value)), PrimitiveKind.R8),
            ParameterKind.I => ArgumentValue.Primitive(unchecked((ulong)(long)MemoryMarshal.Read<nint>(value)), PrimitiveKind.I),
            ParameterKind.U => ArgumentValue.Primitive(MemoryMarshal.Read<nuint>(value), PrimitiveKind.U),
            _ => throw new NotSupportedException($"Could not parse parameter {method.Parameters[index].Type}."),
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
