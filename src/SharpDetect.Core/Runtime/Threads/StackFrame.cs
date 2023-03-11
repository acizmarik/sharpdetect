// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Common;

namespace SharpDetect.Core.Runtime.Threads
{
    internal record struct StackFrame(FunctionInfo FunctionInfo, MethodInterpretation Interpretation, object? Arguments);
}
