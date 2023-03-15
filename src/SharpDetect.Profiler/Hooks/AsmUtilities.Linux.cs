// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Profiler.Hooks.Platform.Linux;

namespace SharpDetect.Profiler.Hooks;

internal partial class AsmUtilities
{
    private static NakedEntryStub EmitStubForNakedCall_Linux_X64(IntPtr nakedFunctionPtr)
        => EmitStubForNakedCall_Linux(Architecture.Intel.AssemblerX64.Instance.Assemble(nakedFunctionPtr));

    private static NakedEntryStub EmitStubForNakedCall_Linux_X86(IntPtr nakedFunctionPtr)
        => EmitStubForNakedCall_Linux(Architecture.Intel.AssemblerX86.Instance.Assemble(nakedFunctionPtr));

    private unsafe static NakedEntryStub EmitStubForNakedCall_Linux(byte[] code)
    {
        // Note(alignment): if addr is null kernele guarantees good alignment (page-alignment)
        // Note(alignment): runtime seems to require alignment in ASM using ALIGN 16

        // Note(MAP_PRIVATE): private copy-on-writing mapping
        // Note(MAP_ANONYMOUS): mapping is not backed by a file
        const MmapFlags flags = 
            MmapFlags.MAP_PRIVATE | 
            MmapFlags.MAP_ANONYMOUS;

        // Note(PROT_READ): page can be read
        // Note(PROT_WRITE): page can be written
        // Note(PROT_EXEC): page can be executed
        const MmapProtectionType protection = 
            MmapProtectionType.PROT_READ | 
            MmapProtectionType.PROT_WRITE | 
            MmapProtectionType.PROT_EXEC;

        var codeSize = code.Length;
        // Allocate memory
        var ptrDest = LinuxNativeFunctions.mmap(IntPtr.Zero, (nuint)codeSize, protection, flags, 0, 0);
        if (ptrDest == IntPtr.Zero)
            throw new InvalidOperationException("Could not allocate memory for method hook.");

        // Fill it with machine code
        fixed (byte* ptrSrc = code)
            Buffer.MemoryCopy(ptrSrc, (void*)ptrDest, codeSize, codeSize);

        return new(ptrDest, (uint)codeSize, () => LinuxNativeFunctions.munmap(ptrDest, (nuint)codeSize));
    }
}
