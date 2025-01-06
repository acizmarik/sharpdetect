// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.MethodDescriptors.Arguments;

public readonly record struct CapturedArgumentDescriptor(byte Index, CapturedValueDescriptor Value);
