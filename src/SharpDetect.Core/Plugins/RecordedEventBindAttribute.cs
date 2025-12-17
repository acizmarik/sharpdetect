// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Plugins;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class RecordedEventBindAttribute(ushort value) : Attribute
{
    public readonly ushort Value = value;
}
