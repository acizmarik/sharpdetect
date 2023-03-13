// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

namespace SharpDetect.Profiler.Hooks.Architecture.Intel
{
    internal sealed class AssemblerX64 : CodeGeneratorBase
    {
        public override byte[] Assemble(HCORENUM nakedFunctionPtr)
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

            return CompileAsm(assembler);
        }

        #region SINGLETON_IMPLEMENTATION
        public static AssemblerX64 Instance
        {
            get
            {
                instance ??= new AssemblerX64();
                return instance;
            }
        }

        private static AssemblerX64? instance;

        private AssemblerX64()
        {

        }
        #endregion
    }
}
