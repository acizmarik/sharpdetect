using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Common.ControlFlow;
using Xunit;

namespace SharpDetect.UnitTests.ControlFlow
{
    public class ControlFlowGraphBuildingTests
    {
        [Fact]
        public void ControlFlowGraph_SingleBlock()
        {
            var method = new MethodDefUser("Method");
            method.Body = new CilBody();

            // return
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            method.Body.UpdateInstructionOffsets();

            var cfg = ControlFlowGraph.Construct(method.Body);
            Assert.NotNull(cfg);
            Assert.Equal(1, cfg.Count);
            Assert.Equal(Code.Nop, cfg[0].Header.OpCode.Code);
            Assert.Equal(Code.Ret, cfg[0].Footer.OpCode.Code);
        }

        [Fact]
        public void ControlFlowGraph_IfElseBlocks()
        {
            var method = new MethodDefUser("Method");
            method.Body = new CilBody();
            var label = Instruction.Create(OpCodes.Ret);

            // if (true)
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_1));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Brfalse, label));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            // else
            {
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            }
            method.Body.Instructions.Add(label);
            method.Body.UpdateInstructionOffsets();

            var cfg = ControlFlowGraph.Construct(method.Body);
            Assert.NotNull(cfg);
            Assert.Equal(4, cfg.Count);
            Assert.Equal(method.Body.Instructions[0]/*LDC*/.Offset, cfg[0].Header.Offset);
            Assert.Equal(method.Body.Instructions[1]/*BRFALSE*/.Offset, cfg[0].Footer.Offset);
            Assert.Equal(2, cfg[0].Targets.Count);
            Assert.Equal(0, cfg[0].Sources.Count);
            Assert.Equal(method.Body.Instructions[2]/*NOP*/.Offset, cfg[1].Header.Offset);
            Assert.Equal(method.Body.Instructions[3]/*RET*/.Offset, cfg[1].Footer.Offset);
            Assert.Equal(0, cfg[1].Targets.Count);
            Assert.Equal(1, cfg[1].Sources.Count);
            Assert.Equal(method.Body.Instructions[4]/*NOP*/.Offset, cfg[2].Header.Offset);
            Assert.Equal(method.Body.Instructions[5]/*NOP*/.Offset, cfg[2].Footer.Offset);
            Assert.Equal(1, cfg[2].Targets.Count);
            Assert.Equal(0, cfg[2].Sources.Count);
            Assert.Equal(method.Body.Instructions[6]/*RET*/.Offset, cfg[3].Header.Offset);
            Assert.Equal(0, cfg[3].Targets.Count);
            Assert.Equal(2, cfg[3].Sources.Count);
        }
    }
}
