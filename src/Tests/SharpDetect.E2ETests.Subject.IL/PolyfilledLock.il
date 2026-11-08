// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 8:0:0:0 }
.assembly extern System.Threading { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 8:0:0:0 }

.class private auto ansi sealed beforefieldinit System.Threading.Lock
    extends [System.Runtime]System.Object
{
    .class nested public sequential ansi sealed beforefieldinit Scope
        extends [System.Runtime]System.ValueType
    {
        .field private initonly class System.Threading.Lock _lock

        .method public hidebysig specialname rtspecialname instance void .ctor(class System.Threading.Lock 'lock') cil managed
        {
            ldarg.0
            ldarg.1
            stfld class System.Threading.Lock System.Threading.Lock/Scope::_lock
            ret
        }

        .method public hidebysig instance void Dispose() cil managed
        {
            ldarg.0
            ldfld class System.Threading.Lock System.Threading.Lock/Scope::_lock
            call instance void System.Threading.Lock::Exit()
            ret
        }
    }

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }

    .method public hidebysig instance void Enter() cil managed
    {
        ldarg.0
        call void [System.Threading]System.Threading.Monitor::Enter(object)
        ret
    }

    .method public hidebysig instance void Exit() cil managed
    {
        ldarg.0
        call void [System.Threading]System.Threading.Monitor::Exit(object)
        ret
    }

    .method public hidebysig instance valuetype System.Threading.Lock/Scope EnterScope() cil managed
    {
        ldarg.0
        call instance void System.Threading.Lock::Enter()
        ldarg.0
        newobj instance void System.Threading.Lock/Scope::.ctor(class System.Threading.Lock)
        ret
    }
}

.class public auto ansi abstract sealed beforefieldinit SharpDetect.E2ETests.Subject.IL.PolyfilledLock
    extends [System.Runtime]System.Object
{
    // Equivalent of "lock (polyfilledLock) { }" in a loop, which C# lowers to EnterScope + Scope.Dispose
    .method public hidebysig static void EnterScopeLoop(int32 iterations) cil managed
    {
        .maxstack 2
        .locals init (class System.Threading.Lock lockObj, valuetype System.Threading.Lock/Scope scope, int32 i)
        newobj instance void System.Threading.Lock::.ctor()
        stloc.0
        ldc.i4.0
        stloc.2
        br.s CHECK
    LOOP:
        ldloc.0
        call instance valuetype System.Threading.Lock/Scope System.Threading.Lock::EnterScope()
        stloc.1
        ldloca.s scope
        call instance void System.Threading.Lock/Scope::Dispose()
        ldloc.2
        ldc.i4.1
        add
        stloc.2
    CHECK:
        ldloc.2
        ldarg.0
        blt.s LOOP
        ret
    }
}
