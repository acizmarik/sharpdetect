using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
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

            CheckAllMethodsReflection(typeRef, typeDef);
        }

        [Theory]
        [InlineData(typeof(object))]
        public void FastMethodAssemblerTests_MethodsAreNotChanged_WholeAssemblies_AgainstReflection(Type type)
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

                CheckAllMethodsReflection(typeRef, typeDef);
            }
        }

        [Theory]
        [InlineData(typeof(object))]
        public void FastMethodAssemblerTests_MethodsAreNotChanged_WholeAssemblies_AgainstDnlib(Type type)
        {
            var moduleDef = AssemblyDef.Load(type.Assembly.Location).ManifestModule;
            var moduleRef = type.Assembly.ManifestModule;

            // Initialize module metadata
            var constants = new UniqueChunkList<ByteArrayChunk>();
            var methodBodies = new MethodBodyChunks(false);
            var netResources = new NetResources(alignment: 4);
            var options = new MetadataOptions() { Flags = MetadataFlags.PreserveAll };
            var metadata = dnlib.DotNet.Writer.Metadata.Create(moduleDef, constants, methodBodies, netResources, options);
            metadata.CreateTables();

            // Check all types
            foreach (var typeRef in moduleRef.GetTypes().Where(t => t is not null))
            {
                // Check all methods
                var typeDef = moduleDef.ResolveToken(typeRef.MetadataToken) as TypeDef;
                if (typeDef is null)
                    continue;

                foreach (var methodDef in typeDef.Methods.Where(m => m.HasBody))
                {
                    // Our result
                    var assembler = new FastMethodAssembler(methodDef, new Dictionary<Instruction, MDToken>(), new StringHeapCache(), false, false);
                    var ourBytecode = assembler.Assemble();

                    var writer = new MethodBodyWriter(metadata, methodDef.Body);
                    writer.Write();
                    var correctBytecode = writer.GetFullMethodBody();

                    // Check header + method body
                    EnsureMethodBodyIsSameDnlib(methodDef, correctBytecode, ourBytecode);
                }
            }
        }

        private void CheckAllMethodsReflection(Type typeRef, TypeDef typeDef)
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

                EnsureMethodBodyIsSameReflection(methodDef, expected, actual);
            }
        }

        private static void EnsureMethodBodyIsSameReflection(MethodDef methodDef, Span<byte> correctResult, Span<byte> ourResult)
        {
            var isTinyHeader = (ourResult[0] & 0x03) != 3;
            var headerSize = (isTinyHeader) ? 1 : 12;
            ourResult = (isTinyHeader) ? ourResult[1..] : ourResult[12..];

            for (var index = 0; index < correctResult.Length; index++)
            {
                var instruction = methodDef.Body.Instructions.Last(instr => instr.Offset <= index);
                if (correctResult[index] != ourResult[index])
                    throw new Exception($"Wrongly assembled instruction {instruction}");
            }
        }

        private static void EnsureMethodBodyIsSameDnlib(MethodDef methodDef, Span<byte> correctResult, Span<byte> ourResult)
        {
            var index = 0;
            var isTinyHeader = (ourResult[0] & 0x03) != 3;
            var headerSize = (isTinyHeader) ? 1 : 12;
            var lastInstruction = methodDef.Body.Instructions.Last();
            var additionalSectionsStart = lastInstruction.Offset + lastInstruction.GetSize();

            // Check header
            Assert.Equal(correctResult[..headerSize].ToArray(), ourResult[..headerSize].ToArray());
            index += headerSize;

            // Check instructions
            for (; index < additionalSectionsStart; index++)
            {
                var instruction = methodDef.Body.Instructions.Last(instr => instr.Offset <= index);
                if (correctResult[index] != ourResult[index])
                    throw new Exception($"Wrongly assembled instruction {instruction}");
            }

            // Check handler blocks
            for (; index < correctResult.Length; index++)
            {
                if (correctResult[index] != ourResult[index])
                    throw new Exception($"Wrongly assembled handler block");
            }
        }
    }
}
