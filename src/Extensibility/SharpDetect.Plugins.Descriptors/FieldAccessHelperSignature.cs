// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public static class FieldAccessHelperSignature
{
    public static ArgumentTypeDescriptor[] GetParameterTypes(bool isInstance, bool isGenericStatic = false) => (isInstance, isGenericStatic) switch
    {
        (true, _) =>
        [
            Sig.Int64,
            Sig.Object
        ],
        (false, true) =>
        [
            Sig.Int64,
            Sig.ValueType("System.RuntimeTypeHandle")
        ],
        (false, false) =>
        [
            Sig.Int64
        ]
    };

    public static MethodSignatureDescriptor Create(bool isInstance, bool isGenericStatic = false)
        => MethodSignatureDescriptor.ForStaticMethod(Sig.Void, GetParameterTypes(isInstance, isGenericStatic));
}
