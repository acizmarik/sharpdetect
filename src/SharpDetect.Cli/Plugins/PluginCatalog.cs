// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using System.Reflection;
using SharpDetect.Core.Plugins;

namespace SharpDetect.Cli.Plugins;

internal sealed record PluginDescriptor(string Name, string Description, Type Type);

internal static class PluginCatalog
{
    public static Assembly LoadAssembly(string assemblyPath)
    {
        try
        {
            return Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
        }
        catch (Exception e)
        {
            throw new ArgumentException($"Could not load plugin assembly \"{assemblyPath}\": {e.Message}", e);
        }
    }

    public static IReadOnlyList<PluginDescriptor> GetPlugins(Assembly assembly)
    {
        return assembly
            .GetTypes().Concat(assembly.GetForwardedTypes())
            .Select(type => (Type: type, Metadata: type.GetCustomAttribute<PluginMetadataAttribute>()))
            .Where(candidate => candidate.Metadata is not null)
            .Select(candidate => new PluginDescriptor(candidate.Metadata!.Name, candidate.Metadata.Description, candidate.Type))
            .OrderBy(plugin => plugin.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
