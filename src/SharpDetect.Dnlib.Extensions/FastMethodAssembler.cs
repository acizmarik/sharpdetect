// The following code is heavily inspired and partially copied from the dnlib code base
// Copyright (C) 2012-2019 de4dot@gmail.com under MIT license

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.Services.Instrumentation;
using SharpDetect.Dnlib.Extensions.Utilities;

namespace SharpDetect.Dnlib.Extensions
{
    /// <summary>
    /// This is an assembler that performs almost no checks.
    /// Its purpose is to compile methods from dnlib instructions as fast as possible. It preserves metadata.
    /// </summary>
    public class FastMethodAssembler
    {
        private readonly MethodDef method;
        private readonly IReadOnlyDictionary<Instruction, MDToken> stubs;
        private readonly IStringHeapCache stringHeapCache;
        private byte[] bytecode;
        private byte[]? extraSections;
        private int firstInstructionOffset;
        private int position;
        private ushort maxStackSize;
        private uint codeSize;
        
        /// <summary>
        /// FastMethodAssembler ctor
        /// </summary>
        /// <param name="method">Method to assemble</param>
        /// <param name="stubs">Metadata tokens to fill in for injected stubs</param>
        public FastMethodAssembler(MethodDef method, IReadOnlyDictionary<Instruction, MDToken> stubs, IStringHeapCache stringHeapCache)
        {
            this.method = method;
            this.stubs = stubs;
            this.stringHeapCache = stringHeapCache;
            this.bytecode = null!;

            // Prepare for writing
            Guard.True<ArgumentException>(method.HasBody);
            Initialize();
        }

        private void Initialize()
        {
            method.Body.OptimizeMacros();
            method.Body.OptimizeBranches();
            method.Body.UpdateInstructionOffsets();

            // Calculate code size
            var lastInstruction = method.Body.Instructions[method.Body.Instructions.Count - 1];
            codeSize = (uint)(lastInstruction.Offset + lastInstruction.GetSize());

            // Calculate max stack size
            maxStackSize = (ushort)MaxStackCalculator.GetMaxStack(method.Body.Instructions, method.Body.ExceptionHandlers);
        }

        public byte[] Assemble()
        {
            if (!RequiresFatHeader())
            {
                // Tiny method
                bytecode = new byte[codeSize + 1 /* header size */];
                bytecode.WriteByte(ref position, 0x02);
                firstInstructionOffset = position;
                WriteInstructions();
            }
            else
            {
                // Fat method
                bytecode = new byte[codeSize + 12 /* header size */];
                WriteFatHeader();
                firstInstructionOffset = position;
                WriteInstructions();

                if (method.Body.ExceptionHandlers.Count > 0)
                    WriteExceptionHandlers();
            }

            if (extraSections is not null)
            {
                // Align code to next 4 bytes mark
                const uint align = 4;
                var padding = (align - bytecode.Length % align) % align;
                var result = new byte[bytecode.Length + padding + extraSections.Length];

                // Generate method body
                Buffer.BlockCopy(bytecode, 0, result, 0, bytecode.Length);
                Buffer.BlockCopy(extraSections, 0, result, bytecode.Length + (int)padding, extraSections.Length);

                return result;
            }

            return bytecode;
        }

        private bool RequiresFatHeader()
        {
            return codeSize >= 64 ||
                   method.Body.HasVariables ||
                   method.Body.HasExceptionHandlers ||
                   maxStackSize > 8;
        }

        private void WriteFatHeader()
        {
            /*  0-11  bits are for Flags (0x08 is CorILMethod_MoreSects; 0x10 is CorILMethods_InitLocals)
             *  12-15 bits is for header size (write always 3 - actually means multiplied by 4 = 12 bytes)
             *  2-3 bytes  is max stack size
             *  4-7 bytes  is code size
             *  8-11 bytes is LocalVarSig token
             */

            byte flags = 0x03;
            if (method.Body.InitLocals)
                flags |= 0x10;
            if (method.Body.HasExceptionHandlers)
                flags |= 0x08;

            bytecode.WriteByte(ref position, flags);
            bytecode.WriteByte(ref position, 0x30);
            bytecode.WriteUInt16(ref position, maxStackSize);
            bytecode.WriteUInt32(ref position, codeSize);
            bytecode.WriteUInt32(ref position, method.Body.LocalVarSigTok);
        }

