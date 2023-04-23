using dnlib.DotNet;
using SharpDetect.Common.Runtime.Arguments;
using SharpDetect.Core.Runtime.Arguments;
using System.Runtime.InteropServices;

namespace SharpDetect.Core.Utilities
{
    internal class ArgumentsHelper
    {
        internal static (ushort Index, IValueOrPointer Argument)[] ParseArguments(MethodDef method, ReadOnlySpan<byte> values, ReadOnlySpan<byte> offsets)
        {
            var pValues = 0;
            var argInfos = MemoryMarshal.Cast<byte, uint>(offsets);
            // TODO: this should be pooled
            var arguments = new (ushort, IValueOrPointer)[argInfos.Length];

            // Read information about arguments and parse them
            for (var i = 0; i < argInfos.Length; i++)
            {
                // Argument index is stored in upper 16 bits
                var index = (ushort)((argInfos[i] & 0xFFFF0000) >> 16);
                // Argument size is stored in the lower 16 bits
                var size = (ushort)(argInfos[i] & 0x0000FFFF);

                // Retrieve argument
                var argument = ParseArgument(method.Parameters[index].Type, values.Slice(pValues, size));
                arguments[i] = (index, argument);

                // Move values pointer
                pValues += size;
            }

            return arguments;
        }

        internal static ValueOrPointer ParseArgument(TypeSig parameter, ReadOnlySpan<byte> value)
        {
            // If it is indirect, profiler already resolved the actual value
            var paramSig = (parameter.IsByRef) ? parameter.Next : parameter;

            // If its not a value type then just load the reference
            if (!paramSig.IsValueType)
            {
                var pointer = new UIntPtr(MemoryMarshal.Read<ulong>(value));
                // If pointer == 0 it is a null
                if (pointer == UIntPtr.Zero)
                    return new ValueOrPointer(UIntPtr.Zero);

                // Make sure we do not create new instances for identical pointers
                return new ValueOrPointer(pointer);
            }

            // Otherwise try parse value type
            return paramSig.ElementType switch
            {
                ElementType.Boolean => new(MemoryMarshal.Read<bool>(value)),
                ElementType.Char => new(MemoryMarshal.Read<char>(value)),
                ElementType.I1 => new(MemoryMarshal.Read<sbyte>(value)),
                ElementType.U1 => new(MemoryMarshal.Read<byte>(value)),
                ElementType.I2 => new(MemoryMarshal.Read<short>(value)),
                ElementType.U2 => new(MemoryMarshal.Read<ushort>(value)),
                ElementType.I4 => new(MemoryMarshal.Read<int>(value)),
                ElementType.U4 => new(MemoryMarshal.Read<uint>(value)),
                ElementType.I8 => new(MemoryMarshal.Read<long>(value)),
                ElementType.U8 => new(MemoryMarshal.Read<ulong>(value)),
                ElementType.R4 => new(MemoryMarshal.Read<float>(value)),
                ElementType.R8 => new(MemoryMarshal.Read<double>(value)),
                ElementType.I => new(MemoryMarshal.Read<IntPtr>(value)),
                ElementType.U => new(MemoryMarshal.Read<UIntPtr>(value)),
                _ => throw new NotSupportedException($"Could not parse parameter {paramSig}."),
            };
        }
    }
}
