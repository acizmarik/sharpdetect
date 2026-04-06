// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Core.Plugins;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PluginMetadataAttribute : Attribute
{
    public required string Name { get; init; }
    public required string Description { get; init; }
}

