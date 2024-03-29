﻿// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Logging.Abstractions;
using SharpDetect.Core.Runtime.Memory;
using SharpDetect.Profiler;
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
            var shadowGC = new ShadowGC(new NullLoggerFactory());

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
            var shadowGC = new ShadowGC(new NullLoggerFactory());

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
            var shadowGC = new ShadowGC(new NullLoggerFactory());
            var generations = new bool[3] { true /* GEN0 */, false /* GEN1 */, false /* GEN2 */ };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[]
            {
                /*   We have a heap block:
                 *   - starting at an address 8
                 *   - with length 32
                 *   - reserved length 64 (max size)
                 */
                new(generation: 0, 
                    rangeStart: new(8), 
                    rangeLength: new(32),
                    rangeLengthReserved: new(64))
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
                lengths: new uint[]
                {
                    8,
                    8
                });
            shadowGC.ProcessGarbageCollectionFinished(bounds);

            // Assert
            Assert.True(obj1.IsAlive);
            Assert.False(obj2.IsAlive);
            Assert.True(obj3.IsAlive);
        }

        [Fact]
        public static void ShadowMemory_GC_TrackingMovedObjects_Liveness()
        {
            // Prepare
            var shadowGC = new ShadowGC(new NullLoggerFactory());
            var generations = new bool[3] { true /* GEN0 */, false /* GEN1 */, false /* GEN2 */ };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[]
            {
                /*   We have a heap block:
                 *   - starting at an address 8
                 *   - with length 20
                 *   - reserved length 64 (max size)
                 */
                new(generation: 0,
                    rangeStart: new(7),
                    rangeLength: new(20),
                    rangeLengthReserved: new(64))
            };

            // Act

            // Step 1: fill heap
            // Example from: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/profiling.md
            var obj1 = shadowGC.GetObject(new UIntPtr(8));
            var obj2 = shadowGC.GetObject(new UIntPtr(9));
            var obj3 = shadowGC.GetObject(new UIntPtr(10));
            var obj4 = shadowGC.GetObject(new UIntPtr(12));
            var obj5 = shadowGC.GetObject(new UIntPtr(13));
            var obj6 = shadowGC.GetObject(new UIntPtr(15));
            var obj7 = shadowGC.GetObject(new UIntPtr(16));
            var obj8 = shadowGC.GetObject(new UIntPtr(17));
            var obj9 = shadowGC.GetObject(new UIntPtr(18));
            var obj10 = shadowGC.GetObject(new UIntPtr(19));

            // Step 2: clear and move survivors objects
            shadowGC.ProcessGarbageCollectionStarted(bounds, generations);
            shadowGC.ProcessMovedReferences(
                oldBlockStarts: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(10),
                    new UIntPtr(15)
                },
                newBlockStarts: new[]
                {
                    new UIntPtr(7),
                    new UIntPtr(8),
                    new UIntPtr(11)
                },
                lengths: new uint[]
                {
                    1,
                    3,
                    4
                }
            );
            bounds[0] = new(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_1, bounds[0].RangeStart, bounds[0].RangeLength, bounds[0].RangeLengthReserved);
            shadowGC.ProcessGarbageCollectionFinished(bounds);

            // Assert
            // Object liveness
            Assert.True(obj1.IsAlive);
            Assert.False(obj2.IsAlive);
            Assert.True(obj3.IsAlive);
            Assert.True(obj4.IsAlive);
            Assert.False(obj5.IsAlive);
            Assert.True(obj6.IsAlive);
            Assert.True(obj7.IsAlive);
            Assert.True(obj8.IsAlive);
            Assert.True(obj9.IsAlive);
            Assert.False(obj10.IsAlive);
        }

        [Fact]
        public static void ShadowMemory_GC_TrackingMovedObjects_Movement()
        {
            // Prepare
            var shadowGC = new ShadowGC(new NullLoggerFactory());
            var generations = new bool[3] { true /* GEN0 */, false /* GEN1 */, false /* GEN2 */ };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[]
            {
                /*   We have a heap block:
                 *   - starting at an address 8
                 *   - with length 20
                 *   - reserved length 64 (max size)
                 */
                new(generation: 0,
                    rangeStart: new(7),
                    rangeLength: new(20),
                    rangeLengthReserved: new(64))
            };

            // Act

            // Step 1: fill heap
            // Example from: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/profiling.md
            var obj1 = shadowGC.GetObject(new UIntPtr(8));
            var obj2 = shadowGC.GetObject(new UIntPtr(9));
            var obj3 = shadowGC.GetObject(new UIntPtr(10));
            var obj4 = shadowGC.GetObject(new UIntPtr(12));
            var obj5 = shadowGC.GetObject(new UIntPtr(13));
            var obj6 = shadowGC.GetObject(new UIntPtr(15));
            var obj7 = shadowGC.GetObject(new UIntPtr(16));
            var obj8 = shadowGC.GetObject(new UIntPtr(17));
            var obj9 = shadowGC.GetObject(new UIntPtr(18));
            var obj10 = shadowGC.GetObject(new UIntPtr(19));

            // Step 2: clear and move survivors objects
            shadowGC.ProcessGarbageCollectionStarted(bounds, generations);
            shadowGC.ProcessMovedReferences(
                oldBlockStarts: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(10),
                    new UIntPtr(15)
                },
                newBlockStarts: new[]
                {
                    new UIntPtr(7),
                    new UIntPtr(8),
                    new UIntPtr(11)
                },
                lengths: new uint[]
                {
                    1,
                    3,
                    4
                }
            );
            bounds[0] = new(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_1, bounds[0].RangeStart, bounds[0].RangeLength, bounds[0].RangeLengthReserved);
            shadowGC.ProcessGarbageCollectionFinished(bounds);

            // Assert
            // Movement tracking
            Assert.Equal(new UIntPtr(7), obj1.ShadowPointer);
            Assert.Equal(new UIntPtr(8), obj3.ShadowPointer);
            Assert.Equal(new UIntPtr(10), obj4.ShadowPointer);
            Assert.Equal(new UIntPtr(11), obj6.ShadowPointer);
            Assert.Equal(new UIntPtr(12), obj7.ShadowPointer);
            Assert.Equal(new UIntPtr(13), obj8.ShadowPointer);
            Assert.Equal(new UIntPtr(14), obj9.ShadowPointer);
        }

        [Theory]
        [InlineData(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0, COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_0)]
        [InlineData(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_1, COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_1)]
        public static void ShadowMemory_GC_TrackingMovedObjects_Generation(COR_PRF_GC_GENERATION generationFrom, COR_PRF_GC_GENERATION generationTo)
        {
            // Prepare
            var shadowGC = new ShadowGC(new NullLoggerFactory());
            var generations = new bool[3] { true /* GEN0 */, false /* GEN1 */, false /* GEN2 */ };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[]
            {
                /*   We have a heap block:
                 *   - starting at an address 8
                 *   - with length 20
                 *   - reserved length 64 (max size)
                 */
                new(generation: generationFrom,
                    rangeStart: new(7),
                    rangeLength: new(20),
                    rangeLengthReserved: new(64))
            };

            // Act

            // Step 1: fill heap
            // Example from: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/profiling.md
            var obj1 = shadowGC.GetObject(new UIntPtr(8));
            var obj2 = shadowGC.GetObject(new UIntPtr(9));
            var obj3 = shadowGC.GetObject(new UIntPtr(10));
            var obj4 = shadowGC.GetObject(new UIntPtr(12));
            var obj5 = shadowGC.GetObject(new UIntPtr(13));
            var obj6 = shadowGC.GetObject(new UIntPtr(15));
            var obj7 = shadowGC.GetObject(new UIntPtr(16));
            var obj8 = shadowGC.GetObject(new UIntPtr(17));
            var obj9 = shadowGC.GetObject(new UIntPtr(18));
            var obj10 = shadowGC.GetObject(new UIntPtr(19));

            // Step 2: clear and move survivors objects
            shadowGC.ProcessGarbageCollectionStarted(bounds, generations);
            shadowGC.ProcessMovedReferences(
                oldBlockStarts: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(10),
                    new UIntPtr(15)
                },
                newBlockStarts: new[]
                {
                    new UIntPtr(7),
                    new UIntPtr(8),
                    new UIntPtr(11)
                },
                lengths: new uint[]
                {
                    1,
                    3,
                    4
                }
            );
            bounds[0] = new(generationTo, bounds[0].RangeStart, bounds[0].RangeLength, bounds[0].RangeLengthReserved);
            shadowGC.ProcessGarbageCollectionFinished(bounds);

            // Assert
            // Movement tracking
            Assert.Equal(generationTo, shadowGC.GetObjectGeneration(obj1));
            Assert.Equal(generationTo, shadowGC.GetObjectGeneration(obj3));
            Assert.Equal(generationTo, shadowGC.GetObjectGeneration(obj4));
            Assert.Equal(generationTo, shadowGC.GetObjectGeneration(obj6));
            Assert.Equal(generationTo, shadowGC.GetObjectGeneration(obj7));
            Assert.Equal(generationTo, shadowGC.GetObjectGeneration(obj8));
            Assert.Equal(generationTo, shadowGC.GetObjectGeneration(obj9));
        }

        [Fact]
        public static void ShadowMemory_GC_TrackingMovedObjects_KeepsReferences()
        {
            // Prepare
            var shadowGC = new ShadowGC(new NullLoggerFactory());
            var generations = new bool[3] { true /* GEN0 */, false /* GEN1 */, false /* GEN2 */ };
            var bounds = new COR_PRF_GC_GENERATION_RANGE[]
            {
                /*   We have a heap block:
                 *   - starting at an address 8
                 *   - with length 20
                 *   - reserved length 64 (max size)
                 */
                new(generation: 0,
                    rangeStart: new(7),
                    rangeLength: new(20),
                    rangeLengthReserved: new(64))
            };

            // Act

            // Step 1: fill heap
            // Example from: https://github.com/dotnet/runtime/blob/main/docs/design/coreclr/botr/profiling.md
            var obj1 = shadowGC.GetObject(new UIntPtr(8));
            var obj2 = shadowGC.GetObject(new UIntPtr(9));
            var obj3 = shadowGC.GetObject(new UIntPtr(10));
            var obj4 = shadowGC.GetObject(new UIntPtr(12));
            var obj5 = shadowGC.GetObject(new UIntPtr(13));
            var obj6 = shadowGC.GetObject(new UIntPtr(15));
            var obj7 = shadowGC.GetObject(new UIntPtr(16));
            var obj8 = shadowGC.GetObject(new UIntPtr(17));
            var obj9 = shadowGC.GetObject(new UIntPtr(18));
            var obj10 = shadowGC.GetObject(new UIntPtr(19));

            // Step 2: clear and move survivors objects
            shadowGC.ProcessGarbageCollectionStarted(bounds, generations);
            shadowGC.ProcessMovedReferences(
                oldBlockStarts: new[]
                {
                    new UIntPtr(8),
                    new UIntPtr(10),
                    new UIntPtr(15)
                },
                newBlockStarts: new[]
                {
                    new UIntPtr(7),
                    new UIntPtr(8),
                    new UIntPtr(11)
                },
                lengths: new uint[]
                {
                    1,
                    3,
                    4
                }
            );
            bounds[0] = new(COR_PRF_GC_GENERATION.COR_PRF_GC_GEN_1, bounds[0].RangeStart, bounds[0].RangeLength, bounds[0].RangeLengthReserved);
            shadowGC.ProcessGarbageCollectionFinished(bounds);

            // Assert
            // Correct updates
            Assert.Equal(obj1, shadowGC.GetObject(new UIntPtr(7)));
            Assert.Equal(obj3, shadowGC.GetObject(new UIntPtr(8)));
            Assert.Equal(obj4, shadowGC.GetObject(new UIntPtr(10)));
            Assert.Equal(obj6, shadowGC.GetObject(new UIntPtr(11)));
            Assert.Equal(obj7, shadowGC.GetObject(new UIntPtr(12)));
            Assert.Equal(obj8, shadowGC.GetObject(new UIntPtr(13)));
            Assert.Equal(obj9, shadowGC.GetObject(new UIntPtr(14)));
        }
    }
}
