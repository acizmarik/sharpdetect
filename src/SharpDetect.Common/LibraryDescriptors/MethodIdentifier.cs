// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common.Utilities;

namespace SharpDetect.Common.LibraryDescriptors
{
    public record struct MethodIdentifier(string Name, string DeclaringType, bool IsStatic, ushort ArgsCount, ValueCollection<string> ArgumentTypes, bool IsInjected);
}
