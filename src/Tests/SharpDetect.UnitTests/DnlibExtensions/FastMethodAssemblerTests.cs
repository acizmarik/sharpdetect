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
        public void FastMethodAssemblerTests_MethodsAreNotChanged_SingleTypes(Type typeRef)
        {
            var module = AssemblyDef.Load(typeRef.Assembly.Location).ManifestModule;
            var typeDef = module.Find(typeRef.FullName, isReflectionName: true);

            CheckAllMethods(module, typeRef, typeDef);
        }

        [Theory]
        [InlineData(typeof(object))]
        public void FastMethodAssemblerTests_MethodsAreNotChanged_WholeAssemblies(Type type)
        {
            var moduleDef = AssemblyDef.Load(type.Assembly.Location).ManifestModule;
            var moduleRef = type.Assembly.ManifestModule;
            
            // Check all types
            foreach (var typeRef in moduleRef.GetTypes().Where(t => t is not null))
            {
                // Check all methods
                var typeDef = moduleDef.ResolveToken(typeRef.MetadataToken) as TypeDef;
                if (typeDef is null)
                    continue;

                CheckAllMethods(moduleDef, typeRef, typeDef);
            }
        }

        private void CheckAllMethods(ModuleDef module, Type typeRef, TypeDef typeDef)
        {
            // Check all methods
            foreach (var methodDef in typeDef.Methods.Where(m => m.HasBody))
            {
                var assembler = new FastMethodAssembler(methodDef, new Dictionary<Instruction, MDToken>(), new StringHeapCache(), false, false);
                var expected = typeRef.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
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

            for (var i = 0; i < reflectionResult.Length; i++)
            {
                var instruction = methodDef.Body.Instructions.Last(instr => instr.Offset <= i);
                if (reflectionResult[i] != ourResult[i])
                    throw new Exception($"Wrongly assembled instruction {instruction}");
            }
        }
    }
}
