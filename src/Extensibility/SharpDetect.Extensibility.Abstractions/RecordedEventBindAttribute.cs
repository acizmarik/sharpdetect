// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

﻿namespace SharpDetect.Extensibility.Abstractions;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class RecordedEventBindAttribute(ushort value) : Attribute
{
    public readonly ushort Value = value;
}
