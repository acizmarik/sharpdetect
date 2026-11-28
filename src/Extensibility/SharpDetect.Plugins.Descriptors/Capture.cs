// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public static class Capture
{
    public static CapturedValueDescriptor Reference { get; } = new((byte)nint.Size, CapturedValue.CaptureAsReference);
    public static CapturedValueDescriptor ReferenceArray { get; } = new((byte)nint.Size, CapturedValue.CaptureAsReferenceArray);
    public static CapturedValueDescriptor Boolean { get; } = new(sizeof(bool), CapturedValue.CaptureAsValue);
    public static CapturedValueDescriptor Int32 { get; } = new(sizeof(int), CapturedValue.CaptureAsValue);
    public static CapturedValueDescriptor Int64 { get; } = new(sizeof(long), CapturedValue.CaptureAsValue);
    public static CapturedValueDescriptor NativeInt { get; } = new((byte)nint.Size, CapturedValue.CaptureAsValue);

    public static CapturedValueDescriptor DereferencedReference { get; } = Dereferenced(Reference);
    public static CapturedValueDescriptor DereferencedBoolean { get; } = Dereferenced(Boolean);
    public static CapturedValueDescriptor OutReference { get; } = WrittenByCallee(Reference);
    public static CapturedValueDescriptor OutBoolean { get; } = WrittenByCallee(Boolean);

    private static CapturedValueDescriptor Dereferenced(CapturedValueDescriptor value)
        => value with { Flags = value.Flags | CapturedValue.Dereference };

    private static CapturedValueDescriptor WrittenByCallee(CapturedValueDescriptor value)
        => value with { Flags = value.Flags | CapturedValue.Dereference | CapturedValue.ReloadOnExit };
}
