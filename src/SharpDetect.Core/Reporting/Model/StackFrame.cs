// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.Core.Reporting.Model;

public record StackFrame(string MethodName, string SourceMapping, int MethodToken);
