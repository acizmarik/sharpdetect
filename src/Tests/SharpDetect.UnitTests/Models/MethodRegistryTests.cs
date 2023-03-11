// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using SharpDetect.Common;
using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Core.Models;
using SharpDetect.Core.Scripts;
using Xunit;

namespace SharpDetect.UnitTests.Models
{
    public class MethodRegistryTests : TestsBase
    {
        [Fact]
        public async Task MethodRegistry_TryGetMethodInterpretationData_Match()
        {
            // Prepare
            var registry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var moduleDef = AssemblyDef.Load(typeof(object).Assembly.Location).ManifestModule;
            var typeDef = moduleDef.Types.Single(t => t.ReflectionFullName == typeof(Monitor).FullName);
            var methodDef = typeDef.Methods.Single(m => m.Name == nameof(Monitor.Exit));
            var interpretation = MethodInterpretation.LockRelease;
            var rewriting = MethodRewritingFlags.InjectManagedWrapper |
                            MethodRewritingFlags.InjectEntryExitHooks |
                            MethodRewritingFlags.CaptureArguments;

            // Act & Assert
            Assert.True(registry.TryGetMethodInterpretationData(methodDef, out var interpretationData));
            Assert.NotNull(interpretationData);
            Assert.Equal(interpretation, interpretationData!.Interpretation);
            Assert.Equal(rewriting, interpretationData!.Flags);
            Assert.Single(interpretationData.CapturedParams);
        }

        [Fact]
        public async Task MethodRegistry_TryGetMethodInterpretationData_NoMatch()
        {
            // Prepare
            var registry = await CreateRegistryForModulesAsync("Modules/system.private.corelib.lua");
            var moduleDef = AssemblyDef.Load(typeof(object).Assembly.Location).ManifestModule;
            var typeDef = moduleDef.Types.Single(t => t.ReflectionFullName == typeof(object).FullName);
            var methodDef = typeDef.Methods.Single(m => m.Name == nameof(object.GetHashCode));

            // Act & Assert
            Assert.False(registry.TryGetMethodInterpretationData(methodDef, out var interpretationData));
            Assert.Null(interpretationData);
        }
    }
}
