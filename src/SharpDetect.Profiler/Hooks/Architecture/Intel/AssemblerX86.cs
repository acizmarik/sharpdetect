// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Iced.Intel;
using static Iced.Intel.AssemblerRegisters;

namespace SharpDetect.Profiler.Hooks.Architecture.Intel
{
    internal sealed class AssemblerX86 : CodeGeneratorBase
    {
        public override byte[] Assemble(HCORENUM nakedFunctionPtr)
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

            return CompileAsm(assembler);
        }

        #region SINGLETON_IMPLEMENTATION
        public static AssemblerX86 Instance
        {
            get
            {
                instance ??= new AssemblerX86();
                return instance;
            }
        }

        private static AssemblerX86? instance;

        private AssemblerX86()
        {

        }
        #endregion
    }
}
