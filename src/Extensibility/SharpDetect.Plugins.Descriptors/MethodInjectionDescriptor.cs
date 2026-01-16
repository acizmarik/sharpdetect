// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using SharpDetect.Core.Events;

namespace SharpDetect.Plugins.Descriptors;

public sealed record MethodInjectionDescriptor(
    string Name,
    RecordedEventType EventType,
    MethodSignatureDescriptor Signature);