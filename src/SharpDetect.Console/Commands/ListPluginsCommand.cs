// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.DependencyInjection;
using SharpDetect.Common.Services;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace SharpDetect.Console.Commands
{
    internal class ListPluginsCommand : CommandBase
    {
        public override ICommandHandler Handler { get; }
        public override IReadOnlyList<Argument> Arguments { get; }

        public ListPluginsCommand()
            : base(name: "plugins", description: "List all available plugins.")
        {
            Arguments = new List<Argument>();
            Handler = CommandHandler.Create(HandlerImplementationAsync);
        }

        private async Task HandlerImplementationAsync()
        {
            var configuration = Program.CreateConfiguration();
            var serviceProvider = BuildServices(configuration);
            var pluginsManager = serviceProvider.GetRequiredService<IPluginsManager>();
            await pluginsManager.LoadPluginsAsync(CancellationToken.None);

            System.Console.WriteLine("List of available plugins: ");
            foreach (var plugin in pluginsManager.GetLoadedPluginInfos())
                System.Console.WriteLine($"{plugin.Name}; Version={plugin.Version}; Path={plugin.FilePath}");
        }
    }
}
