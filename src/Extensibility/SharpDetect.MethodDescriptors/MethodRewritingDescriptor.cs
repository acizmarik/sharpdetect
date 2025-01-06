// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿using SharpDetect.MethodDescriptors.Arguments;

namespace SharpDetect.MethodDescriptors;

public record MethodRewritingDescriptor(
    bool InjectHooks,
    bool InjectManagedWrapper,
    CapturedArgumentDescriptor[]? Arguments,
    CapturedValueDescriptor? ReturnValue,
    ushort? MethodEnterInterpretation,
    ushort? MethodExitInterpretation);
