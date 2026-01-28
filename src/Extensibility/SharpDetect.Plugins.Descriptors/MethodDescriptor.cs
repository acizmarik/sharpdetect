// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public record MethodDescriptor(
    string MethodName,
    string DeclaringTypeFullName,
    MethodVersionDescriptor? VersionDescriptor,
    MethodSignatureDescriptor SignatureDescriptor,
    MethodRewritingDescriptor RewritingDescriptor);
