using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SharpDetect.Dnlib.Extensions.Utilities
{
    public static class ByteArrayHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(this byte[] array, ref int position, byte value)
        {
            array[position++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(this byte[] array, ref int position, sbyte value)
        {
            array.WriteByte(ref position, (byte)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt16(this byte[] array, ref int position, ushort value)
        {
            array[position++] = (byte)value;
            array[position++] = (byte)(value >> 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt16(this byte[] array, ref int position, short value)
        {
            array.WriteUInt16(ref position, (ushort)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt32(this byte[] array, ref int position, uint value)
        {
            array[position++] = (byte)value;
            array[position++] = (byte)(value >> 8);
            array[position++] = (byte)(value >> 16);
            array[position++] = (byte)(value >> 24);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt32(this byte[] array, ref int position, int value)
        {
            array.WriteUInt32(ref position, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt64(this byte[] array, ref int position, long value)
        {
            array.WriteUInt64(ref position, (uint)value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt64(this byte[] array, ref int position, ulong value)
        {
            array[position++] = (byte)value;
            array[position++] = (byte)(value >> 8);
            array[position++] = (byte)(value >> 16);
            array[position++] = (byte)(value >> 24);
            array[position++] = (byte)(value >> 32);
            array[position++] = (byte)(value >> 40);
            array[position++] = (byte)(value >> 48);
            array[position++] = (byte)(value >> 56);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSingle(this byte[] array, ref int position, float value)
        {
            MemoryMarshal.Write(new Span<byte>(array, position, sizeof(float)), ref value);
            position += sizeof(float);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(this byte[] array, ref int position, double value)
        {
            MemoryMarshal.Write(new Span<byte>(array, position, sizeof(double)), ref value);
            position += sizeof(double);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBytes(this byte[] array, ref int position, byte[] bytes)
        {
            Buffer.BlockCopy(bytes, 0, array, position, bytes.Length);
            position += bytes.Length;
        }
    }
}
