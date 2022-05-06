using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Dnlib.Extensions.Assembler;
using System.Reflection;
using Xunit;

namespace SharpDetect.UnitTests.DnlibExtensions
{
    public class FastMethodAssemblerTests
    {
        [Theory]
        [InlineData(typeof(Console))]
        public void FastMethodAssemblerTests_TinyMethod(Type type)
        {
            var module = AssemblyDef.Load(type.Assembly.Location).ManifestModule;
            var typeDef = module.Find(type.FullName, isReflectionName: true);

            // Check all methods
            foreach (var methodDef in typeDef.Methods.Where(m => m.HasBody && m.Body.IsSmallHeader))
            {
                var assembler = new FastMethodAssembler(methodDef, new Dictionary<Instruction, MDToken>());
                var expected = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Single(m => m.MetadataToken == methodDef.MDToken.Raw).GetMethodBody()!.GetILAsByteArray();
                var actual = assembler.Assemble();

                EnsureMethodBodyIsSame(methodDef, expected, actual);
            }
        }

        private void EnsureMethodBodyIsSame(MethodDef methodDef, Span<byte> reflectionResult, Span<byte> ourResult)
        {
            var isTinyHeader = (ourResult[0] & 0x02) != 0;
            ourResult = (isTinyHeader) ? ourResult[1..] : ourResult[12..];

            Assert.Equal(reflectionResult.Length, ourResult.Length);
            for (var i = 0; i < reflectionResult.Length; i++)
            {
                var instruction = methodDef.Body.Instructions.Last(instr => instr.Offset <= i);
                if (reflectionResult[i] != ourResult[i])
                    throw new Exception($"Wrongly assembled instruction {instruction}");
            }
        }
    }
}
