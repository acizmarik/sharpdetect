using dnlib.DotNet;
using SharpDetect.Dnlib.Extensions;
using System.Runtime.CompilerServices;
using Xunit;

namespace SharpDetect.UnitTests.DnlibExtensions
{
    public class StringHeapCacheTests
    {
        [Fact]
        public void StringHeapCacheTests_EmptyHeap()
        {
            // Prepare
            var cache = new StringHeapCache();
            var module = (AssemblyDef.Load(typeof(Unsafe).Assembly.Location).ManifestModule as ModuleDefMD)!;
            
            // Act
            var lookup = cache.GetAllOffsets(module);

            // Assert
            Assert.Empty(lookup);
        }

        [Theory]
        [InlineData(typeof(Console) /* System.Console.dll */)]
        [InlineData(typeof(object) /* System.Private.CoreLib.dll */)]
        public void StringHeapCacheTests_CorrectlyConstructed(Type type)
        {
            // Prepare
            var cache = new StringHeapCache();
            var module = (AssemblyDef.Load(type.Assembly.Location).ManifestModule as ModuleDefMD)!;
            
            // Act
            var lookup = cache.GetAllOffsets(module);
            
            // Assert
            foreach (var (str, index) in lookup)
                Assert.Equal(str, module.ReadUserString(index.Raw));
        }
    }
}
