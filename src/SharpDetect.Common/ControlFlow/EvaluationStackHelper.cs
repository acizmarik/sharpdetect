// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Common.ControlFlow
{
    public static class EvaluationStackHelper
    {
        internal static bool TryFindNthTypeOnStack(int instructionIndex, MethodDef method, int elementIndex, [NotNullWhen(true)] out Instruction? result)
        {
            /*  Assumption:
             *  
             *  Regardless of the control flow that allows execution to arrive there, each slot on the
             *  stack shall have the same data type at any given point within the method body
             *  
             *  In order for the JIT compilers to efficiently track the data types stored on the stack,
             *  the stack shall normally be empty at the instruction following an unconditional
             *  control transfer instruction (br, br.s, ret, jmp, throw, endfilter, endfault, or
             *  endfinally). The stack shall be non-empty at such an instruction only if at some
             *  earlier location within the method there has been a forward branch to that
             *  instruction
             *  
             *  Source: ECMA-335, I.12.4 Control Flow
             *  https://www.ecma-international.org/publications/files/ECMA-ST/ECMA-335.pdf */

            var totalPushes = 0;
            var cfg = ControlFlowGraph.Construct(method.Body);
            var block = cfg.GetContainingBlock(instructionIndex);
            var instruction = method.Body.Instructions[instructionIndex];

            do
            {
                if (block.Header != instruction)
                {
                    // Next instruction is within the same block
                    instructionIndex--;
                    instruction = method.Body.Instructions[instructionIndex];
                }
                else
                {
                    // Stack state should be same for each source block
                    block = block.Sources[0];
                    instructionIndex = method.Body.Instructions.IndexOf(block.Footer);
                    instruction = block.Footer;
                }

                switch (instruction.OpCode.Code)
                {
                    case Code.Br:
                    case Code.Br_S:
                    case Code.Ret:
                    case Code.Jmp:
                    case Code.Throw:
                    case Code.Endfilter:
                    case Code.Endfinally:
                        continue;
                    default:
                        // Calculate stack changes
                        instruction.CalculateStackUsage(out var pushes, out var pops);
                        totalPushes += pushes;
                        if (totalPushes != elementIndex)
                        {
                            totalPushes -= pops;
                            if (totalPushes != elementIndex)
                                continue;
                        }

                        result = instruction;
                        return true;
                }
            } while (instructionIndex > -1);

            result = null;
            return false;
        }

        public static bool TryFindFieldReadInstanceInfo(MethodDef method, int instructionIndex, [NotNullWhen(true)] out Instruction? result)
            => TryFindNthTypeOnStack(instructionIndex, method, 1, out result);

        public static bool TryFindFieldWriteInstanceInfo(MethodDef method, int instructionIndex, [NotNullWhen(true)] out Instruction? result)
            => TryFindNthTypeOnStack(instructionIndex, method, 2, out result);

        public static bool TryFindArrayInstanceReadElementInfo(MethodDef method, int instructionIndex, [NotNullWhen(true)] out Instruction? result)
            => TryFindNthTypeOnStack(instructionIndex, method, 2, out result);

        public static bool TryFindArrayInstanceWriteElementInfo(MethodDef method, int instructionIndex, [NotNullWhen(true)] out Instruction? result)
            => TryFindNthTypeOnStack(instructionIndex, method, 3, out result);

        public static bool TryFindArrayIndexWriteInfo(MethodDef method, int instructionIndex, [NotNullWhen(true)] out Instruction? result)
            => TryFindNthTypeOnStack(instructionIndex, method, 2, out result);

        public static bool TryFindArrayIndexReadInfo(MethodDef method, int instructionIndex, [NotNullWhen(true)] out Instruction? result)
            => TryFindNthTypeOnStack(instructionIndex, method, 1, out result);
    }
}
