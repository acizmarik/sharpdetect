// Copyright 2023 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace SharpDetect.Console.Commands
{
    internal abstract class CommandBase
    {
        public string Name { get; }
        public string Description { get; }
        public abstract ICommandHandler Handler { get; }
        public abstract IReadOnlyList<Argument> Arguments { get; }

        protected IServiceProvider BuildServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();
            Program.ConfigureCommonServices(services, configuration);
            Program.ConfigureCliSpecificServices(services);
            return services.BuildServiceProvider();
        }

        public CommandBase(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
