// Copyright 2026 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using CliFx;
using CliFx.Binding;
using CliFx.Infrastructure;
using SharpDetect.Cli.Plugins;
using SharpDetect.Worker.Commands.Run;
using SharpDetect.Worker.Configuration;

namespace SharpDetect.Cli.Commands;

[Command("plugins", Description = "Lists the analysis plugins that can be passed to --plugin")]
public sealed partial class PluginsCommand : ICommand
{
    [CommandOption("plugin-assembly", Description = "Plugin assembly to inspect. Default: the plugins shipped with SharpDetect.")]
    public string? PluginAssembly { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var assemblyPath = PluginAssembly
            ?? EnvironmentUtils.ExpandEnvironmentVariablesForPath(AnalysisPluginConfigurationArgs.DefaultPath);

        IReadOnlyList<PluginDescriptor> plugins;
        try
        {
            plugins = PluginCatalog.GetPlugins(PluginCatalog.LoadAssembly(assemblyPath));
        }
        catch (Exception exception)
        {
            throw new CommandException(
                message: ExceptionMessages.Flatten(exception),
                exitCode: (int)ExitCode.ConfigurationError,
                innerException: exception);
        }

        if (plugins.Count == 0)
        {
            await console.Output.WriteLineAsync($"No plugins found in \"{assemblyPath}\".");
            return;
        }

        var nameWidth = plugins.Max(plugin => plugin.Name.Length);
        foreach (var plugin in plugins)
            await console.Output.WriteLineAsync($"{plugin.Name.PadRight(nameWidth)}  {plugin.Description} ({plugin.Type.FullName})");
    }
}
