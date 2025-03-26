// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Plugins.Descriptors;

public record MethodDescriptor(
    string MethodName,
    string DeclaringTypeFullName,
    MethodSignatureDescriptor SignatureDescriptor,
    MethodRewritingDescriptor RewritingDescriptor);
