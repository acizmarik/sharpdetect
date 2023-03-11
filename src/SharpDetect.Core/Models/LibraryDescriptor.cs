// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.LibraryDescriptors;

namespace SharpDetect.Core.Models
{
    internal class LibraryDescriptor : ILibraryDescriptor
    {
        public bool IsCoreLibrary { get; }
        public string AssemblyName { get; }
        public IReadOnlyList<(MethodIdentifier Identifier, MethodInterpretationData Data)> Methods { get; }

        public LibraryDescriptor(string name, bool isCoreLib, List<(MethodIdentifier, MethodInterpretationData)> methods)
        {
            AssemblyName = name;
            IsCoreLibrary = isCoreLib;
            Methods = methods;
        }
    }
}
