// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Extensibility.Descriptors;

public record MethodDescriptor(
    string MethodName,
    string DeclaringTypeFullName,
    string AssemblyName,
    string? ReferenceAssembly,
    MethodSignatureDescriptor SignatureDescriptor,
    MethodRewritingDescriptor RewritingDescriptor);
