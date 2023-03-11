// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.LibraryDescriptors
{
    public interface ILibraryDescriptor
    {
        bool IsCoreLibrary { get; }
        string AssemblyName { get; }
        IReadOnlyList<(MethodIdentifier Identifier, MethodInterpretationData Data)> Methods { get; }
    }
}
