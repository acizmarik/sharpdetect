// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events.Profiler;

namespace SharpDetect.Core.Plugins.Descriptors;

public record MethodSignatureDescriptor(
    CorCallingConvention CallingConvention,
    byte ParametersCount,
    CorElementType ReturnType,
    CorElementType[] ArgumentTypeElements);
