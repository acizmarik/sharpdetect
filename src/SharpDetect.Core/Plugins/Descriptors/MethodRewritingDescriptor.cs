// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Plugins.Descriptors;

public record MethodRewritingDescriptor(
    bool InjectHooks,
    bool InjectManagedWrapper,
    CapturedArgumentDescriptor[]? Arguments,
    CapturedValueDescriptor? ReturnValue,
    ushort? MethodEnterInterpretation,
    ushort? MethodExitInterpretation);
