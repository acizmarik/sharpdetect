// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Plugins.Descriptors;

public readonly record struct CapturedArgumentDescriptor(byte Index, CapturedValueDescriptor Value);
