// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.Events.Descriptors.Profiler;

namespace SharpDetect.Extensibility.Descriptors;

public record MethodSignatureDescriptor(
    CorCallingConvention CallingConvention,
    byte ParametersCount,
    CorElementType ReturnType,
    CorElementType[] ArgumentTypeElements);
