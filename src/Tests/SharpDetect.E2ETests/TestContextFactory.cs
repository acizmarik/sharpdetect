// Copyright 2025 Andrej Čižmárik and Contributors
// SPDX-License-Identifier: Apache-2.0

using Meziantou.Extensions.Logging.Xunit;
using Microsoft.Extensions.Logging;
using SharpDetect.Worker.Commands;
using SharpDetect.Worker.Commands.Run;
using SharpDetect.Worker.Configuration;
using Xunit.Abstractions;

namespace SharpDetect.E2ETests;

internal static class TestContextFactory
{
    public static IServiceProvider CreateServiceProvider(string filename, ITestOutputHelper output)
    {
        var args = CommandDeserializer.DeserializeCommandArguments<RunCommandArgs>(filename);
        var pluginType = Type.GetType(args.Analysis.FullTypeName)
                         ?? typeof(Plugins.ConcurrencyContext).Assembly.GetType(args.Analysis.FullTypeName)
                         ?? throw new InvalidOperationException($"Could not find analysis plugin type {args.Analysis.FullTypeName}.");
            
        return new AnalysisServiceProviderBuilder(args)
            .WithTimeProvider(TimeProvider.System)
            .WithPlugin(pluginType)
            .ConfigureLogging(logging =>
            {
                logging.AddProvider(new XUnitLoggerProvider(output));
                logging.SetMinimumLevel(args.Analysis.LogLevel);
            })
            .Build();
    }
}