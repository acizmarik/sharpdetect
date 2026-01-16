// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public record MethodRewritingDescriptor(
    bool InjectHooks,
    bool InjectManagedWrapper,
    CapturedArgumentDescriptor[]? Arguments,
    CapturedValueDescriptor? ReturnValue,
    ushort? MethodEnterInterpretation,
    ushort? MethodExitInterpretation);
