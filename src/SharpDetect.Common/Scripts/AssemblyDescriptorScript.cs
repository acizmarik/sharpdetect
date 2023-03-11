// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CommunityToolkit.Diagnostics;
using MoonSharp.Interpreter;
using SharpDetect.Common.Exceptions;
using SharpDetect.Common.LibraryDescriptors;

namespace SharpDetect.Common.Scripts
{
    public readonly struct AssemblyDescriptorScript
    {
        private const string assemblyNameIdentifier = "assemblyName";
        private const string isCoreLibraryIdentifier = "isCoreLibrary";
        private const string methodDescriptorsFillerIdentifier = "getMethodDescriptors";
        private readonly Script script;

        public AssemblyDescriptorScript(Script script)
        {
            this.script = script;
        }

        public string GetAssemblyName()
        {
            var tableEntry = script.Globals.Get(assemblyNameIdentifier);
            Guard.IsTrue(tableEntry.IsNotNil());

            return tableEntry.String;
        }

        public bool IsCoreLibrary()
        {
            var tableEntry = script.Globals.Get(isCoreLibraryIdentifier);
            Guard.IsTrue(tableEntry.IsNotNil());

            return tableEntry.Boolean;
        }

        public void GetMethodDescriptors(List<(MethodIdentifier, MethodInterpretationData)> methods)
        {
            var tableEntry = script.Globals.Get(methodDescriptorsFillerIdentifier);
            Guard.IsTrue(tableEntry.IsNotNil());

            tableEntry.Function.Call(methods);
        }
    }
}
