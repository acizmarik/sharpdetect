// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Events;

public readonly struct PrimitiveArgument(ulong bits, PrimitiveKind kind)
{
    public PrimitiveKind Kind => kind;

    private ulong Integral()
    {
        if (kind is PrimitiveKind.R4 or PrimitiveKind.R8)
            ThrowMismatch("an integer");

        return bits;
    }

    private ulong Exact(PrimitiveKind expected)
    {
        if (kind != expected)
            ThrowMismatch(expected.ToString());

        return bits;
    }

    private void ThrowMismatch(string expected)
        => throw new InvalidOperationException($"Argument is {kind}, which cannot be read as {expected}.");

    public static explicit operator bool(PrimitiveArgument v) => v.Exact(PrimitiveKind.Boolean) != 0;
    public static explicit operator char(PrimitiveArgument v) => (char)v.Integral();
    public static explicit operator sbyte(PrimitiveArgument v) => (sbyte)v.Integral();
    public static explicit operator byte(PrimitiveArgument v) => (byte)v.Integral();
    public static explicit operator short(PrimitiveArgument v) => (short)v.Integral();
    public static explicit operator ushort(PrimitiveArgument v) => (ushort)v.Integral();
    public static explicit operator int(PrimitiveArgument v) => (int)v.Integral();
    public static explicit operator uint(PrimitiveArgument v) => (uint)v.Integral();
    public static explicit operator long(PrimitiveArgument v) => (long)v.Integral();
    public static explicit operator ulong(PrimitiveArgument v) => v.Integral();
    public static explicit operator float(PrimitiveArgument v) => BitConverter.UInt32BitsToSingle((uint)v.Exact(PrimitiveKind.R4));
    public static explicit operator double(PrimitiveArgument v) => BitConverter.UInt64BitsToDouble(v.Exact(PrimitiveKind.R8));
    public static explicit operator nint(PrimitiveArgument v) => (nint)v.Integral();
    public static explicit operator nuint(PrimitiveArgument v) => (nuint)v.Integral();
}