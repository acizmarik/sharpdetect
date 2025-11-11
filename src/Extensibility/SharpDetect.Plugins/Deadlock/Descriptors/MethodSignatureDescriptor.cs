// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;
using SharpDetect.Plugins.Deadlock.Descriptors;

namespace SharpDetect.Core.Plugins.Descriptors;

public record MethodSignatureDescriptor(
    CorCallingConvention CallingConvention,
    byte ParametersCount,
    ArgumentTypeDescriptor ReturnType,
    ArgumentTypeDescriptor[] ArgumentTypeElements);
