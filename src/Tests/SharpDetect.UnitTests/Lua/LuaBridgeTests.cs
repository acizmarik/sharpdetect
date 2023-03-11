// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.LibraryDescriptors;
using SharpDetect.Core.Scripts;
using Xunit;

namespace SharpDetect.UnitTests.Lua
{
    public class LuaBridgeTests : TestsBase
    {
        private const string moduleDescriptorsDirectory = "Modules";
        private const string coreLibDescriptorPath = moduleDescriptorsDirectory + "/system.private.corelib.lua";

        [Fact]
        public void LuaBridge_ReadsModulePaths()
        {
            // Prepare & Act
            var luaBridge = new LuaBridge(CreateModulesConfiguration());

            // Assert
            Assert.NotEmpty(luaBridge.ModuleDirectories);
        }

        [Fact]
        public async Task LuaBridge_LoadsScript()
        {
            // Prepare
            var luaBridge = new LuaBridge(CreateModulesConfiguration());

            // Act
            var script = await luaBridge.LoadModuleAsync(coreLibDescriptorPath);

            // Assert
            Assert.NotNull(script);
        }

        [Fact]
        public async Task LuaBridge_CreatesAssemblyDescriptor()
        {
            // Prepare
            var luaBridge = new LuaBridge(CreateModulesConfiguration());

            // Act
            var script = await luaBridge.LoadModuleAsync(coreLibDescriptorPath);
            var descriptor = luaBridge.CreateAssemblyDescriptor(script);

            // Assert
            Assert.NotEqual(default, descriptor);
        }

        [Fact]
        public async Task LuaBridge_ReadsAssemblyName()
        {
            // Prepare
            var luaBridge = new LuaBridge(CreateModulesConfiguration());

            // Act
            var script = await luaBridge.LoadModuleAsync(coreLibDescriptorPath);
            var descriptor = luaBridge.CreateAssemblyDescriptor(script);
            var name = descriptor.GetAssemblyName();

            // Assert
            Assert.Equal("System.Private.CoreLib", name);
        }

        [Fact]
        public async Task LuaBridge_ReadsIsCoreLibFlag()
        {
            // Prepare
            var luaBridge = new LuaBridge(CreateModulesConfiguration());

            // Act
            var script = await luaBridge.LoadModuleAsync(coreLibDescriptorPath);
            var descriptor = luaBridge.CreateAssemblyDescriptor(script);
            var isCoreLib = descriptor.IsCoreLibrary();

            // Assert
            Assert.True(isCoreLib);
        }

        [Fact]
        public async Task LuaBridge_ReadsMethodDescriptors()
        {
            // Prepare
            var luaBridge = new LuaBridge(CreateModulesConfiguration());
            var methods = new List<(MethodIdentifier, MethodInterpretationData)>();

            // Act
            var script = await luaBridge.LoadModuleAsync(coreLibDescriptorPath);
            var descriptor = luaBridge.CreateAssemblyDescriptor(script);
            descriptor.GetMethodDescriptors(methods);

            // Assert
            Assert.NotEmpty(methods);
        }
    }
}
