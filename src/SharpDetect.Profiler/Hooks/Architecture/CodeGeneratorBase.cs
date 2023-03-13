// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Iced.Intel;

namespace SharpDetect.Profiler.Hooks.Architecture
{
    internal unsafe abstract class CodeGeneratorBase
    {
        public abstract byte[] Assemble(IntPtr nakedFunctionPtr);

        public static byte[] CompileAsm(Assembler assembler)
        {
            using var memoryStream = new MemoryStream();
            var codeWriter = new StreamCodeWriter(memoryStream);
            assembler.Assemble(codeWriter, 0);
            return memoryStream.ToArray();
        }
    }
}