        private void WriteInstructions()
        {
            for (var index = 0; index < method.Body.Instructions.Count; index++)
            {
                var instruction = method.Body.Instructions[index];
                WriteOpCode(instruction.OpCode);
                WriteOperand(instruction);
            }
        }

        private void WriteOpCode(OpCode opcode)
        {
            if (opcode.Size == 1)
            {
                bytecode.WriteByte(ref position, (byte)opcode.Code);
            }
            else
            {
                bytecode.WriteByte(ref position, (byte)((ushort)opcode.Code >> 8));
                bytecode.WriteByte(ref position, (byte)opcode.Code);
            }
        }

        private void WriteOperand(Instruction instruction)
        {
            switch (instruction.OpCode.OperandType)
            {
                // This instruction has no operand
                case OperandType.InlineNone: break;

                // Inline literals
                case OperandType.InlineI: bytecode.WriteInt32(ref position, (int)instruction.Operand); break;
                case OperandType.InlineI8: bytecode.WriteInt64(ref position, (long)instruction.Operand); break;
                case OperandType.InlineR: bytecode.WriteDouble(ref position, (double)instruction.Operand); break;
                case OperandType.ShortInlineR: bytecode.WriteSingle(ref position, (float)instruction.Operand); break;
                case OperandType.ShortInlineI: WriteShortInlineI(instruction); break;

                // Inline offsets
                case OperandType.InlineBrTarget: bytecode.WriteInt32(ref position, (int)((instruction.Operand as Instruction)!.Offset - (position - (firstInstructionOffset - sizeof(int))))); break;
                case OperandType.ShortInlineBrTarget: bytecode.WriteSByte(ref position, (sbyte)((instruction.Operand as Instruction)!.Offset - (position - (firstInstructionOffset - sizeof(sbyte))))); break;
                case OperandType.InlineSwitch: WriteInlineSwitch(instruction); break;

                // Inline tokens
                case OperandType.InlineString:
                case OperandType.InlineType:
                case OperandType.InlineField:
                case OperandType.InlineMethod:
                case OperandType.InlineSig:
                case OperandType.InlineTok:
                    bytecode.WriteUInt32(ref position, GetToken(instruction).Raw); break;

                // Variables and locals
                case OperandType.InlineVar: bytecode.WriteUInt16(ref position, (ushort)(instruction.Operand as IVariable)!.Index); break;
                case OperandType.ShortInlineVar: bytecode.WriteByte(ref position, (byte)(instruction.Operand as IVariable)!.Index); break;

                // Unused stuff
                case OperandType.InlinePhi:
                case OperandType.NOT_USED_8:
                default:
                    Guard.NotReachable<InvalidProgramException>(); break;
            }
        }

        private void WriteShortInlineI(Instruction instruction)
        {
            if (instruction.OpCode.Code == Code.Ldc_I4_S)
            {
                bytecode.WriteSByte(ref position, (sbyte)instruction.Operand);
            }
            else
            {
                bytecode.WriteByte(ref position, (byte)instruction.Operand);
            }
        }

        private void WriteInlineSwitch(Instruction instruction)
        {
            var targets = (instruction.Operand as IList<Instruction>)!;
            uint exitOffset = (uint)(4 * targets.Count * 4);
            bytecode.WriteInt32(ref position, targets.Count);

            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                bytecode.WriteUInt32(ref position, target.Offset - exitOffset);
            }
        }
        
        /// <summary>
        /// Note this method is making a lot of assumptions
        /// (i.e. tokens are not changing - if they are we supply them as stubs)
        private MDToken GetToken(Instruction instruction)
        {
            if (stubs.TryGetValue(instruction, out var token))
                return token;
            var operand = instruction.Operand;
            
            if (operand is IMDTokenProvider tokenProvider)
            {
                return tokenProvider.MDToken;
            }

            if (operand is string str)
            {
                return stringHeapCache.GetStringOffset(method.Module, str);
            }

            if (operand is MethodSig methodSig)
            {
                return new MDToken(methodSig.OriginalToken);
            }

            if (operand is FieldSig)
            {
                return new MDToken(Table.StandAloneSig, 0);
            }

            Guard.NotReachable<ArgumentException>();
            return default;
        }

        private void WriteExceptionHandlers()
        {
            if (NeedFatExceptionClauses())
                extraSections = WriteFatExceptionClauses(method.Body.ExceptionHandlers);
            else
                extraSections = WriteSmallExceptionClauses(method.Body.ExceptionHandlers);
        }

