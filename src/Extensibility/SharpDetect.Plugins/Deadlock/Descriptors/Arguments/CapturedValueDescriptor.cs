// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Plugins.Descriptors;

public readonly record struct CapturedValueDescriptor(byte Size, CapturedValue Flags);
