// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Plugins.Metadata
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginExportAttribute : Attribute
    {
        public readonly string Name;
        public readonly string? Version;

        public PluginExportAttribute(string name, string? version = default)
        {
            Name = name;
            Version = version;
        }
    }
}
