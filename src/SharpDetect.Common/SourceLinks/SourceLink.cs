// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Pdb;

namespace SharpDetect.Common.SourceLinks
{
    public record SourceLink(ulong Id, AnalysisEventType Type, MethodDef Method, Instruction Instruction, SequencePoint? SequencePoint = null);
}
