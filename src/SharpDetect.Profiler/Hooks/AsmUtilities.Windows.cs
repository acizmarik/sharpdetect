// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Profiler.Hooks.Platform.Windows;

namespace SharpDetect.Profiler.Hooks;

internal partial class AsmUtilities
{
    private static NakedEntryStub EmitStubForNakedCall_Windows_X64(IntPtr nakedFunctionPtr)
        => EmitStubForNakedCall_Windows(Architecture.Intel.AssemblerX64.Instance.Assemble(nakedFunctionPtr));

    private static NakedEntryStub EmitStubForNakedCall_Windows_X86(IntPtr nakedFunctionPtr)
        => EmitStubForNakedCall_Windows(Architecture.Intel.AssemblerX86.Instance.Assemble(nakedFunctionPtr));

    private unsafe static NakedEntryStub EmitStubForNakedCall_Windows(byte[] code)
    {
        /*  TODO: This implementation is wasteful
         *  -- It is badly documented but it seems like each call allocates 64KiB virtual address space (1 page)
         *  -- Either reuse the page for multiple stubs or use different API (something like memory protect) */

        // Note(alignment): generated code guarantees good alignment (all pages are well aligned)
        // Note(alignment): runtime seems to require alignment in ASM using ALIGN 16

        // Note(MEM_RESERVE): reserve memory page (needs to be committed before using)
        // Note(MEM_COMMIT): commits memory page (once accessed it gets mapped to a physical page)
        const AllocationType allocationType = AllocationType.COMMIT | AllocationType.RESERVE;

        // Note(EXECUTE_READWRITE): we need to both write the memory (copy compiled asm)
        // Note(EXECUTE_READWRITE): also we need it to be executable (it will be passed as a function ptr to runtime)
        const MemoryProtection protectionType = MemoryProtection.EXECUTE_READWRITE;

        // Note(MEM_RELEASE): releases the page (it is available for other allocations)
        const FreeType freeType = FreeType.RELEASE;

        var codeSize = (DWORD)code.Length;
        // Allocate memory
        var ptrDest = WindowsNativeFunctions.VirtualAlloc(IntPtr.Zero, codeSize, allocationType, protectionType);
        if (ptrDest == IntPtr.Zero)
            throw new InvalidOperationException("Could not allocate memory for method hook.");

        // Fill it with machine code
        fixed (byte* ptrSrc = code)
            Buffer.MemoryCopy(ptrSrc, (void*)ptrDest, codeSize, codeSize);

        return new(ptrDest, codeSize, () => WindowsNativeFunctions.VirtualFree(ptrDest, codeSize, freeType));
    }
}
