// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

namespace SharpDetect.Common.Plugins.Metadata
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginDiagnosticsCategoriesAttribute : Attribute
    {
        public readonly string[] Categories;

        public PluginDiagnosticsCategoriesAttribute(string[] categories)
        {
            Categories = categories;
        }
    }
}
