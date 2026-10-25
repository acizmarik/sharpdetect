// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 8:0:0:0 }
.assembly extern System.Threading { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 8:0:0:0 }
.assembly SharpDetect.E2ETests.Subject.IL { }
.module SharpDetect.E2ETests.Subject.IL.dll

.class public auto ansi beforefieldinit SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess
    extends [System.Runtime]System.Object
{
    .field public int32 Value
    .field public static int32 Counter

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig static int32 ReadVolatileUnaligned(class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess target) cil managed
    {
        ldarg.0
        volatile.
        unaligned. 1
        ldfld int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Value
        ret
    }

    .method public hidebysig static int32 ReadUnalignedVolatile(class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess target) cil managed
    {
        ldarg.0
        unaligned. 1
        volatile.
        ldfld int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Value
        ret
    }

    .method public hidebysig static int32 ReadUnaligned(class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess target) cil managed
    {
        ldarg.0
        unaligned. 1
        ldfld int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Value
        ret
    }

    .method public hidebysig static int32 ReadVolatileUnalignedFromEither(
        bool pickFirst,
        class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess first,
        class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess second) cil managed
    {
        ldarg.0
        brfalse.s PICK_SECOND
        ldarg.1
        br.s ACCESS
    PICK_SECOND:
        ldarg.2
    ACCESS:
        volatile.
        unaligned. 1
        ldfld int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Value
        ret
    }

    .method public hidebysig static void WriteVolatileUnaligned(class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess target, int32 'value') cil managed
    {
        ldarg.0
        ldarg.1
        volatile.
        unaligned. 1
        stfld int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Value
        ret
    }

    .method public hidebysig static void WriteUnalignedVolatile(class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess target, int32 'value') cil managed
    {
        ldarg.0
        ldarg.1
        unaligned. 1
        volatile.
        stfld int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Value
        ret
    }

    .method public hidebysig static void WriteUnaligned(class SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess target, int32 'value') cil managed
    {
        ldarg.0
        ldarg.1
        unaligned. 1
        stfld int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Value
        ret
    }

    .method public hidebysig static int32 IncrementInTailPosition() cil managed
    {
        ldsflda int32 SharpDetect.E2ETests.Subject.IL.PrefixedFieldAccess::Counter
        tail.
        call int32 [System.Threading]System.Threading.Interlocked::Increment(int32&)
        ret
    }
}
