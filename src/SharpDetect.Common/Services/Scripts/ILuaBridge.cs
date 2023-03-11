// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using MoonSharp.Interpreter;
using SharpDetect.Common.Scripts;

namespace SharpDetect.Common.Services.Scripts
{
    public interface ILuaBridge
    {
        string[] ModuleDirectories { get; }

        Task<Script> LoadModuleAsync(string path);

        AssemblyDescriptorScript CreateAssemblyDescriptor(Script script);
    }
}
