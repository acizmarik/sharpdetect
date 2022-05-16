using SharpDetect.Common.Interop;
using SharpDetect.Core.Runtime.Memory;
using Xunit;

namespace SharpDetect.UnitTests.Runtime.Memory
{
    public class ShadowMemoryTests
    {
        [Fact]
        public static void ShadowMemory_GetObject()
        {
            // Prepare
            var ptr = new UIntPtr(123);
            var shadowGC = new ShadowGC();

            // Act
            var obj = shadowGC.GetObject(ptr);

            // Assert
            Assert.NotNull(obj);
            Assert.True(obj.IsAlive);
            Assert.Equal(ptr, obj.ShadowPointer);
        }

        [Fact]
        public static void ShadowMemory_GetObject_ReferenceEqualsForSamePointers()
        {
            // Prepare
            var ptr = new UIntPtr(123);
            var shadowGC = new ShadowGC();

            // Act
            var obj1 = shadowGC.GetObject(ptr);
            var obj2 = shadowGC.GetObject(ptr);

            // Assert
            Assert.Same(obj1, obj2);
        }

        [Fact]
        public static void ShadowMemory_GC_SimpleNonCompacting()
        {
            // Prepare
            var shadowGC = new ShadowGC();
            var generations = new bool[3] { true /* GEN0 */, false /* GEN1 */, false /* GEN2 */ };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[]
            {
                new() 
                {
                    /*   We have a heap block:
                     *   - starting at an address 8
                     *   - with length 32
                     *   - reserved length 64 (max size)
                     */
                    generation = 0, 
                    rangeStart = new(8), 
                    rangeLength = new(32),
                    rangeLengthReserved = new(64)
                }
            };

            // Act

            // Step 1: fill heap
            var obj1 = shadowGC.GetObject(new UIntPtr(8));
            var obj2 = shadowGC.GetObject(new UIntPtr(16));
            var obj3 = shadowGC.GetObject(new UIntPtr(24));

            // Step 2: delete reference to the second object
            shadowGC.ProcessGarbageCollectionStarted(bounds, generations);
            shadowGC.ProcessSurvivingReferences(
                survivingBlockStarts: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(24)
                },
                lengths: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(8)
                });
            shadowGC.ProcessGarbageCollectionFinished(bounds);

            // Assert
            Assert.True(obj1.IsAlive);
            Assert.False(obj2.IsAlive);
            Assert.True(obj3.IsAlive);
        }

        [Fact]
        public static void ShadowMemory_GC_SimpleCompacting()
        {
            // Prepare
            var shadowGC = new ShadowGC();
            var generations = new bool[3] { true /* GEN0 */, false /* GEN1 */, false /* GEN2 */ };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[]
            {
                new()
                {
                    /*   We have a heap block:
                     *   - starting at an address 8
                     *   - with length 32
                     *   - reserved length 64 (max size)
                     */
                    generation = 0,
                    rangeStart = new(8),
                    rangeLength = new(32),
                    rangeLengthReserved = new(64)
                }
            };

            // Act

            // Step 1: fill heap
            var obj1 = shadowGC.GetObject(new UIntPtr(8));
            var obj2 = shadowGC.GetObject(new UIntPtr(16));
            var obj3 = shadowGC.GetObject(new UIntPtr(24));

            // Step 2: delete reference to the second object
            shadowGC.ProcessGarbageCollectionStarted(bounds, generations);
            shadowGC.ProcessSurvivingReferences(
                survivingBlockStarts: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(24)
                },
                lengths: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(8)
                });
            shadowGC.ProcessGarbageCollectionFinished(bounds);

            // Assert
            Assert.True(obj1.IsAlive);
            Assert.False(obj2.IsAlive);
            Assert.True(obj3.IsAlive);
        }
    }
}