        bool NeedFatExceptionClauses()
        {
            // Size must fit in a byte, and since one small exception record is 12 bytes
            // and header is 4 bytes: x*12+4 <= 255 ==> x <= 20
            var exceptionHandlers = method.Body.ExceptionHandlers;
            if (exceptionHandlers.Count > 20)
                return true;

            for (int i = 0; i < exceptionHandlers.Count; i++)
            {
                var eh = exceptionHandlers[i];
                if (!FitsInSmallExceptionClause(eh.TryStart, eh.TryEnd))
                    return true;
                if (!FitsInSmallExceptionClause(eh.HandlerStart, eh.HandlerEnd))
                    return true;
            }

            return false;
        }

        private static bool FitsInSmallExceptionClause(Instruction start, Instruction end)
        {
            if (start.Offset < end.Offset)
                return false;
            return start.Offset <= ushort.MaxValue && end.Offset - start.Offset <= byte.MaxValue;
        }

        private static byte[] WriteFatExceptionClauses(IList<ExceptionHandler> exceptionHandlers)
        {
            int numExceptionHandlers = exceptionHandlers.Count;

            var data = new byte[numExceptionHandlers * 24 + 4];
            var writer = new ArrayWriter(data);

            // Write exception handlers size
            writer.WriteUInt32((((uint)numExceptionHandlers * 24 + 4) << 8) | 0x41);
            for (int i = 0; i < numExceptionHandlers; i++)
            {
                var eh = exceptionHandlers[i];

                // Write handler type
                writer.WriteUInt32((uint)eh.HandlerType);

                // Write try block
                Guard.True<InvalidProgramException>(eh.TryStart.Offset < eh.TryEnd.Offset);
                writer.WriteUInt32(eh.TryStart.Offset);
                writer.WriteUInt32(eh.TryEnd.Offset - eh.TryStart.Offset);

                // Write handler block
                Guard.True<InvalidProgramException>(eh.HandlerStart.Offset < eh.HandlerEnd.Offset);
                writer.WriteUInt32(eh.HandlerStart.Offset);
                writer.WriteUInt32(eh.HandlerEnd.Offset - eh.HandlerStart.Offset);

                // Write types for catch / filter blocks
                if (eh.IsCatch)
                    writer.WriteUInt32(eh.CatchType.MDToken.Raw);
                else if (eh.IsFilter)
                    writer.WriteUInt32(eh.FilterStart.Offset);
                else
                    writer.WriteInt32(0);
            }

            Guard.Equal<int, InvalidProgramException>(data.Length, writer.Position);
            return data;
        }

        private static byte[] WriteSmallExceptionClauses(IList<ExceptionHandler> exceptionHandlers)
        {
            int numExceptionHandlers = exceptionHandlers.Count;

            var data = new byte[numExceptionHandlers * 12 + 4];
            var writer = new ArrayWriter(data);

            // Write exception handlers size
            writer.WriteUInt32((((uint)numExceptionHandlers * 12 + 4) << 8) | 1);
            for (int i = 0; i < numExceptionHandlers; i++)
            {
                var eh = exceptionHandlers[i];

                // Write handler type
                writer.WriteUInt16((ushort)eh.HandlerType);

                // Write try block
                Guard.True<InvalidProgramException>(eh.TryStart.Offset < eh.TryEnd.Offset);
                writer.WriteUInt16((ushort)eh.TryStart.Offset);
                writer.WriteByte((byte)(eh.TryEnd.Offset - eh.TryStart.Offset));

                // Write handler block
                Guard.True<InvalidProgramException>(eh.HandlerStart.Offset < eh.HandlerEnd.Offset);
                writer.WriteUInt16((ushort)eh.HandlerStart.Offset);
                writer.WriteByte((byte)(eh.HandlerEnd.Offset - eh.HandlerStart.Offset));

                // Write types for catch / filter blocks
                if (eh.IsCatch)
                    writer.WriteUInt32(eh.CatchType.MDToken.Raw);
                else if (eh.IsFilter)
                    writer.WriteUInt32(eh.FilterStart.Offset);
                else
                    writer.WriteInt32(0);
            }

            if (writer.Position != data.Length)
                throw new InvalidOperationException();
            return data;
        }
    }
}
