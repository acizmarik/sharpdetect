using dnlib.DotNet;
using dnlib.DotNet.Emit;
using System.Diagnostics.CodeAnalysis;

namespace SharpDetect.Instrumentation.Utilities
{
    internal static class MethodDefExtensions
    {
        private static bool HasPrefixInstruction(MethodDef method, int instructionIndex)
              => instructionIndex != 0 && method.Body.Instructions[instructionIndex - 1].OpCode.OpCodeType == OpCodeType.Prefix;

        private static bool IsStartingTry(MethodDef method, Instruction instruction, [NotNullWhen(returnValue: true)] out ExceptionHandler? handler)
        {
            handler = method.Body.ExceptionHandlers.FirstOrDefault(h => h.TryStart == instruction);
            return handler != null;
        }

        private static bool IsStartingFilter(MethodDef method, Instruction instruction, [NotNullWhen(returnValue: true)] out ExceptionHandler? handler)
        {
            handler = method.Body.ExceptionHandlers.FirstOrDefault(h => h.FilterStart == instruction);
            return handler != null;
        }

        private static bool IsStartingHandler(MethodDef method, Instruction instruction, [NotNullWhen(returnValue: true)] out ExceptionHandler? handler)
        {
            handler = method.Body.ExceptionHandlers.FirstOrDefault(h => h.HandlerStart == instruction);
            return handler != null;
        }

        private static bool IsEndingHandler(MethodDef method, Instruction instruction, [NotNullWhen(returnValue: true)] out ExceptionHandler? handler)
        {
            handler = method.Body.ExceptionHandlers.FirstOrDefault(h => h.HandlerEnd == instruction);
            return handler != null;
        }

        private static void ExtendTryStart(Instruction newTryStart, ExceptionHandler handler)
            => handler.TryStart = newTryStart;

        private static void ExtendFilterStart(Instruction newFilterStart, ExceptionHandler handler)
        {
            // Shift filter start
            handler.FilterStart = newFilterStart;

            // We have to shift also end of try block
            handler.TryEnd = newFilterStart;
        }

        private static void ExtendHandlerStart(Instruction oldHandlerStart, Instruction newHandlerStart, ExceptionHandler handler)
        {
            // Shift handler start
            handler.HandlerStart = newHandlerStart;

            // Check if we have to shift also end of try block
            if (handler.TryEnd == oldHandlerStart)
                handler.TryEnd = newHandlerStart;
        }

        private static void ExtendHandlerEnd(MethodDef method, Instruction oldHandlerEnd, Instruction newHandlerEnd, ExceptionHandler handler)
        {
            // If handler end is valid until method end we do not need to extend anything
            if (handler.HandlerEnd == null)
                return;

            var index = method.Body.Instructions.IndexOf(newHandlerEnd);
            handler.HandlerEnd = method.Body.Instructions[index];

            // Shift handler leave instruction
            foreach (var leaveInstruction in method.Body.Instructions.Where(i => IsLeaveInstructionForBlock(i, oldHandlerEnd)))
                leaveInstruction.Operand = newHandlerEnd;
        }

        private static bool IsLeaveInstructionForBlock(Instruction instruction, Instruction oldLabel)
        {
            if (instruction.OpCode.Code != Code.Leave && instruction.OpCode.Code != Code.Leave_S)
                return false;
            if (instruction.Operand != oldLabel)
                return false;

            return true;
        }

        private static int GetLabelBeforeOrSelf(MethodDef method, int instructionIndex)
        {
            if (HasPrefixInstruction(method, instructionIndex))
            {
                var currentInstructionIndex = instructionIndex - 1;
                while (method.Body.Instructions[currentInstructionIndex - 1].OpCode.OpCodeType == OpCodeType.Prefix)
                    --currentInstructionIndex;

                return currentInstructionIndex;
            }

            return instructionIndex;
        }

        private static void RedirectBranchTargets(MethodDef method, Instruction oldTarget, Instruction newTarget)
        {
            for (var instructionIndex = 0; instructionIndex < method.Body.Instructions.Count; instructionIndex++)
            {
                var currentInstruction = method.Body.Instructions[instructionIndex];
                // Check if the instruction is branching
                if (!currentInstruction.IsConditionalBranch() && !currentInstruction.IsBr() && currentInstruction.OpCode.Code != Code.Jmp)
                    continue;

                var target = (Instruction)currentInstruction.Operand;
                // Check that target is correct
                if (target != oldTarget)
                    continue;

                // Fix branch target
                currentInstruction.Operand = newTarget;
            }
        }

        private static void FixHandlers(MethodDef method, Instruction target, Instruction firstInjectedInstruction)
        {
            // Make sure that we did not break any try block
            if (IsStartingTry(method, target, out var handlerBlock))
                ExtendTryStart(firstInjectedInstruction, handlerBlock);
            // Make sure that we did not break any filter block
            else if (IsStartingFilter(method, target, out handlerBlock))
                ExtendFilterStart(firstInjectedInstruction, handlerBlock);
            // Make sure that we did not break any handler block
            else if (IsStartingHandler(method, target, out handlerBlock))
                ExtendHandlerStart(target, firstInjectedInstruction, handlerBlock);
            else if (IsEndingHandler(method, target, out handlerBlock))
                ExtendHandlerEnd(method, target, firstInjectedInstruction, handlerBlock);
        }

        public static void InjectBefore(this MethodDef method, Instruction target, IEnumerable<Instruction> instructions)
        {
            // If instructions are empty then there is nothing to do
            if (!instructions.Any())
                return;

            // Simplify branch instructions so that we wont break short branches
            method.Body.SimplifyBranches();

            var index = method.Body.Instructions.IndexOf(target);
            index = GetLabelBeforeOrSelf(method, index);

            foreach (var instruction in instructions)
            {
                method.Body.Instructions.Insert(index, instruction);
                ++index;
            }

            // Make sure that we fix branches, handlers
            RedirectBranchTargets(method, target, instructions.First());
            FixHandlers(method, target, instructions.First());

            method.Body.OptimizeBranches();
            method.Body.OptimizeMacros();
        }

        public static void InjectAfter(this MethodDef method, Instruction target, IEnumerable<Instruction> instructions)
        {
            // If instructions are empty then there is nothing to do
            if (!instructions.Any())
                return;

            // Simplify branch instructions so that we wont break short branches
            method.Body.SimplifyBranches();

            var injectingIndex = method.Body.Instructions.IndexOf(target);
            foreach (var instruction in instructions)
            {
                method.Body.Instructions.Insert(injectingIndex + 1, instruction);
                ++injectingIndex;
            }

            method.Body.OptimizeBranches();
            method.Body.OptimizeMacros();
        }

        public static void InjectAfter(this MethodDef method, Instruction target, Instruction instruction)
        {
            if (instruction == null)
                return;

            method.Body.SimplifyBranches();

            var injectingIndex = method.Body.Instructions.IndexOf(target);
            method.Body.Instructions.Insert(injectingIndex + 1, instruction);

            method.Body.OptimizeBranches();
            method.Body.OptimizeMacros();
        }
    }
}
