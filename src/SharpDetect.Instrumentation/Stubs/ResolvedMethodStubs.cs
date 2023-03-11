// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SharpDetect.Instrumentation.Stubs
{
    public class ResolvedMethodStubs : Dictionary<Instruction, MDToken>
    {
    }
}
