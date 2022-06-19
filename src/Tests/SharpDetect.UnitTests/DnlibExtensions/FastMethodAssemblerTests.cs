using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SharpDetect.Dnlib.Extensions;
using SharpDetect.UnitTests.Runtime.Scheduling;
using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;

namespace SharpDetect.UnitTests.DnlibExtensions
{
    public class FastMethodAssemblerTests
    {
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(Console))]
        [InlineData(typeof(Monitor))]
        [InlineData(typeof(Unsafe))]
        [InlineData(typeof(AssemblerTestMethods))]
        [InlineData(typeof(HappensBeforeScheduler_ProfilingEvents_Tests))]
        [InlineData(typeof(HappensBeforeScheduler_RewritingEvents_Tests))]
        [InlineData(typeof(HappensBeforeScheduler_ExecutingEvents_Tests))]
        public void FastMethodAssemblerTests_MethodsAreNotChanged(Type type)
        {
            var module = AssemblyDef.Load(type.Assembly.Location).ManifestModule;
            var typeDef = module.Find(type.FullName, isReflectionName: true);

            // Check all methods
            foreach (var methodDef in typeDef.Methods.Where(m => m.HasBody))
            {
                var assembler = new FastMethodAssembler(methodDef, new Dictionary<Instruction, MDToken>(), new StringHeapCache());
                var expected = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .SingleOrDefault(m => m.MetadataToken == methodDef.MDToken.Raw)?.GetMethodBody()!.GetILAsByteArray();
                if (expected is null)
                {
                    // For some reason we did not find the type using reflection
                    // This happens for example for .cctors
                    continue;
                }

                var actual = assembler.Assemble();

                EnsureMethodBodyIsSame(methodDef, expected, actual);
            }
        }

        private static void EnsureMethodBodyIsSame(MethodDef methodDef, Span<byte> reflectionResult, Span<byte> ourResult)
        {
            var isTinyHeader = (ourResult[0] & 0x03) != 3;
            ourResult = (isTinyHeader) ? ourResult[1..] : ourResult[12..];

            if (!methodDef.Body.HasExceptionHandlers)
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
