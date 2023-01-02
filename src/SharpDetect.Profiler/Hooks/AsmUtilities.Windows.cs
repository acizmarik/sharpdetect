using Iced.Intel;
using SharpDetect.Profiler.Hooks.PAL.Windows;
using static Iced.Intel.AssemblerRegisters;

namespace SharpDetect.Profiler.Hooks;

internal partial class AsmUtilities
{
    private static NakedEntryStub EmitStubForNakedCall_Windows_X64(IntPtr nakedFunctionPtr)
    {
        // We are on a 64-bit architecture (x86-64)
        // 1) save registers from call clobbering (RAX-RDX, R8 - R11)
        // 2) store target function pointer as R11
        // 3) call hook (indirect call based on R11)
        // 4) restore registers (RAX-RDX, R8 - R11)
        // 5) return

        var assembler = new Assembler(64);
        assembler.push(rax);
        assembler.push(rcx);
        assembler.push(rdx);
        assembler.push(r8);
        assembler.push(r9);
        assembler.push(r10);
        assembler.push(r11);
        assembler.mov(r11, nakedFunctionPtr);
        assembler.sub(rsp, 0x20);
        assembler.call(r11);
        assembler.add(rsp, 0x20);
        assembler.pop(r11);
        assembler.pop(r10);
        assembler.pop(r9);
        assembler.pop(r8);
        assembler.pop(rdx);
        assembler.pop(rcx);
        assembler.pop(rax);
        assembler.ret();

        return EmitStubForNakedCall_Windows(CompileAsm(assembler));
    }

    private static NakedEntryStub EmitStubForNakedCall_Windows_X86(IntPtr nakedFunctionPtr)
    {
        // TODO: x86 was not tested during development
        // -- I am actually not sure whether I am coordinated with calling conventions

        // We are on a 32-bit architecture (x86)
        // 1) save registers from call clobbering (EAX-EDX)
        // 2) store target function pointer as EDX
        // 3) call hook (indirect call based on EDX)
        // 4) restore registers (RAX-RDX, R8 - R11)
        // 5) return

        var assembler = new Assembler(32);
        assembler.push(eax);
        assembler.push(ecx);
        assembler.push(edx);
        assembler.sub(esp, 0x10);
        assembler.mov(edx, (uint)nakedFunctionPtr);
        assembler.add(esp, 0x10);
        assembler.pop(edx);
        assembler.pop(ecx);
        assembler.pop(eax);
        assembler.ret();

        return EmitStubForNakedCall_Windows(CompileAsm(assembler));
    }

    private static NakedEntryStub EmitStubForNakedCall_Windows(byte[] code)
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
        var ptr = WindowsNativeFunctions.VirtualAlloc(IntPtr.Zero, codeSize, allocationType, protectionType);
        // Fill it with machine code
        FillMemory(code, ptr, codeSize);

        return new(ptr, codeSize, () => WindowsNativeFunctions.VirtualFree(ptr, codeSize, freeType));
    }
}
